using Microsoft.AspNetCore.Mvc;
using UTNGolCoin.Api.Services;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Controllers
{
    [ApiController]
    [Route("api/bonos")]
    public class BonosController : ControllerBase
    {
        private readonly BonoDiarioService _bonoDiarioService;

        public BonosController(BonoDiarioService bonoDiarioService)
        {
            _bonoDiarioService = bonoDiarioService;
        }

        // Body opcional: { "fecha": "2026-07-23" }. Sin body (o sin "fecha"), usa el día de hoy.
        // Mandar fechas distintas en llamadas sucesivas simula el paso de los días para la demo.
        [HttpPost("ejecutar-bono-diario")]
        public async Task<IActionResult> EjecutarBonoDiario([FromBody] EjecutarBonoDiarioRequest? request)
        {
            var resultado = await _bonoDiarioService.EjecutarBonoDiarioAsync(request?.Fecha);
            return Ok(resultado);
        }

        [HttpGet("estado/{usuarioId}")]
        public async Task<IActionResult> ObtenerEstado(int usuarioId)
        {
            try
            {
                var estado = await _bonoDiarioService.ObtenerEstadoAsync(usuarioId);
                return Ok(estado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }
    }
}
