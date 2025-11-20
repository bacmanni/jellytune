namespace JellyTune.Shared.Models;

public class SessionInfo
{
    public Guid? UserId { get; set; }
    public string? DeviceId { get; set; }
    public string? SessionId { get; set; }
}