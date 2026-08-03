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

        foreach (IHardware hardware in _computer.Hardware)
        {
            // Find cpu sensors and set cpu related variables
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                cpuUsage ??= FindSensorValue(
                    hardware,
                    SensorType.Load,
                    sensor => sensor.Name.Contains("CPU Total", StringComparison.OrdinalIgnoreCase)
                );

                cpuTemperature ??= FindSensorValue(
                    hardware,
                    SensorType.Temperature,
                    sensor => sensor.Name.Contains("CPU Package", StringComparison.OrdinalIgnoreCase)
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

            }
        }

        //DEBUG:
        DebugAvailableHardware();
        DebugAvailableCpuSensors();
        DebugAvailableStorageSensors();

        return new SystemStatus
        {
            CpuUsage = cpuUsage,
            CpuTemperature = cpuTemperature,
            MemoryUsage = memoryUsage,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }

    public SystemInfo GetSystemInfo()
    {
        ThrowIfDisposed();

        string? cpuName = null;
        string? moboName = null;
        var gpuNames = new List<string>();
        var storageNames = new List<string>();

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
                    gpuNames.Add(hardware.Name);
                    break;

                case HardwareType.Storage:
                    storageNames.Add(hardware.Name);
                    break;

            }
        }

        return new SystemInfo
        {
            CpuName = cpuName,
            // just changed models, need refactoring
        }

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
        foreach (IHardware hardware in this._computer.Hardware)
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
        foreach (IHardware hardware in this._computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Storage)
            {
                continue;
            }
            Debug.WriteLine($"Storage: {hardware.Name}");
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
