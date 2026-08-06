namespace PcMonitorServer.Models
{
    public sealed class SystemStatus
    {
        public float? CpuUsage { get; init; }
        public float? CpuTemperature { get; init; }
        public float? MemoryUsage { get; init; }

        public IReadOnlyList<GpuStatus> GpuStatuses { get; init; } = [];
        public IReadOnlyList<StorageStatus> StorageStatuses { get; init; } = [];
        public DateTimeOffset Timestamp { get; init; }
    }

    public sealed class GpuStatus
    {
        public required string Id { get; init; }
        public float? Usage { get; init; }
        public float? Temperature { get; init; }
    }

    public sealed class StorageStatus
    {
        public required string Id { get; init; }
        public float? Temperature { get; init; }
        public float? LifePercentage { get; init; }
        public float? UsedSpace { get; init; }
        public float? FreeSpace { get; init; }
    }
}
