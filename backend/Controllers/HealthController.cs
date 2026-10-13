using Microsoft.AspNetCore.Mvc;

namespace SmartIotApi.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", service = "SmartX API" });
}
