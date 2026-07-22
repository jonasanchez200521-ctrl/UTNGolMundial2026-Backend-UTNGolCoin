using Microsoft.AspNetCore.Mvc;
using UTNGolCoin.Api.Services;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrediccionesController : ControllerBase
    {
        private readonly PrediccionService _prediccionService;

        public PrediccionesController(PrediccionService prediccionService)
        {
            _prediccionService = prediccionService;
        }

        /// <summary>Crea una apuesta 1X2 (LOCAL/EMPATE/VISITANTE) sobre un partido y descuenta el monto de la billetera del usuario.</summary>
        /// <remarks>
        /// Valida en orden: monto mayor a 0, pronóstico válido, que el partido no haya cerrado por hora (RF17),
        /// que el usuario tenga billetera, que el saldo alcance, y que no exista ya una predicción del usuario
        /// para ese partido (una apuesta por partido). <c>fechaInicioPartido</c> debe mandarse en UTC (formato ISO 8601 con "Z").
        /// </remarks>
        /// <response code="201">Predicción creada en estado PENDIENTE.</response>
        /// <response code="400">Datos inválidos, partido ya cerrado por hora, o saldo insuficiente.</response>
        /// <response code="404">El usuario no tiene billetera creada.</response>
        /// <response code="409">El usuario ya tiene una predicción para ese partido.</response>
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearPrediccionRequest request)
        {
            if (request.UsuarioId <= 0 || request.PartidoId <= 0)
            {
                return BadRequest(new { mensaje = "UsuarioId y PartidoId son requeridos y deben ser mayores a 0." });
            }

            try
            {
                var prediccion = await _prediccionService.CrearPrediccionAsync(
                    request.UsuarioId, request.PartidoId, request.Pronostico, request.Monto, request.FechaInicioPartido);

                return CreatedAtAction(nameof(ObtenerPorUsuario), new { usuarioId = prediccion.UsuarioId }, prediccion);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>Devuelve todas las predicciones de un usuario (más recientes primero), con su estado (PENDIENTE/GANADA/PERDIDA).</summary>
        /// <response code="200">Lista de predicciones (vacía si el usuario no apostó todavía).</response>
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerPorUsuario(int usuarioId)
        {
            var predicciones = await _prediccionService.ObtenerPorUsuarioIdAsync(usuarioId);
            return Ok(predicciones);
        }
    }
}
