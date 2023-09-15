using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PostAPI.Interfaces;
using PostAPI.Models;

namespace PostAPI.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TierController : ControllerBase
    {
        private readonly ITier _tierService;

        public TierController(ITier tierService)
        {
            _tierService = tierService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ReaderTier>))]
        public async Task<IActionResult> GetReaderTiers()
        {
            var tiers = await _tierService.GetReaderTiers();

            return Ok(tiers);
        }
    }
}
