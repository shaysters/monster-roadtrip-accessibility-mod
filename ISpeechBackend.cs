using System;

namespace RoadtripAccessibilityProbe
{
    internal interface ISpeechBackend : IDisposable
    {
        string Name { get; }

        void Speak(string text, bool interrupt);
    }
}
