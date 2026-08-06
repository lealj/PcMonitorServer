namespace PcMonitorServer.Models
{
    public sealed class MonitoredServicesOptions
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public string? Url { get; init; }
        public bool Enabled { get; init; } = true;
    }

    public sealed class ApplicationStatus
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public bool IsHealthy { get; init; }
        public int? StatusCode { get; init; }
        public long? ResponseTimeMs { get; init; }
        public string? Message { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
    }
}
