using LibreHardwareMonitor.Hardware;

namespace PcMonitorServer.Services;

/// <summary>
/// Updates every enabled hardware device and all of its subhardware.
/// </summary>
public sealed class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor)
    {
        // Sensors are updated by their containing hardware.
    }

    public void VisitParameter(IParameter parameter)
    {
        // No parameter processing is required.
    }
}