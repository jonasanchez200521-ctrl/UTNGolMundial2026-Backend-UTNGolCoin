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

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerPorUsuario(int usuarioId)
        {
            var predicciones = await _prediccionService.ObtenerPorUsuarioIdAsync(usuarioId);
            return Ok(predicciones);
        }
    }
}
