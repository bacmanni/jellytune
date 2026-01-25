using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Services;

public class PlayerServiceTests
{
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly IPlayerService _playerService;

    private readonly Guid _almumId1;
    private readonly Guid _almumId2;
    private readonly Guid _trackId1;
    private readonly Guid _trackId2;
    private readonly Guid _trackId3;
    
    public PlayerServiceTests()
    {
        _mockJellyTuneApiService = new Mock<IJellyTuneApiService>();
        _playerService = new PlayerService(_mockJellyTuneApiService.Object);
        
        _almumId1 = Guid.NewGuid();
        var album1 = new Album()
        {
            Id = _almumId1,
            Artist = "Test artist 1",
            Name = "Test album",
            Runtime = TimeSpan.FromHours(2),
            Year = 1979,
            HasArtwork = false
        };
        
        _almumId2 = Guid.NewGuid();
        var album2 = new Album()
        {
            Id = _almumId2,
            Artist = "Test artist 2",
            Name = "Test album",
            Runtime = TimeSpan.FromHours(2),
            Year = 1979,
            HasArtwork = false
        };
        
        _trackId1 = Guid.NewGuid();
        var track1 = new Track()
        {
            Id =  _trackId1,
            Artist = "Test artist 1",
            Name = "Test track 1",
            AlbumId =  _almumId1,
            RunTime =  TimeSpan.FromMinutes(5),
            HasArtwork = false
        };
        
        _trackId2 = Guid.NewGuid();
        var track2 = new Track()
        {
            Id =  _trackId2,
            Artist = "Test artist 1",
            Name = "Test track 2",
            AlbumId =  _almumId1,
            RunTime =  TimeSpan.FromMinutes(5),
            HasArtwork = false
        };
        
        _trackId3 = Guid.NewGuid();
        var track3 = new Track()
        {
            Id =  _trackId3,
            Artist = "Test artist 2",
            Name = "Test track 3",
            AlbumId =  _almumId2,
            RunTime =  TimeSpan.FromMinutes(5),
            HasArtwork = false
        };
        
        _mockJellyTuneApiService.Setup(repo => repo.GetTrackAsync(_trackId1)).ReturnsAsync(track1);
        _mockJellyTuneApiService.Setup(repo => repo.GetTrackAsync(_trackId2)).ReturnsAsync(track2);
        _mockJellyTuneApiService.Setup(repo => repo.GetTrackAsync(_trackId3)).ReturnsAsync(track3);
        _mockJellyTuneApiService.Setup(repo => repo.GetAlbumAsync(_almumId1, CancellationToken.None)).ReturnsAsync(album1);
        _mockJellyTuneApiService.Setup(repo => repo.GetAlbumAsync(_almumId2, CancellationToken.None)).ReturnsAsync(album2);
        _mockJellyTuneApiService.Setup(repo => repo.GetTracksAsync(_almumId1, CancellationToken.None)).ReturnsAsync(new List<Track>() { track1, track2 });
        _mockJellyTuneApiService.Setup(repo => repo.GetTracksAsync(_almumId2, CancellationToken.None)).ReturnsAsync(new List<Track>() { track3 });
        _mockJellyTuneApiService.Setup(repo => repo.GetAudioStreamUrl(null, _trackId1, null)).Returns("http://test");
        _mockJellyTuneApiService.Setup(repo => repo.GetAudioStreamUrl(null, _trackId2, null)).Returns("http://test");
        _mockJellyTuneApiService.Setup(repo => repo.GetAudioStreamUrl(null, _trackId3, null)).Returns("http://test");
    }

    [Fact]
    public async Task StartTrackAsync_NoTracks()
    {
        // Nothing to select, so selected track is null
        _playerService.SelectTrack(_trackId1);
        Assert.Null(_playerService.GetSelectedTrack());
        
        // Starting loads tracks
        await _playerService.StartTrackAsync(_trackId1);
        
        // Test some basic functionality
        var selectedTrack = _playerService.GetSelectedTrack();
        Assert.NotNull(selectedTrack);
        Assert.Equal(_trackId1, selectedTrack.Id);
        Assert.True(_playerService.IsPlayingTrack(_trackId1));

        Assert.False(_playerService.HasPreviousTrack());
        Assert.True(_playerService.HasNextTrack());
        
        await _playerService.NextTrackAsync();
        Assert.True(_playerService.IsPlayingTrack(_trackId2));
        
        Assert.False(_playerService.HasNextTrack());
        Assert.True(_playerService.HasPreviousTrack());
        
        await _playerService.PreviousTrackAsync();
        Assert.True(_playerService.IsPlayingTrack(_trackId1));
        
        _playerService.StopTrack();
        Assert.False(_playerService.IsPlayingTrack(_trackId1));
    }

