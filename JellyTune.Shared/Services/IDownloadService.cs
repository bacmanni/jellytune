using System.IO.Abstractions;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;

namespace JellyTune.Shared.Services;

public interface IDownloadService
{
    public event EventHandler<DownloadArgs>? OnDownloadProgressChanged;
    public void DeleteParent(Guid parent);
    public Task DownloadTracksAsync(Guid parentId, List<Track> tracks);
    public bool IsDownloading(Guid? parentId);
    public bool HasParent(Guid parent);
    public bool HasTrackFile(Guid parent, Track track);
    public Stream GetAudioStream(Guid parent, Track track);
}