using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using JellyTune.Shared.Models;
using MetaBrainz.MusicBrainz;

namespace JellyTune.Shared.Services;

public class JellyTuneExtApiService : IJellyTuneExtApiService
{
    private readonly ApplicationInfo _applicationInfo;

    public JellyTuneExtApiService(ApplicationInfo applicationInfo)
    {
        _applicationInfo = applicationInfo;
    }

    /// <summary>
    /// Get artist information by name
    /// </summary>
    /// <param name="artistName"></param>
    /// <returns></returns>
    public async Task<Models.External.Artist?> GetArtistAsync(string artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName)) return null;
        
        var result = new Models.External.Artist() { Name = artistName };
        
        Console.WriteLine($"Fetching data from musicbrainz");
        
        using var q = new Query(_applicationInfo.Name, _applicationInfo.Version, _applicationInfo.Email);
        var artists = await RetryAsync(() => q.FindArtistsAsync($"name:{artistName}", 1, 0, false));
        if (artists == null) return result;
        
        var artist = artists.Results.FirstOrDefault()?.Item;
        if (artist == null) return result;

        var details = await RetryAsync(() => q.LookupArtistAsync(artist.Id, Include.UrlRelationships));
        if (details == null) return result;
        
        var wikipediaUrl = details.Relationships.FirstOrDefault(r => r.Type == "wikipedia")?.Target;

        result.MbId = details.Id;
        result.Area = details.Area?.Name;
        result.From = details.LifeSpan?.Begin?.Year;
        result.To = details.LifeSpan?.End?.Year;
        
        string? wikipediaTitle = null;
        
        // If no direct link is found we use wikidata
        if (wikipediaUrl != null)
        {
            result.WikipediaUrl = wikipediaUrl.ToString();
            wikipediaTitle = wikipediaUrl.ToString()?.Split('/').Last();
        }
        else
        {
            var wikidataUrl = details.Relationships?.FirstOrDefault(r => r.Type == "wikidata")?.Target;
            if (wikidataUrl == null) return result;
            
            result.WikidataUrl = wikidataUrl.ToString();
            wikipediaTitle = await GetWikipediaTitleFromWikidata(wikidataUrl.ToString());
        }

        if (wikipediaTitle == null) return result;
        
        Console.WriteLine($"Fetching data from wikipedia: {wikipediaTitle}");
        result.WikipediaTitle = wikipediaTitle;
        
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{_applicationInfo.Name}/{_applicationInfo.Version} ({_applicationInfo.Email})");
        var url = $"https://en.wikipedia.org/w/api.php?action=query&prop=extracts&exintro=true&explaintext=true&titles={Uri.EscapeDataString(wikipediaTitle)}&format=json";
        var data = await RetryAsync(() => http.GetFromJsonAsync<JsonElement>(url));
        var pages = data.GetProperty("query").GetProperty("pages");
        var firstPage = pages.EnumerateObject().First().Value;
        var description = firstPage.TryGetProperty("extract", out var extract) ? extract.GetString() : null;
        result.Description = description;
        return result;
    }

    public Task<Models.External.Album?> GetAlbumAsync(string artistName, string albumName)
    {
        if (string.IsNullOrWhiteSpace(artistName) || string.IsNullOrWhiteSpace(albumName)) return null;

        var result = new Models.External.Album()
        {
            Name = albumName,
        };

        return null;
    }

    private string? GetWikidataIdFromUrl(string url)
    {
        var match = Regex.Match(url, @"[A-Za-z]\d+");
        if (match.Success) return match.Value;
            
        return null;
    }
    
    private async Task<string?> GetWikipediaTitleFromWikidata(string url)
    {
        var wikidataId = GetWikidataIdFromUrl(url);
            
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{_applicationInfo.Name}/{_applicationInfo.Version} ({_applicationInfo.Email})");
        var wikidataApi = $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={wikidataId}&format=json&props=sitelinks&languages=en";

        var wikidataJson = await RetryAsync(() => http.GetFromJsonAsync<JsonElement>(wikidataApi));
        
        if (!wikidataJson.TryGetProperty("entities", out var entities)) return null;
        
        if (!entities.TryGetProperty(wikidataId, out var entity)) return null;
        
        if (!entity.TryGetProperty("sitelinks", out var sitelinks)) return null;
        
        if (!sitelinks.TryGetProperty("enwiki", out var enwiki)) return null;
        
        if (!enwiki.TryGetProperty("title", out var titleElement)) return null;
        
        var wikipediaTitle = titleElement.GetString();
        if (!string.IsNullOrWhiteSpace(wikipediaTitle))
            return wikipediaTitle;
        
        return null;
    }
    
    private async Task<T?> RetryAsync<T>(Func<Task<T>> action, int retries = 3, int delayMs = 500)
    {
        for (int attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException) when (attempt < retries)
            {
                await Task.Delay(delayMs);
            }
            catch (TaskCanceledException) when (attempt < retries)
            {
                await Task.Delay(delayMs);
            }
        }

        return default;
    }
}