using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Controls;

public class PlaylistTracksControllerTests
{
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly Mock<IPlayerService> _mockPlayerService;
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly PlaylistTracksController _controller;

    private readonly Guid _playlistId = Guid.NewGuid();
    
    public PlaylistTracksControllerTests()
    {
        _mockJellyTuneApiService = new Mock<IJellyTuneApiService>();
        _mockPlayerService = new Mock<IPlayerService>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockFileService = new Mock<IFileService>();
        _controller = new PlaylistTracksController(_mockJellyTuneApiService.Object, _mockConfigurationService.Object,
            _mockPlayerService.Object, _mockFileService.Object);

        var playlist = new Playlist()
        {
            Id =  _playlistId,
            Name = "Test Playlist"
        };
        
        var track = new Track()
        {
            Id = Guid.NewGuid(),
            Name = "Track"
        };
        
        _mockJellyTuneApiService.Setup(repo => repo.GetPlaylistTracksAsync(_playlistId)).ReturnsAsync([track]);
        _mockJellyTuneApiService.Setup(repo => repo.GetPlaylistAsync(_playlistId)).ReturnsAsync(playlist);
    }
    
    [Fact]
    public async Task OpenPlaylist()
    {
        var evt = Assert.Raises<PlaylistTracksStateArgs>(handler => _controller.OnPlaylistTracksStateChanged += handler, handler => _controller.OnPlaylistTracksStateChanged -= handler, () => _controller.OpenPlaylist(_playlistId).GetAwaiter().GetResult() );
        
        Assert.Equal(_controller, evt.Sender);
        Assert.False(evt.Arguments.Loading);
        Assert.NotNull(_controller.Playlist);
        Assert.NotEmpty(_controller.Tracks);
    }
}