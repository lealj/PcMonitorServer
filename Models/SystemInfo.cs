namespace PcMonitorServer.Models
{
    public class SystemInfo
    {
        public string? CpuName {  get; init; }
        public string? MoboName { get; init; }
        public IReadOnlyList<GpuInfo> GpuNames { get; init; } = []
        public IReadOnlyList<StorageInfo> StorageNames { get; init; } = [];
    }

    public sealed class GpuInfo
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
    }

    public sealed class StorageInfo
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
    }
}
