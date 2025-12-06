using System.Net;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Audio.Item.Universal;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Sdk.Generated.Sessions.Item.Playing;
using JellyTune.Shared.Enums;
using JellyTune.Shared.Models;
using Microsoft.Kiota.Abstractions;

namespace JellyTune.Shared.Services;

public class JellyTuneApiService : IJellyTuneApiService, IDisposable
{
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;

    private readonly int _searchCount = 30;
    private Guid? _collectionId;
    private string? _sessionId;
    private Guid? _userId;
    private string? _deviceId;
    
    public JellyTuneApiService(JellyfinSdkSettings sdkClientSettings, JellyfinApiClient jellyfinApiClient)
    {
        _sdkClientSettings = sdkClientSettings;
        _jellyfinApiClient = jellyfinApiClient;
    }
    
    public void Dispose()
    {
        _jellyfinApiClient.Dispose();
    }

    /// <summary>
    /// Set jellyfin api url
    /// </summary>
    /// <param name="serverUrl"></param>
    public bool SetServer(string serverUrl)
    {
        try
        {
            _sdkClientSettings.SetServerUrl(serverUrl);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    /// <summary>
    /// Set active collection id. Used example when fetching albums or searching
    /// </summary>
    /// <param name="collectionId"></param>
    public void SetCollectionId(Guid collectionId)
    {
        _collectionId = collectionId;
    }

    /// <summary>
    /// Get selected collection id
    /// </summary>
    /// <returns></returns>
    public Guid? GetCollectionId()
    {
        return _collectionId;
    }

    /// <summary>
    /// Check if server we are trying to connect is valid server
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns>Return true if connection was success and server is valid jellyfin server</returns>
    public async Task<bool> CheckServerAsync(string serverUrl)
    {
        try
        {
            _sdkClientSettings.SetServerUrl(serverUrl);
            var systemInfo = await _jellyfinApiClient.System.Info.Public.GetAsync()
                .ConfigureAwait(true);

            return true;
        }
        catch (InvalidOperationException ex)
        {
            return false;
        }
        catch (SystemException ex)
        {
            return false;
        }
        catch (HttpRequestException ex)
        {
            return false;
        }
    }

    /// <summary>
    /// Login to server with username/password
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns>Return true if success</returns>
    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var authenticationResult = await _jellyfinApiClient.Users.AuthenticateByName.PostAsync(
                new AuthenticateUserByName
                {
                    Username = username,
                    Pw = password
                }).ConfigureAwait(false);
            
            if (authenticationResult != null)
            {
                _sessionId = authenticationResult.SessionInfo?.Id;
                _userId = authenticationResult.User?.Id;
                _deviceId = authenticationResult.SessionInfo?.DeviceId;
                _sdkClientSettings.SetAccessToken(authenticationResult.AccessToken);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            return false;
        }
    }

    /// <summary>
    /// Search available albums by search criteria
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<Search>> SearchAlbum(string value, CancellationToken cancellationToken = default)
    {
        var searchResults = new List<Models.Search>();

        var queryResult = await _jellyfinApiClient.Items.GetAsync(configuration =>
        {
            configuration.QueryParameters.ParentId = _collectionId;
            configuration.QueryParameters.Limit = _searchCount;
            configuration.QueryParameters.Fields = [ItemFields.SortName];
            configuration.QueryParameters.Recursive = true;
            configuration.QueryParameters.IsMissing = false;
            configuration.QueryParameters.ImageTypeLimit = 1;
            configuration.QueryParameters.EnableTotalRecordCount = false;
            configuration.QueryParameters.SearchTerm = value;
            configuration.QueryParameters.IncludeItemTypes = [ BaseItemKind.MusicAlbum ];
        }, cancellationToken).ConfigureAwait(false);
        
        if (queryResult?.Items == null)
            return searchResults;
        
        foreach (var baseItem in queryResult.Items)
        {
            if (baseItem.Id == null)
                continue;

            var album = new Models.Search()
            {
                Id = baseItem.Id.GetValueOrDefault(),
                Type = SearchType.Album,
                ArtistName = baseItem.AlbumArtist ?? string.Empty,
                AlbumName = baseItem.Name ?? string.Empty,
                AlbumId = baseItem.Id.GetValueOrDefault(),
                HasArtwork = baseItem.ImageTags?.AdditionalData.ContainsKey(ImageType.Primary.ToString()) == true
            };
            
            searchResults.Add(album);
        }
        
        return searchResults;
    }
    
    /// <summary>
    /// Search available artist by search criteria
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<List<Models.Search>> SearchArtistAlbums(string value, CancellationToken cancellationToken = default)
    {
        var searchResults = new List<Models.Search>();
        var queryResult = await _jellyfinApiClient.Artists.GetAsync(configuration =>
        {
            configuration.QueryParameters.Limit = _searchCount;
            configuration.QueryParameters.Fields = [ItemFields.SortName];
            configuration.QueryParameters.ImageTypeLimit = 1;
            configuration.QueryParameters.SearchTerm = value;
        }, cancellationToken).ConfigureAwait(false);
        
        if (queryResult?.Items == null || queryResult.Items.Count == 0)
            return searchResults;

        var artistIds = queryResult.Items.Select(i => i.Id).ToArray();
        
        var queryResult2 = await _jellyfinApiClient.Items.GetAsync(configuration =>
           {
               configuration.QueryParameters.ParentId = _collectionId;
               configuration.QueryParameters.AlbumArtistIds = artistIds;
               configuration.QueryParameters.Limit = _searchCount;
               configuration.QueryParameters.Fields = [ItemFields.SortName];
               configuration.QueryParameters.Recursive = true;
               configuration.QueryParameters.IsMissing = false;
               configuration.QueryParameters.ImageTypeLimit = 1;
               configuration.QueryParameters.EnableTotalRecordCount = false;
               configuration.QueryParameters.IncludeItemTypes = [ BaseItemKind.MusicAlbum ];
           }, cancellationToken).ConfigureAwait(false);
        
        if (queryResult2?.Items == null)
            return searchResults;

        foreach (var baseItem in queryResult2.Items)
        {
            if (baseItem.Id == null)
                continue;

            var album = new Models.Search()
            {
                Id = baseItem.Id.GetValueOrDefault(),
                Type = SearchType.Artist,
                ArtistName = baseItem.AlbumArtist ?? string.Empty,
                AlbumName = baseItem.Name ?? string.Empty,
                AlbumId = baseItem.Id.GetValueOrDefault(),
                HasArtwork = baseItem.ImageTags?.AdditionalData.ContainsKey(ImageType.Primary.ToString()) == true
            };
            
            searchResults.Add(album);
        }
        
        return searchResults;
    }
    
    /// <summary>
    /// Search available tracks by search criteria
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<List<Models.Search>> SearchTrack(string value, CancellationToken cancellationToken = default)
    {
        var searchResults = new List<Models.Search>();

        var queryResult = await _jellyfinApiClient.Items.GetAsync(configuration =>
        {
            configuration.QueryParameters.ParentId = _collectionId;
            configuration.QueryParameters.Limit = _searchCount;
            configuration.QueryParameters.Fields = [ItemFields.SortName, ItemFields.PrimaryImageAspectRatio, ItemFields.CanDelete, ItemFields.MediaSourceCount ];
            configuration.QueryParameters.Recursive = true;
            configuration.QueryParameters.IsMissing = false;
            configuration.QueryParameters.ImageTypeLimit = 1;
            configuration.QueryParameters.EnableTotalRecordCount = false;
            configuration.QueryParameters.SearchTerm = value;
            configuration.QueryParameters.IncludeItemTypes = [ BaseItemKind.Audio ];
        }, cancellationToken).ConfigureAwait(false);
        
        if (queryResult?.Items == null)
            return searchResults;
        
        foreach (var baseItem in queryResult.Items)
        {
            if (baseItem.Id == null || baseItem.Name == null || baseItem.AlbumArtist == null)
                continue;

            var track = new Models.Search()
            {
                Id = baseItem.Id.GetValueOrDefault(),
                Type = SearchType.Track,
                ArtistName = baseItem.AlbumArtist ?? string.Empty,
                AlbumName = baseItem.Album ?? string.Empty,
                TrackName = baseItem.Name ?? string.Empty,
                AlbumId = baseItem.AlbumId.GetValueOrDefault(),
                HasArtwork = !string.IsNullOrWhiteSpace(baseItem.AlbumPrimaryImageTag)
            };
            
            searchResults.Add(track);
        }
        
        return searchResults;
    }

    /// <summary>
    /// Get all available collections
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task<List<Models.Collection>> GetCollectionsAsync(Enums.CollectionType type)
    {
        var collectionResult = new List<Models.Collection>();
        var queryResult = await _jellyfinApiClient.Items.GetAsync().ConfigureAwait(false);

        if (queryResult?.Items == null) return collectionResult;

        var collectionType = type == Enums.CollectionType.Audio
            ? BaseItemDto_CollectionType.Music
            : BaseItemDto_CollectionType.Playlists;
        
        foreach (var baseItem in queryResult.Items.Where(i => i.CollectionType == collectionType))
        {
            if (baseItem.CollectionType == null || baseItem.Id == null || baseItem.Name == null)
                continue;
            
            collectionResult.Add(new Models.Collection()
            {
                Id = baseItem.Id.Value,
                Name = baseItem.Name
            });
        }

        return collectionResult;
    }
    
    /// <summary>
    /// Get all available artists/albums from collection
    /// </summary>
    /// <param name="startIndex">Start index for list</param>
    /// <param name="count">Count of items to get from list</param>
    /// <returns></returns>
    public async Task<List<Models.Album>> GetArtistsAndAlbumsAsync(int? startIndex = null, int? count = null)
    {
        var albumResult = new List<Models.Album>();
        var queryResult = await _jellyfinApiClient.Items.GetAsync(configuration =>
        {
            configuration.QueryParameters.StartIndex = startIndex;
            configuration.QueryParameters.Limit = count;
            configuration.QueryParameters.Recursive = true;
            configuration.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.SortName];
            configuration.QueryParameters.ParentId = _collectionId;
            configuration.QueryParameters.IncludeItemTypes = [ BaseItemKind.MusicAlbum ];
        }).ConfigureAwait(false);

        if (queryResult?.Items == null)
            return albumResult;

        foreach (var baseItem in queryResult.Items)
        {
            if (baseItem.Id == null || baseItem.Name == null || baseItem.AlbumArtist == null)
                continue;

            albumResult.Add(new Models.Album()
            {
                Id = baseItem.Id.Value,
                Artist = baseItem.AlbumArtist,
                Name = baseItem.Name,
                Year = baseItem.ProductionYear,
                Runtime = baseItem.RunTimeTicks.HasValue ? new TimeSpan(baseItem.RunTimeTicks.Value) : null,
                HasArtwork = baseItem.ImageTags?.AdditionalData.ContainsKey(ImageType.Primary.ToString()) == true
            });
        }

        return albumResult.OrderBy(a => a.Name).ToList();
    }

