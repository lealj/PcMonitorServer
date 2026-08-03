using LibreHardwareMonitor.Hardware;
using PcMonitorServer.Models;

namespace PcMonitorServer.Services;

/// <summary>
/// Collects hardware metrics such as CPU usage, temperatures, memory, and storage.
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
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

        _computer.Open();
    }

    public SystemStatus GetSystemStatus()
    {
        ThrowIfDisposed();

        float? cpuUsage = null;
        float? cpuTemperature = null;
        float? memoryUsage = null;

        foreach (IHardware hardware in _computer.Hardware)
        {
            hardware.Update();
            UpdateSubHardware(hardware);

            if (hardware.HardwareType == HardwareType.cpu)
            {
                cpuUsage ??= FindSensorValue(
                    
                    )
            }

        }
    }

    private static void UpdateSubHardware(IHardware hardware)
    {
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            UpdateSubHardware(subHardware);
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
