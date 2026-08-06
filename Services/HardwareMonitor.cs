using LibreHardwareMonitor.Hardware;
using PcMonitorServer.Models;
using System.Diagnostics;

namespace PcMonitorServer.Services;

/// <summary>
/// Collects hardware metrics such as CPU usage, temperatures, memory, and storage.
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor;
    private bool _disposed;

    public HardwareMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsMemoryEnabled = true,
            IsGpuEnabled = true,
            IsStorageEnabled = true,
            IsMotherboardEnabled = true,
            IsNetworkEnabled = true,
            IsControllerEnabled = true,
        };

        _updateVisitor = new UpdateVisitor();

        _computer.Open();
    }

    public SystemStatus GetSystemStatus()
    {
        ThrowIfDisposed();

        _computer.Accept(_updateVisitor);

        float? cpuUsage = null;
        float? cpuTemperature = null;
        float? memoryUsage = null;
        var gpuStatuses = new List<GpuStatus>();
        var storageStatuses = new List<StorageStatus>();

        foreach (IHardware hardware in _computer.Hardware)
        {
            // Find cpu sensors and set cpu related variables
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                cpuUsage ??= FindSensorValue(
                    hardware,
                    SensorType.Load,
                    sensor => sensor.Name.Equals("CPU Total", StringComparison.OrdinalIgnoreCase)
                );

                cpuTemperature ??= FindSensorValue(
                    hardware,
                    SensorType.Temperature,
                    sensor => sensor.Name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase)
                );
            }

            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuIntel ||
                hardware.HardwareType == HardwareType.GpuAmd)
            {
                string _id = hardware.Identifier.ToString();
                float? _usage = FindOverallGpuUsage(hardware);
                float? _temperature = FindSensorValue(
                    hardware,
                    SensorType.Temperature,
                    sensor => sensor.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase)
                );

                gpuStatuses.Add(
                    new GpuStatus
                    {
                        Id = _id,
                        Usage = _usage,
                        Temperature = _temperature,
                    }
                );
            }

            // Find mem sensors and set memory related variables
            if (hardware.HardwareType == HardwareType.Memory)
            {
                if (hardware.Name.Equals("Total Memory", StringComparison.OrdinalIgnoreCase))
                {
                    memoryUsage ??= FindSensorValue(
                        hardware,
                        SensorType.Load,
                        sensor => sensor.Name.Equals("Memory", StringComparison.OrdinalIgnoreCase)
                    );
                }
            }

            // Find storage sensors and set storage related variables
            if (hardware.HardwareType == HardwareType.Storage)
            {
                string _id = hardware.Identifier.ToString();
                // Ssd uses composite, hdd uses temperature
                float? _temperature =
                    FindSensorValue(
                        hardware,
                        SensorType.Temperature,
                        sensor => sensor.Name.Equals("Composite Temperature", StringComparison.OrdinalIgnoreCase)
                    ) ?? FindSensorValue(
                        hardware,
                        SensorType.Temperature,
                        sensor => sensor.Name.Equals("Temperature", StringComparison.OrdinalIgnoreCase)
                    );
                float? _lifePercentage = FindSensorValue(
                    hardware, SensorType.Level,
                    sensor => sensor.Name.Equals("Life", StringComparison.OrdinalIgnoreCase)
                );
                float? _usedSpace = FindSensorValue(
                    hardware, SensorType.Load,
                    sensor => sensor.Name.Equals("Used Space", StringComparison.OrdinalIgnoreCase)
                );
                float? _freeSpace = FindSensorValue(
                    hardware, SensorType.Data,
                    sensor => sensor.Name.Equals("Total Space", StringComparison.OrdinalIgnoreCase)
                );

                storageStatuses.Add(new StorageStatus
                {
                    Id = _id,
                    Temperature = _temperature,
                    LifePercentage = _lifePercentage,
                    UsedSpace = _usedSpace,
                    FreeSpace = _freeSpace
                });
            }
        }

        /******* DEBUG: ********/
        //DebugAvailableHardware();
        //DebugAvailableCpuSensors();
        DebugAvailableStorageSensors();
        //DebugAvailableGpuSensors();

        return new SystemStatus
        {
            CpuUsage = cpuUsage,
            CpuTemperature = cpuTemperature,
            MemoryUsage = memoryUsage,
            GpuStatuses = gpuStatuses,
            StorageStatuses = storageStatuses,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public SystemInfo GetSystemInfo()
    {
        ThrowIfDisposed();

        string? cpuName = null;
        string? moboName = null;
        var gpuNames = new List<GpuInfo>();
        var storageNames = new List<StorageInfo>();

        foreach (IHardware hardware in _computer.Hardware)
        {
            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    cpuName ??= hardware.Name;
                    break;

                case HardwareType.Motherboard:
                    moboName ??= hardware.Name;
                    break;

                case HardwareType.GpuAmd:
                case HardwareType.GpuNvidia:
                case HardwareType.GpuIntel:
                    gpuNames.Add(new GpuInfo { Id = hardware.Identifier.ToString(), Name = hardware.Name });
                    break;

                case HardwareType.Storage:
                    storageNames.Add(new StorageInfo { Id = hardware.Identifier.ToString(), Name = hardware.Name });
                    break;

            }
        }

        return new SystemInfo
        {
            CpuName = cpuName,
            MoboName = moboName,
            GpuNames = gpuNames,
            StorageNames = storageNames
        };
    }

    private static float? FindSensorValue(
        IHardware hardware,
        SensorType sensorType,
        Func<ISensor, bool>? predicate = null)
    {
        ISensor? sensor = hardware.Sensors.FirstOrDefault(sensor =>
            sensor.SensorType == sensorType &&
            sensor.Value.HasValue &&
            (predicate is null || predicate(sensor)));

        if (sensor?.Value is float value)
        {
            return value;
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            float? subHardwareValue = FindSensorValue(subHardware, sensorType, predicate);
            if (subHardwareValue.HasValue)
            {
                return subHardwareValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the highest utilization reported by the GPU's Direct3D (D3D) engines. Windows Task Manager reports
    /// overall GPU usage using the busiest D3D engine rather than the GPU core load, so this provides a value
    /// that closely matches the percentage displayed in Task Manager.
    /// </summary>
    /// <param name="hardware">
    /// The GPU hardware whose D3D engine sensors will be examined.
    /// </param>
    /// <returns>
    /// The highest D3D engine utilization percentage, or <c>null</c> if no D3D engine load sensors are available.
    /// </returns>
    private static float? FindOverallGpuUsage(IHardware hardware)
    {
        float? highestUsage = null;
        foreach (ISensor sensor in hardware.Sensors)
        {
            bool isD3dEngine =
                sensor.SensorType == SensorType.Load &&
                sensor.Name.StartsWith("D3D ", StringComparison.OrdinalIgnoreCase);

            if (!isD3dEngine || sensor.Value is null)
            {
                continue;
            }

            highestUsage = highestUsage is null ? sensor.Value : Math.Max(highestUsage.Value, sensor.Value.Value);
        }
        return highestUsage;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _computer.Close();
        _disposed = true;
    }

    // Debug print
    private void DebugAvailableHardware()
    {
        foreach (IHardware hardware in _computer.Hardware)
        {
            hardware.Update();

            Debug.WriteLine(
                $"Hardware: {hardware.Name} | Type: {hardware.HardwareType}");

            if (hardware.HardwareType == HardwareType.Memory)
            {
                Debug.WriteLine("Memory Sensors:");

                foreach (ISensor sensor in hardware.Sensors)
                {
                    Debug.WriteLine(
                        $"{sensor.Name} | {sensor.SensorType} | {sensor.Value}");
                }
            }
        }
    }

    private void DebugAvailableCpuSensors()
    {
        foreach (IHardware hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu)
            {
                continue;
            }

            Debug.WriteLine($"CPU hardware: {hardware.Name}");
            PrintSensorsRecursively(hardware);
        }
    }

    private void DebugAvailableStorageSensors()
    {
        foreach (IHardware hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Storage)
            {
                continue;
            }
            Debug.WriteLine($"Storage: {hardware.Name}");
            PrintSensorsRecursively(hardware);
        }
    }

    private void DebugAvailableGpuSensors()
    {
        foreach (IHardware hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.GpuNvidia &&
                hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel)
            {
                continue;
            }

            Debug.WriteLine($"GPU: {hardware.Name}");
            PrintSensorsRecursively(hardware);
        }
    }

    private static void PrintSensorsRecursively(IHardware hardware)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            Debug.WriteLine(
                $"{hardware.Name} | {sensor.Name} | " +
                $"{sensor.SensorType} | {sensor.Value}");
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            PrintSensorsRecursively(subHardware);
        }
    }
}

/*
 * IDisposable - Lets object clean up resources below when no longer needed.
 * C# garbage collector doesn't know how to deal with these by default:
 * -Open files
 * -Network sockets
 * -Database connections
 * -Windows handles
 * -Hardware devices
 * -Timers
 * -Objects from native (C/C++) libraries
*/
