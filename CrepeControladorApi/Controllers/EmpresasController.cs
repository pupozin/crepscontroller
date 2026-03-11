using System.ComponentModel.DataAnnotations;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmpresasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<EmpresasController> _logger;

        public EmpresasController(AppDbContext context, ICurrentUserContext currentUser, ILogger<EmpresasController> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterPorId([FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (empresaId <= 0)
            {
                return BadRequest("Informe a empresa.");
            }

            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var empresa = await _context.Empresas
                .FromSqlInterpolated($"SELECT * FROM \"sp_Admin_Empresa_Obter\"({empresaId})")
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (empresa == null)
            {
                return NotFound();
            }

            return Ok(empresa);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] EmpresaUpdateDto dto)
        {
            if (!ModelState.IsValid || id != dto.Id)
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(id))
            {
                return Forbid();
            }

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == id);
            if (empresa == null)
            {
                return NotFound();
            }

            empresa.Cnpj = dto.Cnpj;
            empresa.Nome = dto.Nome;
            empresa.RazaoSocial = dto.RazaoSocial;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Empresa {EmpresaId} atualizada por usuario {UsuarioId}", empresa.Id, _currentUser.UsuarioId);
            return Ok(empresa);
        }
    }
}
