using System.Linq;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using CrepeControladorApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ItensController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<ItensController> _logger;

        public ItensController(AppDbContext context, ICurrentUserContext currentUser, ILogger<ItensController> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterItens([FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var itens = await _context.Itens
                .AsNoTracking()
                .Where(i => i.EmpresaId == empresaId)
                .ToListAsync();

            return Ok(itens);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterItem(int id, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var item = await _context.Itens
                .AsNoTracking()
                .Where(i => i.Id == id && i.EmpresaId == empresaId)
                .FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CriarItem([FromBody] ItemCreateDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(itemDto.EmpresaId))
            {
                return Forbid();
            }

            var item = new Item
            {
                Nome = itemDto.Nome,
                Preco = itemDto.Preco,
                Ativo = itemDto.Ativo,
                EmpresaId = itemDto.EmpresaId
            };

            _context.Itens.Add(item);
            await _context.SaveChangesAsync();

            return Created($"api/itens/{item.Id}", new
            {
                item.Id,
                item.Nome,
                item.Preco,
                item.Ativo,
                item.EmpresaId
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AtualizarItem(int id, [FromBody] ItemUpdateDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(itemDto.EmpresaId))
            {
                return Forbid();
            }

            var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == id && i.EmpresaId == itemDto.EmpresaId);
            if (item == null)
            {
                return NotFound();
            }

            item.Nome = itemDto.Nome;
            item.Preco = itemDto.Preco;
            item.Ativo = itemDto.Ativo;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} atualizado por usuario {UsuarioId} na empresa {EmpresaId}", item.Id, _currentUser.UsuarioId, item.EmpresaId);
            return Ok(new
            {
                item.Id,
                item.Nome,
                item.Preco,
                item.Ativo,
                item.EmpresaId
            });
        }

    }
}
