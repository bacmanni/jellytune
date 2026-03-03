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

        var details = await RetryAsync(() => q.LookupArtistAsync(artist.Id));
        if (details == null) return result;

        result.MbId = details.Id;
        result.Area = details.Area?.Name;
        result.From = details.LifeSpan?.Begin?.Year;
        result.To = details.LifeSpan?.End?.Year;

        Console.WriteLine($"Fetching data from wikipedia");
        var searchResults = await SearchWikipedia(artistName);
        if (searchResults.Any())
        {
            var description = await GetWikipediaPageDescription(searchResults.FirstOrDefault().Key);
            result.Description = description;
        }
        
        return result;
    }

    private async Task<string?> GetWikipediaPageDescription(long pageId)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{_applicationInfo.Name}/{_applicationInfo.Version} ({_applicationInfo.Email})");

        var url = $"https://en.wikipedia.org/w/api.php?action=query&prop=extracts&exintro=true&explaintext=true&pageids={pageId}&format=json";
        var data = await RetryAsync(() => http.GetFromJsonAsync<JsonElement>(url));
        var pages = data.GetProperty("query").GetProperty("pages");
        var page = pages.GetProperty(pageId.ToString());
        var description = page.TryGetProperty("extract", out var extract) ? extract.GetString() : null;
        return description;
    }
    
    private async Task<Dictionary<long, string>> SearchWikipedia(string value)
    {
        Console.WriteLine($"Fetching data from wikipedia");
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{_applicationInfo.Name}/{_applicationInfo.Version} ({_applicationInfo.Email})");
        var url = "https://en.wikipedia.org/w/api.php?action=query&list=search&format=json&srsearch=" + Uri.EscapeDataString(value);
        var data = await RetryAsync(() => http.GetFromJsonAsync<JsonElement>(url));
        var search = data.GetProperty("query").GetProperty("search");

        var results = new Dictionary<long, string>();
        foreach (var item in search.EnumerateArray())
        {
            var pageId = item.GetProperty("pageid").GetInt64();
            var title = item.GetProperty("title").GetString() ?? string.Empty;
            results.TryAdd(pageId, title);
        }
        
        Console.WriteLine($"Got {results.Count} results");

        var sorted = results
            .OrderBy(kvp => Levenshtein(kvp.Value.ToLower(), value.ToLower()))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        return sorted;
    }
    
    public async Task<Models.External.Album?> GetAlbumAsync(string artistName, string albumName)
    {
        if (string.IsNullOrWhiteSpace(artistName) || string.IsNullOrWhiteSpace(albumName)) return null;

        var result = new Models.External.Album()
        {
            Name = albumName,
        };

        var searchResults = await SearchWikipedia(albumName);
        if (searchResults.Any())
        {
            var description = await GetWikipediaPageDescription(searchResults.FirstOrDefault().Key);
            result.Description = description;
        }

        return result;
    }

    private static int Levenshtein(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[n, m];
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