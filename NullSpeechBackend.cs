namespace RoadtripAccessibilityProbe
{
    internal sealed class NullSpeechBackend : ISpeechBackend
    {
        internal static readonly NullSpeechBackend Instance = new NullSpeechBackend();

        public string Name => "None";

        private NullSpeechBackend()
        {
        }

        public void Speak(string text, bool interrupt)
        {
        }

        public void Dispose()
        {
        }
    }
}