    /// <summary>
    /// Get single album
    /// </summary>
    /// <param name="albumId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Album> GetAlbumAsync(Guid albumId, CancellationToken token)
    {
        var baseItem = await _jellyfinApiClient.Items[albumId].GetAsync().ConfigureAwait(false);

        if (baseItem == null)
            throw new ArgumentException($"No album found with id {albumId}");

        var albumResult = new Models.Album()
        {
            Id = baseItem.Id.GetValueOrDefault(),
            Artist = baseItem.AlbumArtist ?? "",
            Name = baseItem.Name ?? "",
            Year = baseItem.ProductionYear,
            Runtime = baseItem.RunTimeTicks.HasValue ? new TimeSpan(baseItem.RunTimeTicks.Value) : null,
            HasArtwork = baseItem.ImageTags?.AdditionalData.ContainsKey(ImageType.Primary.ToString()) == true
        };
        
        return albumResult;
    }

    /// <summary>
    /// Get all tracks from album
    /// </summary>
    /// <param name="albumId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<List<Track>> GetTracksAsync(Guid albumId, CancellationToken token)
    {
        var trackResult = new List<Models.Track>();
        var queryResult = await _jellyfinApiClient.Items.GetAsync(configuration =>
        {
            configuration.QueryParameters.Fields = [ItemFields.SortName];
            configuration.QueryParameters.ParentId = albumId;
        }).ConfigureAwait(false);

        if (queryResult?.Items == null)
            return trackResult;
        
        foreach (var baseItem in queryResult.Items)
        {
            if (baseItem.Id == null || baseItem.Name == null && !baseItem.IndexNumber.HasValue || !baseItem.AlbumId.HasValue)
                continue;

            trackResult.Add(new Models.Track()
            {
                Id = baseItem.Id.GetValueOrDefault(),
                Number = baseItem.IndexNumber.Value,
                Name = baseItem.Name,
                Artist = baseItem.AlbumArtist ?? string.Empty,
                Album = baseItem.Album ?? string.Empty,
                RunTime = baseItem.RunTimeTicks.HasValue ? TimeSpan.FromTicks(baseItem.RunTimeTicks.Value) : null,
                AlbumId = baseItem.AlbumId.Value,
                HasArtwork = !string.IsNullOrWhiteSpace(baseItem.AlbumPrimaryImageTag),
                HasLyrics = baseItem.HasLyrics ?? false,
            });
        }
        
        return trackResult.OrderBy(t => t.Number).ToList();
    }

