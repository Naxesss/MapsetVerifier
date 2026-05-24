namespace MapsetVerifier.Framework;

public readonly record struct CheckProgress(int Completed, int Total, string? Label);
