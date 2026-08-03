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


// Indicates that this class is an ASP.NET API controller.
//
// This attribute enables several API-specific behaviors, including:
//   • Automatic request validation.
//   • Consistent HTTP error responses.
//   • Improved parameter binding.
//   • Better integration with OpenAPI / Swagger.
//
// Nearly every API controller should have this attribute.
[ApiController]

// Defines the base route for every endpoint in this controller.
//
// "[controller]" is a placeholder that ASP.NET automatically replaces with
// the controller's class name, excluding the word "Controller".
//
// Example:
//
//      HardwareController
//              ↓
//      "Hardware"
//
// Therefore the base route becomes:
//
//      /api/hardware
//
// If another controller were named ProcessController, its base route would
// automatically become:
//
//      /api/process
//
// This avoids hardcoding route names throughout the application.
[Route("api/[controller]")]
public sealed class HardwareController : ControllerBase
{
    // Reference to the service responsible for communicating with
    // LibreHardwareMonitor.
    //
    // The controller never creates this object itself. Instead, ASP.NET's
    // dependency injection container provides an instance when constructing
    // the controller.
    //
    // Keeping this field readonly ensures that the controller cannot
    // accidentally replace the service after construction.
    private readonly HardwareMonitor _hardwareMonitor;

    // Constructor
    //
    // ASP.NET automatically calls this constructor whenever it needs to
    // create a HardwareController.
    //
    // Since Program.cs registered HardwareMonitor as a service:
    //
    //      builder.Services.AddSingleton<HardwareMonitor>();
    //
    // the framework knows how to supply the parameter below.
    //
    // Conceptually, ASP.NET performs something similar to:
    //
    //      HardwareMonitor monitor = ...;
    //      HardwareController controller =
    //          new HardwareController(monitor);
    //
    // You never manually instantiate controllers yourself.
    public HardwareController(HardwareMonitor hardwareMonitor)
    {
        _hardwareMonitor = hardwareMonitor;
    }

    [HttpGet]
    public ActionResult<SystemStatus> GetHardwareStatus()
    {
        // Delegate the work to the HardwareMonitor service.
        //
        // HardwareMonitor is responsible for interacting with
        // LibreHardwareMonitor, reading sensors, and constructing the
        // SystemStatus model.
        SystemStatus status = _hardwareMonitor.GetSystemStatus();

        // Returns an HTTP 200 ("OK") response containing the SystemStatus
        // object.
        //
        // ASP.NET automatically serializes the object into JSON before
        // sending it back to the client.
        //
        // For example:
        //
        // {
        //     "cpuUsagePercent": 17.4,
        //     "cpuTemperatureCelsius": 51.2,
        //     "memoryUsagePercent": 43.1,
        //     "timestamp": "2026-08-03T02:18:34Z"
        // }
        return Ok(status);
    }
}
