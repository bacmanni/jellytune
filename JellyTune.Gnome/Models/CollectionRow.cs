using GObject;
using JellyTune.Shared.Models;

namespace JellyTune.Gnome.Models;

[Subclass<GObject.Object>]
public partial class CollectionRow
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    
    public CollectionRow(Collection collection) : this()
    {
        Id = collection.Id;
        Name = collection.Name;
    }
}