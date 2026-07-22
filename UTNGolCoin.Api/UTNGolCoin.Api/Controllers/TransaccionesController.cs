using Microsoft.AspNetCore.Mvc;
using UTNGolCoin.Api.Services;

namespace UTNGolCoin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransaccionesController : ControllerBase
    {
        private readonly TransaccionService _transaccionService;

        public TransaccionesController(TransaccionService transaccionService)
        {
            _transaccionService = transaccionService;
        }

        /// <summary>Historial de transacciones (RF14) de un usuario: bonos, predicciones y premios, más recientes primero.</summary>
        /// <response code="200">Lista de transacciones de la billetera del usuario.</response>
        /// <response code="404">El usuario no tiene billetera creada.</response>
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerPorUsuario(int usuarioId)
        {
            try
            {
                var transacciones = await _transaccionService.ObtenerPorUsuarioIdAsync(usuarioId);
                return Ok(transacciones);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
        }
    }
}
