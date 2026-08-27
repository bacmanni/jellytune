using JellyTune.Shared.Models;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Services;

namespace JellyTune.Gnome.Views;

[GObject.Subclass<Adw.ActionRow>(qualifiedName: "JellyTuneTrackRow")]
[Gtk.Template<Gtk.AssemblyResource>("JellyTune.Gnome.Blueprints.track_row.ui")]
public partial class TrackRow
{
    private IFileService  _fileService;
    private Track _track;
    private PlayerState _startupState;
    
    [Gtk.Connect] private Gtk.Image _status;
    [Gtk.Connect] private Adw.Spinner _spinner;
    [Gtk.Connect] private Gtk.Image _albumArt;
    [Gtk.Connect] private Gtk.Label _runtime;
    [Gtk.Connect] private Gtk.Label _number;

    public Guid TrackId => _track.Id;

    public static TrackRow NewWithValues(IFileService fileService, Track track, PlayerState state, bool extended = false)
    {
        var obj = NewWithProperties([]);
        obj._fileService  = fileService;
        obj._track = track;
        obj._startupState = state;
        obj.Activatable = true;
        obj.CanFocus = false;
        
        obj._runtime.SetText(obj._track.RunTime.ToString("m\\:ss"));

        if (extended)
        {
            obj._number.SetVisible(false);
            obj._albumArt.SetVisible(true);
            
            obj.SetSubtitle(GLib.Markup.EscapeText(obj._track.Artist));
        }
        else
        {
            if (obj._track.Number > 0)
                obj._number.SetText($"{obj._track.Number.ToString()}.");
        }

        obj.InitializeController();
        return obj;
    }

    private void InitializeController()
    {
        UpdateState(_startupState);
        
        if (_track.HasArtwork)
            _ = UpdateArtwork();
    }
    
    private async Task UpdateArtwork()
    {
        var albumArt = await _fileService.GetFileAsync(FileType.AlbumArt, _track.AlbumId);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        using var bytes = GLib.Bytes.New(albumArt);
        using var texture = Gdk.Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
        albumArt = null;
    }
    
    public void UpdateState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Selected:
            case PlayerState.Starting:
                LoadingTrack();
                break;
            case PlayerState.Playing:
                StartTrack();
                break;
            case PlayerState.Paused:
                StopTrack();
                break;
            default:
                ClearTrack();
                break;
        }
    }

    private void LoadingTrack()
    {
        _status.SetVisible(false);
        _spinner.SetVisible(true);
        SetTitle($"<b>{GLib.Markup.EscapeText(_track.Name)}</b>");
    }
    
    private void StartTrack()
    {
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName("media-playback-start-symbolic");
        SetTitle($"<b>{GLib.Markup.EscapeText(_track.Name)}</b>");
    }

    private void ClearTrack()
    {
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName(null);
        SetTitle(GLib.Markup.EscapeText(_track.Name));
    }

    private void StopTrack()
    {
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName("media-playback-pause-symbolic");
        SetTitle($"<b>{GLib.Markup.EscapeText(_track.Name)}</b>");
    }

    public override void Dispose()
    {
        _albumArt.Clear();
        base.Dispose();
    }
}