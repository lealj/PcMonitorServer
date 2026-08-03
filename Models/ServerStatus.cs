namespace PcMonitorServer.Models
{
    public class ServerStatus
    {
        public string Status { get; set; } = "unknown";
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
