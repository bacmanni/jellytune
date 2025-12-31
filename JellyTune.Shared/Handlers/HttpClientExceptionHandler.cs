using System.Net;
using JellyTune.Shared.Services;

namespace JellyTune.Shared.Handlers;

public class HttpClientExceptionHandler : DelegatingHandler
{
    private readonly IConfigurationService _configurationService;

    public HttpClientExceptionHandler(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var totalRetries = _configurationService.Get().RetryCount;
        var tries = 0;
        
        while (tries <= totalRetries)
        {
            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Request failed with status code {response.StatusCode}");
                    throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
                }

                return response;
            }
            catch (Exception ex)
            {
                tries++;
            }
        }

        throw new ApplicationException("HTTP request failed");
    }
}