using Microsoft.AspNetCore.Mvc;
using UTNGolCoin.Api.Services;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Controllers
{
    // Ruta exacta acordada con Alexis (backend de Estadísticas) para el webhook de resultados.
    // El mismo endpoint también sirve para disparar la liquidación manualmente desde Swagger.
    [ApiController]
    [Route("api/utngolcoin/liquidacion")]
    public class LiquidacionController : ControllerBase
    {
        private readonly LiquidacionService _liquidacionService;

        public LiquidacionController(LiquidacionService liquidacionService)
        {
            _liquidacionService = liquidacionService;
        }

        [HttpPost]
        public async Task<IActionResult> Liquidar([FromBody] LiquidacionRequest request)
        {
            if (request.PartidoId <= 0)
            {
                return BadRequest(new { mensaje = "PartidoId es requerido y debe ser mayor a 0." });
            }

            try
            {
                var resultado = await _liquidacionService.LiquidarAsync(request.PartidoId, request.Resultado);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
