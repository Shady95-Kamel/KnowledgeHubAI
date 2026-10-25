
namespace KnowledgeHubAI.Workers.Email.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "KnowledgeHubAI";
    public string SenderEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = string.Empty;
}
