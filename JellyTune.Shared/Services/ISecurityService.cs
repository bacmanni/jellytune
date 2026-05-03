namespace JellyTune.Shared.Services;

public interface ISecurityService
{
    public Task OpenSessionAsync();
    public Task SetPasswordAsync(string? password);
    public Task<string?> GetPasswordAsync();
}