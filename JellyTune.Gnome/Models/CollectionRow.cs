using GObject;
using JellyTune.Shared.Models;
using Object = GObject.Object;

namespace JellyTune.Gnome.Models;

[Subclass<Object>]
public partial class CollectionRow
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public static CollectionRow New(Collection collection)
    {
        var row = NewWithProperties([]);

        row.Id = collection.Id;
        row.Name = collection.Name;

        return row;
    }
}