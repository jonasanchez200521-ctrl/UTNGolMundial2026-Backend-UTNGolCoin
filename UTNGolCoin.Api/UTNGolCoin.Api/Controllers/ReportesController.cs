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

        /// <summary>Reporte administrativo (RF27): total de UTNGolCoin en circulación entre todas las billeteras.</summary>
        /// <response code="200">Totales de monedas en circulación, cantidad de billeteras y total pagado en premios.</response>
        [HttpGet("monedas-circulacion")]
        public async Task<IActionResult> ObtenerMonedasCirculacion()
        {
            var reporte = await _reporteService.ObtenerMonedasEnCirculacionAsync();
            return Ok(reporte);
        }

        /// <summary>Reporte administrativo (RF27): partidos con más predicciones, de mayor a menor cantidad.</summary>
        /// <param name="top">Opcional: limita a los primeros N partidos. Sin este parámetro, devuelve a todos.</param>
        /// <response code="200">Lista de partidos con su cantidad de predicciones.</response>
        /// <response code="400">El parámetro top es menor o igual a 0.</response>
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
