using JellyTune.Shared.Models;

namespace JellyTune.Shared.Events;

public class ListStateArgs(Guid? id = null, List<ListItem>? items = null, bool isLoading = true) : EventArgs
{
    /// <summary>
    /// Selected item id
    /// </summary>
    public Guid? Id { get; set; } = id;
    
    /// <summary>
    /// All available items
    /// </summary>
    public List<ListItem>? Items { get; set; } = items;
    
    /// <summary>
    /// Should items be updated only
    /// </summary>
    public bool UpdateOnly { get; set; }
    
    /// <summary>
    /// Is list loading
    /// </summary>
    public bool IsLoading { get; set; } = isLoading;
}