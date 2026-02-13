using System;
using System.Threading.Tasks;
using Adw.Internal;
using JellyTune.Shared.Models;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Services;
using JellyTune.Gnome.Helpers;

namespace JellyTune.Gnome.Views;

public partial class TrackRow : Adw.ActionRow
{
    private readonly IFileService  _fileService;
    private readonly Track _track;
    
    [Gtk.Connect] private readonly Gtk.Image _status;
    [Gtk.Connect] private readonly Adw.Spinner _spinner;
    [Gtk.Connect] private readonly Gtk.Image _albumArt;
    [Gtk.Connect] private readonly Gtk.Label _runtime;
    [Gtk.Connect] private readonly Gtk.Label _number;
    [Gtk.Connect] private readonly Gtk.Button _queue;
    public Guid TrackId => _track.Id;

    private TrackRow(Gtk.Builder builder) : base(
        new ActionRowHandle(builder.GetPointer("_root"), false))
    {
        builder.Connect(this);
    }
    
    public TrackRow(IFileService fileService, Track track, PlayerState state, bool extended = false) : this(Blueprint.BuilderFromFile("track_row"))
    {
        _fileService  = fileService;
        _track = track;
        Activatable = true;
        CanFocus = false;
        
        _runtime.SetText(_track.RunTime.ToString("m\\:ss"));

        if (extended)
        {
            _number.SetVisible(false);
            _albumArt.SetVisible(true);
            
            SetSubtitle(_track.Artist);
            
            if (_track.HasArtwork)
                _ = UpdateArtwork();
        }
        else
        {
            if (_track.Number > 0)
                _number.SetText($"{_track.Number.ToString()}.");
        }
        
        UpdateState(state);
    }

    private async Task UpdateArtwork()
    {
        var albumArt = await _fileService.GetFileAsync(FileType.AlbumArt, _track.AlbumId);
        if  (albumArt == null || albumArt.Length == 0)
            return;
        
        GLib.MainContext.Default().InvokeFull(0, () =>
        {
            using var bytes = GLib.Bytes.New(albumArt);
            using var texture = Gdk.Texture.NewFromBytes(bytes);
            _albumArt.SetFromPaintable(texture);
            return false;
        });
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