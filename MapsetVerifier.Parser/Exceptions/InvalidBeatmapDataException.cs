namespace MapsetVerifier.Parser.Exceptions;

public class InvalidBeatmapDataException : Exception
{
    public InvalidBeatmapDataException(string message) : base(message) { }
    public InvalidBeatmapDataException(string message, Exception inner) : base(message, inner) { }
}
