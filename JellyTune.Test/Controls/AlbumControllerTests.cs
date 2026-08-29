using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Controls;

public class AlbumControllerTests
{
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly Mock<IPlayerService> _mockPlayerService;
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly AlbumController _controller;
    
    private readonly Guid _albumId;
    private readonly Guid _trackId;
    
    public AlbumControllerTests()
    {
        _mockJellyTuneApiService =  new Mock<IJellyTuneApiService>();
        _mockPlayerService = new Mock<IPlayerService>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockFileService = new Mock<IFileService>();
        _controller = new AlbumController(_mockJellyTuneApiService.Object, _mockConfigurationService.Object, _mockPlayerService.Object, _mockFileService.Object);
        
        _albumId = Guid.NewGuid();
        _trackId = Guid.NewGuid();
        
        var album = new Album
        {
            Id = _albumId,
            ArtistId =  Guid.NewGuid(),
            HasArtwork = false
        };

        var track1 = new Track
        {
            Id = _trackId,
            AlbumId =  _albumId,
            Name =  "Track 1"
        };
        
        var track2 = new Track
        {
            Id = Guid.NewGuid(),
            AlbumId =  _albumId,
            Name =  "Track 2"
        };
        
        var tracks = new List<Track> {track1, track2};
        _mockJellyTuneApiService.Setup(repo => repo.GetAlbumAsync(_albumId, It.IsAny<CancellationToken>())).ReturnsAsync(album);
        _mockJellyTuneApiService.Setup(repo => repo.GetTracksAsync(_albumId, It.IsAny<CancellationToken>())).ReturnsAsync(tracks);
    }
    
    [Fact]
    public async Task OpenAsync()
    {
        var evt = Assert.Raises<AlbumStateArgs>(handler => _controller.OnAlbumChanged += handler, handler => _controller.OnAlbumChanged -= handler, () => _controller.OpenAsync(_albumId).GetAwaiter().GetResult() );
        
        Assert.Equal(_controller, evt.Sender);
        Assert.True(evt.Arguments.UpdateAlbum);
        Assert.NotNull(_controller.Album);
        Assert.True(evt.Arguments.UpdateTracks);
        Assert.NotEmpty(_controller.Tracks);
        
        Assert.Null(_controller.SelectedTrack);
        _controller.SelectTrack(_trackId);
        Assert.NotNull(_controller.SelectedTrack);
    }
}