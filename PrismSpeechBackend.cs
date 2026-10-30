using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RoadtripAccessibilityProbe
{
    // Prism (https://github.com/ethindp/prism) picks the best backend: a running screen
    // reader (NVDA, JAWS, ...) first, otherwise Windows TTS (SAPI / OneCore).
    // Minimal P/Invoke wrapper over Prism's C API; the prismatoid .NET bindings need a
    // newer runtime than the game's Mono. prism.dll (32-bit) is loaded by full path first
    // so the [DllImport] calls resolve to that already-loaded module.
    internal sealed class PrismSpeechBackend : ISpeechBackend
    {
        private const string PrismLib = "prism.dll";
        private const ulong SupportsOutput = 1UL << 5;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_init(IntPtr cfg);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void prism_shutdown(IntPtr ctx);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_acquire_best(IntPtr ctx);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void prism_backend_free(IntPtr backend);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong prism_backend_get_features(IntPtr backend);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_backend_speak(IntPtr backend, byte[] text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport(PrismLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_backend_output(IntPtr backend, byte[] text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        private readonly string _pluginDirectory;
        private IntPtr _ctx;
        private IntPtr _backend;
        private ulong _features;

        internal PrismSpeechBackend(string pluginDirectory)
        {
            _pluginDirectory = pluginDirectory;
        }

        public string Name => "Prism";

        internal bool Initialize()
        {
            string prismPath = Path.Combine(_pluginDirectory, PrismLib);
            if (!File.Exists(prismPath) || LoadLibraryW(prismPath) == IntPtr.Zero)
            {
                return false;
            }

            _ctx = prism_init(IntPtr.Zero);
            if (_ctx == IntPtr.Zero)
            {
                return false;
            }

            _backend = prism_registry_acquire_best(_ctx);
            if (_backend == IntPtr.Zero)
            {
                return false;
            }

            _features = prism_backend_get_features(_backend);
            return true;
        }

        public void Speak(string text, bool interrupt)
        {
            if (_backend == IntPtr.Zero || string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                byte[] utf8 = new byte[Encoding.UTF8.GetByteCount(text) + 1];
                Encoding.UTF8.GetBytes(text, 0, text.Length, utf8, 0);

                // Output = speech + braille on screen readers that support it.
                if ((_features & SupportsOutput) != 0)
                {
                    prism_backend_output(_backend, utf8, interrupt);
                }
                else
                {
                    prism_backend_speak(_backend, utf8, interrupt);
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            try
            {
                if (_backend != IntPtr.Zero)
                {
                    prism_backend_free(_backend);
                    _backend = IntPtr.Zero;
                }

                if (_ctx != IntPtr.Zero)
                {
                    prism_shutdown(_ctx);
                    _ctx = IntPtr.Zero;
                }
            }
            catch
            {
            }
        }
    }
}
