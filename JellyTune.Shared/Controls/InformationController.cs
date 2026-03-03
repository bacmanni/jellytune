using System.Collections;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Events;
using JellyTune.Shared.Models;
using JellyTune.Shared.Services;
using MetaBrainz.MusicBrainz;

namespace JellyTune.Shared.Controls;

public class InformationController
{
    private readonly IJellyTuneExtApiService _jellyTuneExtApiService;
    
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public List<string> Subtitles { get; } = [];

    public event EventHandler<InformationArgs> OnInformationChanged;
    
    public InformationController(IJellyTuneExtApiService jellyTuneExtApiService)
    {
        _jellyTuneExtApiService = jellyTuneExtApiService;
    }

    /// <summary>
    /// Load description for currently active artist
    /// </summary>
    public async Task OpenArtistAsync(string artistName)
    {
        try
        {
            var artist = await _jellyTuneExtApiService.GetArtistAsync(artistName);
            if (artist == null) return;
            
            Title = artistName;
            
            if (!string.IsNullOrWhiteSpace(artist?.Area))
                Subtitles.Add(artist.Area);
            
            if (artist.To.HasValue)
                Subtitles.Add($"{artist.From} - {artist.To}");
            else
                Subtitles.Add($"{artist.From} - Present");

            Description = artist?.Description;
            OnInformationChanged?.Invoke(this, new InformationArgs() { UpdateDetails = true });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Fetching artist information failed! Message:{e.Message}");
            OnInformationChanged?.Invoke(this, new InformationArgs() { HasError = true });
        }
    }

    public async Task OpenAlbumAsync(string artistName, string albumName)
    {
        try
        {
            var album = await _jellyTuneExtApiService.GetAlbumAsync(artistName, albumName);
            if (album == null) return;
            
            Title = albumName;
            Description = album.Description;
            OnInformationChanged?.Invoke(this, new InformationArgs() { UpdateDetails = true });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Fetching album information failed! Message:{e.Message}");
            OnInformationChanged?.Invoke(this, new InformationArgs() { HasError = true });
        }
    }
}