    [Fact]
    public void StartTrackAsync_Tracks_DifferentAlbum()
    {
        var trackId1 = Guid.NewGuid();
        var trackId2 = Guid.NewGuid();

        var tracks = new List<Track>()
        {
            new() 
            {
                Id =  trackId1,
                Artist = "Test artist 1",
                Name = "Test track 1",
                AlbumId =  _almumId1,
                RunTime =  TimeSpan.FromMinutes(5),
                HasArtwork = false
            },
            new()
            {
                Id =  trackId2,
                Artist = "Test artist 1",
                Name = "Test track 2",
                AlbumId =  _almumId2,
                RunTime =  TimeSpan.FromMinutes(5),
                HasArtwork = false
            }
        };

        _playerService.AddTracks(tracks);
        Assert.Equal(tracks.Count, _playerService.GetTracks().Count);

        _playerService.StartTrackAsync(trackId1);
        
        // Test some basic functionality
        var selectedTrack = _playerService.GetSelectedTrack();
        Assert.NotNull(selectedTrack);
        Assert.Equal(trackId1, selectedTrack.Id);
        Assert.True(_playerService.IsPlayingTrack(trackId1));
        
        _playerService.ClearTracks();
        Assert.Equal(1, _playerService.GetTracks()?.Count);
        
        _playerService.StopTrack();
        _playerService.ClearTracks();
        Assert.Empty(_playerService.GetTracks());
    }

    [Fact]
    public void StartTrackAsync_Events()
    {
        //Start
        var evt = Assert.Raises<PlayerStateArgs>( handler => _playerService.OnPlayerStateChanged += handler, handler => _playerService.OnPlayerStateChanged -= handler, () => _playerService.StartTrackAsync(_trackId1).GetAwaiter().GetResult() );
        Assert.Equal(_playerService, evt.Sender);
        Assert.Equal(PlayerState.Playing, evt.Arguments.State);
        Assert.Equal(2, evt.Arguments.Tracks.Count);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrackId);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrack?.Id);
        Assert.Equal(_almumId1, evt.Arguments.Album?.Id);
        
        // Next
        evt = Assert.Raises<PlayerStateArgs>( handler => _playerService.OnPlayerStateChanged += handler, handler => _playerService.OnPlayerStateChanged -= handler, () => _playerService.NextTrackAsync().GetAwaiter().GetResult() );
        Assert.Equal(_playerService, evt.Sender);
        Assert.Equal(PlayerState.Playing, evt.Arguments.State);
        Assert.Equal(2, evt.Arguments.Tracks.Count);
        Assert.Equal(_trackId2, evt.Arguments.SelectedTrackId);
        Assert.Equal(_trackId2, evt.Arguments.SelectedTrack?.Id);
        Assert.Equal(_almumId1, evt.Arguments.Album?.Id);
        
        // Previous
        evt = Assert.Raises<PlayerStateArgs>( handler => _playerService.OnPlayerStateChanged += handler, handler => _playerService.OnPlayerStateChanged -= handler, () => _playerService.PreviousTrackAsync().GetAwaiter().GetResult() );
        Assert.Equal(_playerService, evt.Sender);
        Assert.Equal(PlayerState.Playing, evt.Arguments.State);
        Assert.Equal(2, evt.Arguments.Tracks.Count);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrackId);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrack?.Id);
        Assert.Equal(_almumId1, evt.Arguments.Album?.Id);
        
        // Start or pause
        var playerState = _playerService.GetPlaybackState() == PlayerState.Playing ? PlayerState.Paused : PlayerState.Playing;
        evt = Assert.Raises<PlayerStateArgs>( handler => _playerService.OnPlayerStateChanged += handler, handler => _playerService.OnPlayerStateChanged -= handler, () => _playerService.StartOrPauseTrackAsync().GetAwaiter().GetResult() );
        Assert.Equal(_playerService, evt.Sender);
        Assert.Equal(playerState, evt.Arguments.State);
        Assert.Equal(2, evt.Arguments.Tracks.Count);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrackId);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrack?.Id);
        Assert.Equal(_almumId1, evt.Arguments.Album?.Id);
        
        // Another way round
        playerState = _playerService.GetPlaybackState() == PlayerState.Playing ? PlayerState.Paused : PlayerState.Playing;
        evt = Assert.Raises<PlayerStateArgs>( handler => _playerService.OnPlayerStateChanged += handler, handler => _playerService.OnPlayerStateChanged -= handler, () => _playerService.StartOrPauseTrackAsync().GetAwaiter().GetResult() );
        Assert.Equal(_playerService, evt.Sender);
        Assert.Equal(playerState, evt.Arguments.State);
        Assert.Equal(2, evt.Arguments.Tracks.Count);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrackId);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrack?.Id);
        Assert.Equal(_almumId1, evt.Arguments.Album?.Id);
        
        // Select
        evt = Assert.Raises<PlayerStateArgs>( handler => _playerService.OnPlayerStateChanged += handler, handler => _playerService.OnPlayerStateChanged -= handler, () => _playerService.SelectTrack(_trackId1) );
        Assert.Equal(_playerService, evt.Sender);
        Assert.Equal(PlayerState.Selected, evt.Arguments.State);
        Assert.Equal(2, evt.Arguments.Tracks.Count);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrackId);
        Assert.Equal(_trackId1, evt.Arguments.SelectedTrack?.Id);
        Assert.Equal(_almumId1, evt.Arguments.Album?.Id);
    }
}