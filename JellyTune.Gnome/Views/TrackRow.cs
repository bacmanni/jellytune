using Adw;
using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Spinner = Adw.Spinner;

namespace JellyTune.Gnome.Views;

[Subclass<ActionRow>(qualifiedName: "JellyTuneTrackRow")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.track_row.ui")]
public partial class TrackRow
{
    private IFileService  _fileService;
    private Track _track;
    private PlayerState _startupState;
    
    [Connect] private Image _status;
    [Connect] private Spinner _spinner;
    [Connect] private Image _albumArt;
    [Connect] private Label _runtime;
    [Connect] private Label _number;

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
            
            obj.SetSubtitle(Markup.EscapeText(obj._track.Artist != null ? obj._track.Artist : string.Empty));
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
        
        using var bytes = Bytes.New(albumArt);
        using var texture = Texture.NewFromBytes(bytes);
        _albumArt.SetFromPaintable(texture);
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
        SetTitle($"<b>{Markup.EscapeText(_track.Name != null ? _track.Name : string.Empty)}</b>");
    }
    
    private void StartTrack()
    {
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName("media-playback-start-symbolic");
        SetTitle($"<b>{Markup.EscapeText(_track.Name != null ? _track.Name : string.Empty)}</b>");
    }

    private void ClearTrack()
    {
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName(null);
        SetTitle(Markup.EscapeText(_track.Name != null ? _track.Name : string.Empty));
    }

    private void StopTrack()
    {
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName("media-playback-pause-symbolic");
        SetTitle($"<b>{Markup.EscapeText(_track.Name != null ? _track.Name : string.Empty)}</b>");
    }

    public override void Dispose()
    {
        _albumArt.Clear();
        base.Dispose();
    }
}