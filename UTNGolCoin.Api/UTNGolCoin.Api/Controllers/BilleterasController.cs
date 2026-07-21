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
