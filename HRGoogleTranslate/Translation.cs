using System;

namespace HRGoogleTranslate
{
    internal struct Translation
    {
        public readonly string Input;
        public readonly string Output;
        public readonly DateTime Timestamp;

        public Translation(string input, string output)
        {
            Input = input;
            Output = output;
            Timestamp = DateTime.UtcNow;
        }
    }
}