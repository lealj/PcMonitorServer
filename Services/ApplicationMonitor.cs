using Microsoft.Extensions.Options;
using PcMonitorServer.Models;
using System.Diagnostics;

namespace PcMonitorServer.Services;

public sealed class ApplicationMonitor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IReadOnlyList<MonitoredServicesOptions> _monitoredServices;
    private readonly ILogger<ApplicationMonitor> _logger;

    public ApplicationMonitor(
        IHttpClientFactory httpClientFactory,
        IOptions<List<MonitoredServicesOptions>> options,
        ILogger<ApplicationMonitor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _monitoredServices = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks every configured application and returns their current status.
    /// </summary>
    public async Task<IReadOnlyList<ApplicationStatus>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var applicationStatuses = new List<ApplicationStatus>();
        foreach (MonitoredServicesOptions service in _monitoredServices)
        {
            if (!service.Enabled)
            {
                continue;
            }

            // check application async
            ApplicationStatus status = await CheckApplicationAsync(
                service.Url,
                service.Id,
                service.DisplayName,
                cancellationToken);
            applicationStatuses.Add(status);
        }

        return applicationStatuses;
    }

    /// <summary>
    /// Performs a health check against a single application.
    /// </summary>
    private async Task<ApplicationStatus> CheckApplicationAsync(
        string? url,
        string id,
        string name,
        CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient("ApplicationHealthCheck");

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            using HttpResponseMessage res = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            stopwatch.Stop();

            return new ApplicationStatus
            {
                Id = id,
                Name = name,
                IsHealthy = true,
                StatusCode = (int)res.StatusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Message = res.IsSuccessStatusCode ? "Success" : $"{(int)res.StatusCode}",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (OperationCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                ex,
                "Could not connect to {ApplicationName} at {Url}",
                name,
                url);

            return CreateFailureStatus(
                id,
                name,
                stopwatch.ElapsedMilliseconds,
                "Application health check timed out");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                ex,
                "Could not connect to {ApplicationName} at {Url}",
                name,
                url);

            return CreateFailureStatus(
                id,
                name,
                stopwatch.ElapsedMilliseconds,
                "Application could not be reached");
        }
    }

    private static ApplicationStatus CreateFailureStatus(
        string id,
        string name,
        long responseTimeMs,
        string message)
    {
        return new ApplicationStatus
        {
            Id = id,
            Name = name,
            IsHealthy = false,
            StatusCode = null,
            ResponseTimeMs = responseTimeMs,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
