using System.Linq;
using System.Threading.Tasks;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmpresasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmpresasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObterPorId([FromQuery] int empresaId)
        {
            if (empresaId <= 0)
            {
                return BadRequest("Informe a empresa.");
            }

            var empresa = await _context.Empresas
                .FromSqlRaw("SELECT * FROM \"sp_Admin_Empresa_Obter\"({0})", empresaId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (empresa == null)
            {
                return NotFound();
            }

            return Ok(empresa);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] EmpresaUpdateDto dto)
        {
            if (!ModelState.IsValid || id != dto.Id)
            {
                return ValidationProblem(ModelState);
            }

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == id);
            if (empresa == null)
            {
                return NotFound();
            }

            empresa.Cnpj = dto.Cnpj;
            empresa.Nome = dto.Nome;
            empresa.RazaoSocial = dto.RazaoSocial;
            // Seguimento não deve ser alterado via esta rota

            await _context.SaveChangesAsync();
            return Ok(empresa);
        }
    }
}
