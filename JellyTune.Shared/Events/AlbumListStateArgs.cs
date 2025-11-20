using JellyTune.Shared.Models;

namespace JellyTune.Shared.Events;

public class AlbumListStateArgs(Guid collectionId, List<Album>? albums, bool isLoading = true) : EventArgs
{
    public Guid CollectionId { get; private set; } = collectionId;
    public List<Album>? Albums { get; private set; } = albums;
    public bool IsLoading { get; private set; } = isLoading;
}