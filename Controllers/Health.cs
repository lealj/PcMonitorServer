using Microsoft.AspNetCore.Mvc;
using PcMonitorServer.Models;

namespace PcMonitorServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ServerStatus> GetHealth()
    {
        var status = new ServerStatus
        {
            Status = "running",
            Timestamp = DateTimeOffset.UtcNow
        };

        return Ok(status);
    }
}