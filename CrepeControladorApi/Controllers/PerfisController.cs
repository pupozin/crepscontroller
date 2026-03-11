using System.Linq;
using CrepeControladorApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PerfisController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PerfisController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var perfis = await _context.Perfis
                .Select(p => new { p.Id, p.Nome })
                .ToListAsync();
            return Ok(perfis);
        }
    }
}
