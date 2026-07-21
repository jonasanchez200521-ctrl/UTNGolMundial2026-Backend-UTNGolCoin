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
