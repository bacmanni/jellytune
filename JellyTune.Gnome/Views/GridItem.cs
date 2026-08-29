using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Models;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

[Subclass<Box>(qualifiedName: "JellyTuneGridItem")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.grid_item.ui")]
public partial class GridItem
{
    private IFileService _fileService;
    
    [Connect] private Image _art;
    [Connect] private Label _title;
    [Connect] private Label _description;
    
    private FileType _fileType;
    private CancellationTokenSource? _cancellationTokenSource;
    
    public static GridItem NewWithValues(IFileService fileService)
    {
        var obj = NewWithProperties([]);
        obj._fileService = fileService;
        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        CanFocus = false;
    }
    
    public void Bind(ListRow row)
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        _fileType = row.FileType;
        _title.SetLabel(row.Title);
        _description.SetLabel(row.Description);
        
        _art.Clear();
        
        if (!row.HasArtwork)
            return;
        
        _ = UpdateImage(row.Id);
    }

    private async Task UpdateImage(Guid id)
    {
        if (_cancellationTokenSource is { IsCancellationRequested: true })
        {
            return;
        }   
        
        var albumArt = await _fileService.GetFileAsync(_fileType, id);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = Bytes.New(albumArt);
        using var texture = Texture.NewFromBytes(bytes);
        _art.SetFromPaintable(texture);
    }
    
    public void Clear()
    {
        _cancellationTokenSource?.Cancel();
        _art.Clear();
    }
    
    public override void Dispose()
    {
        Clear();
        base.Dispose();
    }
}