    /// <summary>
    /// Get single track
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Track> GetTrackAsync(Guid trackId)
    {
        var baseItem = await _jellyfinApiClient.Items[trackId].GetAsync().ConfigureAwait(false);
        
        if (baseItem == null)
            throw new ArgumentException($"No track found with id {trackId}");
        
        if (baseItem.Id == null || baseItem.Name == null && !baseItem.IndexNumber.HasValue || !baseItem.AlbumId.HasValue)
            throw new ArgumentException($"Missing required data from track id {trackId}");
        
        var trackResult = new Models.Track()
        {
            Id = baseItem.Id.GetValueOrDefault(),
            Number = baseItem.IndexNumber.Value,
            Name = baseItem.Name,
            Artist = baseItem.AlbumArtist ?? string.Empty,
            Album = baseItem.Album ?? string.Empty,
            RunTime = baseItem.RunTimeTicks.HasValue ? TimeSpan.FromTicks(baseItem.RunTimeTicks.Value) : null,
            AlbumId = baseItem.AlbumId.Value,
            HasArtwork = !string.IsNullOrWhiteSpace(baseItem.AlbumPrimaryImageTag),
            HasLyrics = baseItem.HasLyrics ?? false,
        };
        
        return trackResult;
    }

    /// <summary>
    /// Download album art
    /// </summary>
    /// <param name="albumId"></param>
    /// <returns></returns>
    public async Task<byte[]?> GetPrimaryArtAsync(Guid albumId)
    {
        try
        {
            await using var stream = await _jellyfinApiClient.Items[albumId].Images[ImageType.Primary.ToString()].GetAsync(configuration =>
            {
                configuration.QueryParameters.Height = 200;
                configuration.QueryParameters.Width = 200;
            }).ConfigureAwait(true);
            
            if (stream == null)
                return null;
        
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (Exception e)
        {
            return null;
        }
    }

    /// <summary>
    /// Get track lyrics
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public async Task<string?> GetTrackLyricsAsync(Guid trackId)
    {
        var lyrics = await _jellyfinApiClient.Audio[trackId].Lyrics.GetAsync().ConfigureAwait(true);
        
        if (lyrics == null || lyrics.Lyrics == null)
            return null;

        var result = "";
        foreach (var lyricRow in lyrics.Lyrics)
        {
            if (lyricRow.Text != null)
                result += $"{lyricRow.Text}{Environment.NewLine}";
        }
        
        return result;
    }
    
    /// <summary>
    /// Get audio stream for playing
    /// </summary>
    /// <param name="trackId"></param>
    /// <returns></returns>
    public async Task<Stream?> GetAudioStreamAsync(Guid trackId)
    {
        return await _jellyfinApiClient.Items[trackId].Download.GetAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Get single playlist
    /// </summary>
    /// <param name="playlistId"></param>
    /// <returns></returns>
    public async Task<Playlist> GetPlaylistAsync(Guid playlistId)
    {
        var baseItem = await _jellyfinApiClient.Items[playlistId].GetAsync();
        TimeSpan? duration = baseItem.RunTimeTicks.HasValue ? TimeSpan.FromTicks(baseItem.RunTimeTicks.Value) : null;
        
        var playlist = new Playlist()
        {
            Id = baseItem.Id.Value,
            Name = baseItem.Name ?? string.Empty,
            Duration = duration,
            TrackCount = baseItem.ChildCount ?? 0,
            HasArtwork = baseItem.ImageTags?.AdditionalData.ContainsKey(ImageType.Primary.ToString()) == true
        };
        
        return playlist;
    }

    /// <summary>
    /// Get available tracks from playlist
    /// </summary>
    /// <param name="playlistId"></param>
    /// <returns></returns>
    public async Task<List<Track>> GetPlaylistTracksAsync(Guid playlistId)
    {
        var trackResult = new List<Track>();

        var queryResult = await _jellyfinApiClient.Playlists[playlistId].Items.GetAsync(configuration =>
        {
            configuration.QueryParameters.Fields = [ItemFields.SortName];
        }).ConfigureAwait(false);

        if (queryResult?.Items == null)
            return trackResult;
        
        foreach (var baseItem in queryResult.Items)
        {
            if (baseItem.Id == null || baseItem.Name == null && !baseItem.IndexNumber.HasValue || !baseItem.AlbumId.HasValue)
                continue;

            trackResult.Add(new Models.Track()
            {
                Id = baseItem.Id.GetValueOrDefault(),
                Number = baseItem.IndexNumber.Value,
                Name = baseItem.Name,
                Artist = baseItem.AlbumArtist ?? string.Empty,
                Album = baseItem.Album ?? string.Empty,
                RunTime = baseItem.RunTimeTicks.HasValue ? TimeSpan.FromTicks(baseItem.RunTimeTicks.Value) : null,
                AlbumId = baseItem.AlbumId.Value,
                HasArtwork = !string.IsNullOrWhiteSpace(baseItem.AlbumPrimaryImageTag),
                HasLyrics = baseItem.HasLyrics ?? false,
            });
        }
        
        return trackResult;
    }

    /// <summary>
    /// Get url for audio streaming
    /// </summary>
    /// <param name="sessiondId"></param>
    /// <param name="trackId"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public string GetAudioStreamUrl(string sessiondId, Guid trackId, int? position)
    {
        long? ticks = position.HasValue ? TimeSpan.FromMilliseconds(position.Value).Ticks : null;

        var information = _jellyfinApiClient.Audio[trackId].Universal.ToGetRequestInformation(configuration =>
        {
            configuration.QueryParameters.TranscodingContainer = "mp4";
            configuration.QueryParameters.TranscodingProtocol = MediaStreamProtocol.Hls;
            configuration.QueryParameters.AudioCodec = "aac";
            configuration.QueryParameters.Container = ["opus", "webm|opus", "mp3", "aac", "m4a|aac", "m4b|aac", "flac", "wav", "ogg", "mp4", "flac"];
            configuration.QueryParameters.EnableAudioVbrEncoding = true;
            configuration.QueryParameters.EnableRedirection = false;
            configuration.QueryParameters.EnableRemoteMedia = true;
            configuration.QueryParameters.UserId = _userId;
            configuration.QueryParameters.StartTimeTicks = ticks;
        });

        // PlaySessionId is part of the api, but not updated to library
        var url = _jellyfinApiClient.BuildUri(information);
        return $"{url}&api_key={_sdkClientSettings.AccessToken}&PlaySessionId={sessiondId}";
    }
    
    /// <summary>
    /// Send server information about starting playback
    /// </summary>
    /// <param name="trackId"></param>
    public async Task<string> StartPlaybackAsync(Guid trackId)
    {
        var body1 = new PlaybackInfoDto()
        {
            UserId = _userId,
            StartTimeTicks = TimeSpan.FromSeconds(0).Ticks,
            AllowAudioStreamCopy = true,
            EnableTranscoding = true,
        };
        
        var result = await _jellyfinApiClient.Items[trackId].PlaybackInfo.PostAsync(body1).ConfigureAwait(true);
        
        if (result == null)
            throw new Exception("Could not get PlaySessionId");
        
        var body2 = new PlaybackStartInfo()
        {
            ItemId = trackId,
            PlayMethod = PlaybackStartInfo_PlayMethod.Transcode,
            PositionTicks = 0,
            CanSeek = true,
            SessionId = _sessionId,
            PlaySessionId = result.PlaySessionId
        };

        await _jellyfinApiClient.Sessions.Playing.PostAsync(body2).ConfigureAwait(true);
        return result.PlaySessionId;
    }

    /// <summary>
    /// Send server information about pausing playback 
    /// </summary>
    /// <param name="sessiondId"></param>
    /// <param name="trackId"></param>
    /// <param name="position"></param>
    public async Task PausePlaybackAsync(string sessiondId, Guid trackId, int? position)
    {
        long? ticks = position.HasValue ? TimeSpan.FromMilliseconds(position.Value).Ticks : null;

        var body = new PlaybackStartInfo()
        {
            ItemId = trackId,
            PlayMethod = PlaybackStartInfo_PlayMethod.Transcode,
            IsPaused = true,
            PositionTicks = ticks,
            CanSeek = true,
            PlaySessionId = sessiondId,
            SessionId = _sessionId,
        };
        
        await _jellyfinApiClient.Sessions.Playing.PostAsync(body).ConfigureAwait(true);
    }

    /// <summary>
    /// Send server information about resuming playback 
    /// </summary>
    /// <param name="sessiondId"></param>
    /// <param name="trackId"></param>
    /// <param name="position"></param>
    public async Task ResumePlaybackAsync(string sessiondId, Guid trackId, int? position)
    {
        long? ticks = position.HasValue ? TimeSpan.FromMilliseconds(position.Value).Ticks : null;
        
        var body = new PlaybackStartInfo()
        {
            ItemId = trackId,
            PlayMethod = PlaybackStartInfo_PlayMethod.Transcode,
            IsPaused = false,
            PositionTicks = ticks,
            CanSeek = true,
            PlaySessionId = sessiondId,
            SessionId = _sessionId,
        };

        await _jellyfinApiClient.Sessions.Playing.PostAsync(body).ConfigureAwait(true);
    }

    /// <summary>
    /// Get available playlists
    /// </summary>
    /// <param name="collectionId"></param>
    /// <returns></returns>
    public async Task<List<Playlist>> GetPlaylistsAsync(Guid collectionId)
    {
        var playlistResults = new  List<Playlist>();
        
        var queryResult = await _jellyfinApiClient.Items.GetAsync(configuration =>
        {
            configuration.QueryParameters.ParentId = collectionId;
            configuration.QueryParameters.Fields = [ItemFields.SortName, ItemFields.ChildCount ];
            configuration.QueryParameters.Recursive = true;
        }).ConfigureAwait(false);
        
        if (queryResult?.Items == null)
            return playlistResults;
        
        foreach (var baseItem in queryResult.Items)
        {
            if (baseItem.Id == null)
                continue;

            TimeSpan? duration = baseItem.RunTimeTicks.HasValue ? TimeSpan.FromTicks(baseItem.RunTimeTicks.Value) : null;
            
            var playlist = new Playlist()
            {
                Id = baseItem.Id.Value,
                Name = baseItem.Name ?? string.Empty,
                Duration = duration,
                TrackCount = baseItem.ChildCount ?? 0,
                HasArtwork = baseItem.ImageTags?.AdditionalData.ContainsKey(ImageType.Primary.ToString()) == true
            };
            
            playlistResults.Add(playlist);
        }

        return playlistResults;
    }

    /// <summary>
    /// Get url for primary art
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Uri? GetPrimaryArtUrl(Guid id)
    {
        var information = _jellyfinApiClient.Items[id].Images[ImageType.Primary.ToString()]
            .ToGetRequestInformation(configuration =>
            {
                configuration.QueryParameters.Height = 200;
                configuration.QueryParameters.Width = 200;
            });
        
        return _jellyfinApiClient.BuildUri(information);
    }

    /// <summary>
    /// Get url used for websocket connection
    /// </summary>
    /// <returns></returns>
    public string GetWebsocketUrl()
    {
        var root = _sdkClientSettings.ServerUrl.StartsWith("http") ? "ws" :  "wss";
        var rootUrl = _sdkClientSettings.ServerUrl.Replace("http", root).Replace("https", root);
        
        return $"{rootUrl}/socket?api_key={_sdkClientSettings.AccessToken}&deviceId={_deviceId}";
    }

    public async Task SeekPlaybackAsync(string sessiondId, Guid trackId, int? position)
    {
        var body = new GeneralCommand()
        {
            Name = GeneralCommand_Name.Play,
            ControllingUserId = _userId,
            Arguments = new GeneralCommand_Arguments()
            {
                AdditionalData =  new Dictionary<string, object>()
                {
                    { "ItemIds", new[] { trackId } },
                    { "PlayCommand", "PlayNow" },
                    { "StartPositionTicks", position ?? 0 }   
                }
            }
        };

        await _jellyfinApiClient.Sessions[sessiondId].Command.PostAsync(body).ConfigureAwait(false);
    }

    /// <summary>
    /// Send server information about stopping playback
    /// </summary>
    /// <param name="sessiondId"></param>
    /// <param name="trackId"></param>
    public async Task StopPlaybackAsync(string sessiondId, Guid trackId)
    {
        var body = new PlaybackStopInfo()
        {
            ItemId = trackId,
            PlaySessionId = sessiondId,
            SessionId = _sessionId,
        };
        
        await _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(body).ConfigureAwait(true);
    }
}