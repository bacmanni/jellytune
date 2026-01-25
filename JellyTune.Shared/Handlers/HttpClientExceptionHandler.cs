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
                    if (tries == totalRetries)
                        return response;
                    
                    Console.WriteLine($"Request failed with status code {response.StatusCode}");
                    tries++;
                    continue;
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed with: {ex.Message}");

                if (tries == totalRetries)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(string.Empty) };
                }
                
                tries++;
            }
        }

        throw new ApplicationException("HTTP request failed");
    }
}