using JellyTune.Shared.Controls;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using Moq;

namespace JellyTune.Test.Controls;

public class ArtistAlbumControllerTests
{
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly ArtistAlbumController _controller;
    
    private readonly Guid _artistId;
    private readonly Guid _trackId;
    
    public ArtistAlbumControllerTests()
    {
        _mockJellyTuneApiService =  new Mock<IJellyTuneApiService>();
        _mockFileService = new Mock<IFileService>();
        _controller = new ArtistAlbumController(_mockJellyTuneApiService.Object, _mockFileService.Object);
        
        _artistId = Guid.NewGuid();
        _trackId = Guid.NewGuid();

        var artist = new Artist()
        {
            Id =  _artistId
        };
        
        var album1 = new Album()
        {
            Id = Guid.NewGuid(),
            ArtistId =  _artistId,
        };
        
        var album2 = new Album()
        {
            Id = Guid.NewGuid(),
            ArtistId =  _artistId,
        };

        var albums = new List<Album>()
        {
            album1, album2
        };
        
        _mockJellyTuneApiService.Setup(repo => repo.GetArtistAlbumsAsync(_artistId, null)).ReturnsAsync(albums);
        _mockJellyTuneApiService.Setup(repo => repo.GetArtistByTrackIdAsync(_trackId)).ReturnsAsync(_artistId);
    }
    
    [Fact]
    public async Task OpenByArtistIdAsync()
    {
        var evt = Assert.Raises<ArtistAlbumArgs>(handler => _controller.OnAlbumsChanged += handler, handler => _controller.OnAlbumsChanged -= handler, () => _controller.OpenByArtistIdAsync(_artistId).GetAwaiter().GetResult() );
        Assert.Equal(_controller, evt.Sender);
        Assert.False(evt.Arguments.IsLoading);
        
        Assert.NotEmpty(_controller.Albums);
    }
    
    [Fact]
    public async Task OpenByTrackIdAsync()
    {
        var evt = Assert.Raises<ArtistAlbumArgs>(handler => _controller.OnAlbumsChanged += handler, handler => _controller.OnAlbumsChanged -= handler, () => _controller.OpenByTrackIdAsync(_trackId).GetAwaiter().GetResult() );
        Assert.Equal(_controller, evt.Sender);
        Assert.False(evt.Arguments.IsLoading);
        
        Assert.NotEmpty(_controller.Albums);
    }
}