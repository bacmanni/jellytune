using Adw;
using Gdk;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Models;
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
    private IPlayerService _playerService;
    private Track _track;
    private PlayerState _startupState;
    private bool _showQueue = false;
    
    [Connect] private Image _status;
    [Connect] private Label _queueLabel;
    [Connect] private Spinner _spinner;
    [Connect] private Image _albumArt;
    [Connect] private Label _runtime;
    [Connect] private Label _number;

    public Guid TrackId => _track.Id;

    public static TrackRow NewWithValues(IPlayerService playerService, IFileService fileService, Track track, PlayerState state, List<RowProperty> properties)
    {
        var obj = NewWithProperties([]);
        obj._playerService = playerService;
        obj._fileService = fileService;
        obj._track = track;
        obj._startupState = state;
        obj.Activatable = true;
        obj.Focusable = true;
        obj.Selectable = false;
        obj._showQueue = properties.Contains(RowProperty.Queue);
        
        obj._runtime.SetText(obj._track.RunTime.ToString("m\\:ss"));

        if (properties.Contains(RowProperty.Extended))
        {
            obj._number.SetVisible(false);
            obj._albumArt.SetVisible(true);
            
            obj.SetSubtitle(Markup.EscapeText(obj._track.Artist ?? string.Empty));
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
        UpdateQueue();
        
        _status.SetVisible(false);
        _spinner.SetVisible(true);
        SetTitle($"<b>{Markup.EscapeText(_track.Name ?? string.Empty)}</b>");
    }
    
    private void StartTrack()
    {
        UpdateQueue();
        
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName("media-playback-start-symbolic");
        SetTitle($"<b>{Markup.EscapeText(_track.Name ?? string.Empty)}</b>");
    }

    private void ClearTrack()
    {
        UpdateQueue();
        
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName(null);
        SetTitle(Markup.EscapeText(_track.Name ?? string.Empty));
    }

    private void StopTrack()
    {
        UpdateQueue();
        
        _spinner.SetVisible(false);
        _status.SetVisible(true);
        _status.SetFromIconName("media-playback-pause-symbolic");
        SetTitle($"<b>{Markup.EscapeText(_track.Name ?? string.Empty)}</b>");
    }

    private void UpdateQueue()
    {
        if (_showQueue)
        {
            var position = _playerService.GetQueuePosition(_track.Id);
            _queueLabel.SetVisible(position > 0);

            if (position > 0)
            {
                _queueLabel.SetLabel(position.ToString() ?? string.Empty);
            }
        }
    }
    
    public override void Dispose()
    {
        _albumArt.Clear();
        base.Dispose();
    }
}