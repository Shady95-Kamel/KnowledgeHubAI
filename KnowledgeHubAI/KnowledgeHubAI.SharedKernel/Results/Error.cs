namespace KnowledgeHubAI.SharedKernel.Results;

public sealed record Error
{
    public string Code { get; init; }

    public string Message { get; init; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static readonly Error None = new(string.Empty, string.Empty);
}