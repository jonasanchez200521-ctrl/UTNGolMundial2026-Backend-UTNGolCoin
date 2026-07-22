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

        /// <summary>Webhook de resultados de partido (RF12/RF19). Ruta exacta acordada con el backend de Estadísticas de Alexis.</summary>
        /// <remarks>
        /// Paga <c>monto × cuota</c> a las predicciones PENDIENTES que coincidan con el resultado (pasan a GANADA)
        /// y marca PERDIDA a las que no. Idempotente por diseño: solo toca predicciones PENDIENTES, así que llamar
        /// dos veces para el mismo <c>partidoId</c> no vuelve a pagar. Alexis puede mandar campos extra (ej. fase, grupo),
        /// se ignoran sin romper la petición. También sirve para disparo manual desde Swagger en la demo.
        /// </remarks>
        /// <response code="200">Resumen de la liquidación (0 liquidadas si no había pendientes o ya se había liquidado).</response>
        /// <response code="400">PartidoId inválido o resultado distinto de LOCAL/EMPATE/VISITANTE.</response>
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
