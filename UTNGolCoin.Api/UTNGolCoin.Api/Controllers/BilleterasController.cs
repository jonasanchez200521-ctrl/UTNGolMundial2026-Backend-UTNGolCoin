using Microsoft.AspNetCore.Mvc;
using UTNGolCoin.Api.Services;
using UTNGolCoin.Api.Services.Dtos;

namespace UTNGolCoin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BilleterasController : ControllerBase
    {
        private readonly BilleteraService _billeteraService;

        public BilleterasController(BilleteraService billeteraService)
        {
            _billeteraService = billeteraService;
        }

        /// <summary>Crea la billetera de un usuario nuevo y le acredita el bono de bienvenida de 10 UTNGolCoin.</summary>
        /// <remarks>El usuario en sí no se crea acá (vive en el backend de Estadísticas de Alexis); esto solo crea su billetera local, referenciada por <c>usuarioId</c>.</remarks>
        /// <response code="201">Billetera creada con saldo 10.</response>
        /// <response code="400">UsuarioId falta o es menor o igual a 0.</response>
        /// <response code="409">El usuario ya tiene una billetera creada.</response>
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearBilleteraRequest request)
        {
            if (request.UsuarioId <= 0)
            {
                return BadRequest(new { mensaje = "UsuarioId es requerido y debe ser mayor a 0." });
            }

            try
            {
                var billetera = await _billeteraService.CrearBilleteraAsync(request.UsuarioId);
                return CreatedAtAction(nameof(ObtenerPorUsuario), new { usuarioId = billetera.UsuarioId }, billetera);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }

        /// <summary>Devuelve el saldo y los datos de la billetera de un usuario.</summary>
        /// <response code="200">Datos de la billetera.</response>
        /// <response code="404">El usuario todavía no tiene billetera creada.</response>
        [HttpGet("{usuarioId}")]
        public async Task<IActionResult> ObtenerPorUsuario(int usuarioId)
        {
            var billetera = await _billeteraService.ObtenerPorUsuarioIdAsync(usuarioId);
            if (billetera is null)
            {
                return NotFound(new { mensaje = $"No existe una billetera para el usuario {usuarioId}." });
            }

            return Ok(billetera);
        }
    }
}
