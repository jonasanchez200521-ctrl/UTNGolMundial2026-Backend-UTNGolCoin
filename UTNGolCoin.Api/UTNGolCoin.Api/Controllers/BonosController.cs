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

        /// <summary>Bono antibancarrota (RF20): da 1 UTNGolCoin a todos los usuarios con saldo &lt;= 0 que no lo recibieron todavía en la fecha indicada.</summary>
        /// <remarks>
        /// Body opcional: <c>{ "fecha": "2026-07-23" }</c>. Sin body (o sin "fecha"), usa el día de hoy (UTC).
        /// Mandar fechas distintas en llamadas sucesivas simula el paso de los días para la demo, sin esperar 24hs reales.
        /// Idempotente por día gracias al índice único (usuarioId, fecha) de la tabla BonosDiarios.
        /// </remarks>
        /// <response code="200">Resumen con la cantidad de beneficiados y sus nuevos saldos (0 si nadie estaba en bancarrota o ya se les dio el bono esa fecha).</response>
        [HttpPost("ejecutar-bono-diario")]
        public async Task<IActionResult> EjecutarBonoDiario([FromBody] EjecutarBonoDiarioRequest? request)
        {
            var resultado = await _bonoDiarioService.EjecutarBonoDiarioAsync(request?.Fecha);
            return Ok(resultado);
        }

        /// <summary>Consulta si un usuario está en bancarrota (saldo &lt;= 0) y si ya recibió el bono diario hoy.</summary>
        /// <response code="200">Estado de bancarrota/bono del usuario.</response>
        /// <response code="404">El usuario no tiene billetera creada.</response>
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
