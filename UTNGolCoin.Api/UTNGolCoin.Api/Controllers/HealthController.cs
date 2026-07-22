using Microsoft.AspNetCore.Mvc;

namespace UTNGolCoin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>Chequeo simple de que el servicio está arriba y respondiendo.</summary>
        /// <response code="200">El servicio está funcionando.</response>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
