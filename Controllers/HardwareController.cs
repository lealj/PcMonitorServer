using Microsoft.AspNetCore.Mvc;
using PcMonitorServer.Models;
using PcMonitorServer.Services;

namespace PcMonitorServer.Controllers;

/// <summary>
/// Handles HTTP requests related to hardware information.
///
/// A controller serves as the entry point into the application for incoming
/// HTTP requests. Its primary responsibility is to:
///     1. Receive an HTTP request.
///     2. Validate or interpret any request data.
///     3. Delegate business logic to one or more services.
///     4. Return an HTTP response.
///
/// Controllers should contain very little business logic. Their purpose is
/// simply to coordinate requests and responses. Any code responsible for
/// actually retrieving hardware information belongs in HardwareMonitor rather
/// than in this class.
///
/// This controller exposes the "/api/hardware" endpoint, allowing external
/// clients (such as desktop applications, mobile apps, or web frontends) to
/// retrieve the computer's current hardware status.
/// </summary>

[ApiController]
[Route("api/[controller]")]
public sealed class HardwareController : ControllerBase
{
    private readonly HardwareMonitor _hardwareMonitor;

    public HardwareController(HardwareMonitor hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;
    }

    [HttpGet("status")]
    public ActionResult<SystemStatus> GetHardwareStatus()
    {
        SystemStatus status = _hardwareMonitor.GetSystemStatus();
        return Ok(status);
    }

    [HttpGet("info")]
    public ActionResult<SystemInfo> GetHardwareInfo()
    {
        SystemInfo info = _hardwareMonitor.GetSystemInfo();
        return Ok(info);
    }
}
