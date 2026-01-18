using System.IO.Abstractions;
using System.Text;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using JellyTune.Test.Models;
using Moq;

namespace JellyTune.Test.Services;

public class FileServiceTests
{
    private readonly Mock<IJellyTuneApiService> _mockJellyTuneApiService;
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly IFileService _fileService;

    private readonly byte[] _file;
    private readonly Guid _albumId1;
    private readonly Guid _albumId2;
    private readonly Guid _playlistId;
    private readonly string _url;
    
    public FileServiceTests()
    {
        _mockJellyTuneApiService = new Mock<IJellyTuneApiService>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockFileSystem  = new Mock<IFileSystem>();
        _fileService = new FileService(_mockJellyTuneApiService.Object, _mockConfigurationService.Object, _mockFileSystem.Object);

        _file = Encoding.UTF8.GetBytes("test string");
        _albumId1 = Guid.NewGuid();
        _albumId2 = Guid.NewGuid();
        _playlistId = Guid.NewGuid();
        _url = "http://test.com/";
        
        var configuration = new Configuration()
        {
            CacheAlbumArt = true
        };
        
        _mockJellyTuneApiService.Setup(repo => repo.GetPrimaryArtUrl(_albumId1)).Returns(new Uri($"{_url}{_albumId1.ToString()}.jpg"));
        _mockJellyTuneApiService.Setup(repo => repo.GetPrimaryArtAsync(_albumId1)).ReturnsAsync(_file);
        _mockJellyTuneApiService.Setup(repo => repo.GetPrimaryArtUrl(_playlistId)).Returns(new Uri($"{_url}{_playlistId.ToString()}.jpg"));
        _mockConfigurationService.Setup(repo => repo.Get()).Returns(configuration);
        _mockConfigurationService.Setup(repo => repo.GetCacheDirectory()).Returns("");
        _mockFileSystem.Setup(repo => repo.File.Exists($"/albums/{_albumId1.ToString()}.jpg")).Returns(true);
        _mockFileSystem.Setup(repo => repo.File.ReadAllBytesAsync($"/albums/{_albumId1.ToString()}.jpg", CancellationToken.None)).ReturnsAsync(_file);
        _mockFileSystem.Setup(repo => repo.File.Exists($"/albums/{_albumId2.ToString()}.jpg")).Returns(true);
        _mockFileSystem.Setup(repo => repo.Directory.Exists($"/albums/{_albumId2.ToString()}.jpg")).Returns(true);
        _mockFileSystem.Setup(repo => repo.Path.GetDirectoryName(It.IsAny<string>())).Returns("/");
        _mockFileSystem.Setup(repo => repo.Directory.Exists($"/")).Returns(true);
        _mockFileSystem.Setup(repo => repo.File.Exists($"/cache/test.json")).Returns(true);
    }

    [Fact]
    public void GetFileUrl()
    {
        var url = _fileService.GetFileUrl(FileType.AlbumArt, _albumId2);
        Assert.Equal(new Uri($"file:///albums/{_albumId2}.jpg"), url);
        
        var configuration = _mockConfigurationService.Object.Get();
        configuration.CacheAlbumArt = false;
        _mockConfigurationService.Object.Set(configuration);
        
        url = _fileService.GetFileUrl(FileType.AlbumArt, _albumId1);
        Assert.Equal(new Uri($"{_url}{_albumId1.ToString()}.jpg"), url);
        
        url = _fileService.GetFileUrl(FileType.Playlist, _playlistId);
        Assert.Equal(new Uri($"{_url}{_playlistId.ToString()}.jpg"), url);
    }
    
    [Fact]
    public async Task GetFileAsync()
    {
        // File from server
        var file = await _fileService.GetFileAsync(FileType.AlbumArt, _albumId1);
        Assert.Equal(file, _file);

        // File from cache
        file = await _fileService.GetFileAsync(FileType.AlbumArt, _albumId1);
        Assert.Equal(file, _file);
    }
    /*
    [Fact]
    public async Task GetCacheFile_WriteCacheFile()
    {
        var file = await _fileService.GetCacheFile<FileExample>("test");
        Assert.Null(file);

        var fileExample = new FileExample()
        {
            Value1 = "value1",
            Value2 = "value2",
        };
        
        // For stream we need to do some magic
        using var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, Encoding.UTF8, 1024, leaveOpen: true);
        
        _mockFileSystem.Setup(repo => repo.File.CreateText("filename")).Returns(writer);
        await _fileService.WriteCacheFile("test", fileExample);
        await writer.FlushAsync();
        
        _mockFileSystem.Setup(repo => repo.File.ReadAllTextAsync($"/cache/test.json", CancellationToken.None)).ReturnsAsync(fileExample.ToString());
        file = await _fileService.GetCacheFile<FileExample>("test");
        Assert.Equal(fileExample, file);
    }*/
}