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

public class ArtistController
{
    private readonly IJellyTuneExtApiService _jellyTuneExtApiService;
    
    public string? Name { get; private set; }
    public string? Area { get; private set; }
    public int? YearFrom { get; private set; }
    public int? YearTo { get; private set; }
    public string? Description { get; private set; }
    
    public event EventHandler<ArtistArgs> OnArtistChanged;
    
    public ArtistController(IJellyTuneExtApiService jellyTuneExtApiService)
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
            Name = artistName;
            Area  = artist?.Area;
            YearFrom = artist?.From;
            YearTo =  artist?.To;
            Description = artist?.Description;
            OnArtistChanged?.Invoke(this, new ArtistArgs() { UpdateDetails = true });
        }
        catch (Exception e)
        {
            Console.WriteLine($"Fetching artist information failed! Message:{e.Message}");
            OnArtistChanged?.Invoke(this, new ArtistArgs() { HasError = true });
        }
    }
}