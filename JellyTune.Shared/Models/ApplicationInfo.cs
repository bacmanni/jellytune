namespace JellyTune.Shared.Models;

public class ApplicationInfo
{
    public string Id { get; set; }
    public string ApplicationId { get; set; }
    public string Name { get; set; }
    public string? Version { get; set; }
    public string? Developer { get; set; }
    public string? Copyright { get; set; }
    public string? Website { get; set; }
    public string? IssueUrl { get; set; }
    public string? License { get; set; }
    public string[] Designers { get; set; }
    public string[] Artists { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Icon { get; set; }
    public string? Email { get; set; }
}