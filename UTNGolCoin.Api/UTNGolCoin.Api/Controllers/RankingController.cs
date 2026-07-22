using Microsoft.AspNetCore.Mvc;
using UTNGolCoin.Api.Services;

namespace UTNGolCoin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RankingController : ControllerBase
    {
        private readonly RankingService _rankingService;

        public RankingController(RankingService rankingService)
        {
            _rankingService = rankingService;
        }

        /// <summary>Tabla de clasificación pública de usuarios (RF21), ordenada por saldo y luego por aciertos.</summary>
        /// <param name="top">Opcional: limita a los primeros N puestos. Sin este parámetro, devuelve a todos los usuarios con billetera.</param>
        /// <response code="200">Lista ordenada del ranking.</response>
        /// <response code="400">El parámetro top es menor o igual a 0.</response>
        [HttpGet]
        public async Task<IActionResult> Obtener([FromQuery] int? top)
        {
            if (top.HasValue && top.Value <= 0)
            {
                return BadRequest(new { mensaje = "El parámetro top debe ser mayor a 0." });
            }

            var ranking = await _rankingService.ObtenerRankingAsync(top);
            return Ok(ranking);
        }
    }
}
