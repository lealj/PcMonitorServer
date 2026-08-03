namespace PcMonitorServer.Models
{
    public class SystemStatus
    {
        public float? CpuUsage { get; init; }
        public float? CpuTemperature { get; init; }
        public float? MemoryUsage { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}
