using Microsoft.AspNetCore.Mvc;

namespace Payments.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() =>
        Ok(new
        {
            Status = "Healthy",
            Service = "PaymentsAPI",
            Description = "Consumes OrderPlacedEvent and publishes PaymentProcessedEvent via RabbitMQ",
            Timestamp = DateTime.UtcNow
        });
}
