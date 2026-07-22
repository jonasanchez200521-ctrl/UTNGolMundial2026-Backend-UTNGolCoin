using Microsoft.AspNetCore.Mvc;
using UTNGolCoin.Api.Services;

namespace UTNGolCoin.Api.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    public class ReportesController : ControllerBase
    {
        private readonly ReporteService _reporteService;

        public ReportesController(ReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [HttpGet("monedas-circulacion")]
        public async Task<IActionResult> ObtenerMonedasCirculacion()
        {
            var reporte = await _reporteService.ObtenerMonedasEnCirculacionAsync();
            return Ok(reporte);
        }

        [HttpGet("partidos-mas-apostados")]
        public async Task<IActionResult> ObtenerPartidosMasApostados([FromQuery] int? top)
        {
            if (top.HasValue && top.Value <= 0)
            {
                return BadRequest(new { mensaje = "El parámetro top debe ser mayor a 0." });
            }

            var reporte = await _reporteService.ObtenerPartidosMasApostadosAsync(top);
            return Ok(reporte);
        }
    }
}
