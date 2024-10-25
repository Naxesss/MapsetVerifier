using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier
{
    internal class Track
    {
        private readonly string message;

        public Track(string message)
        {
            this.message = message;
            EventStatic.OnLoadStart(this.message);
        }

        public void Complete() => EventStatic.OnLoadComplete(message);
    }
}