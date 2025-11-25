using System.Linq;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItensController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItensController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObterItens()
        {
            var itens = await _context.Itens
                .FromSqlRaw("EXEC sp_Item_Obter")
                .AsNoTracking()
                .ToListAsync();

            return Ok(itens);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterItem(int id)
        {
            var itens = await _context.Itens
                .FromSqlInterpolated($"EXEC sp_Item_Obter @ItemId = {id}")
                .AsNoTracking()
                .ToListAsync();

            var item = itens.FirstOrDefault();

            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> CriarItem([FromBody] ItemCreateDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var item = new Item
            {
                Nome = itemDto.Nome,
                Preco = itemDto.Preco,
                Ativo = itemDto.Ativo
            };

            _context.Itens.Add(item);
            await _context.SaveChangesAsync();

            return Created($"api/itens/{item.Id}", new
            {
                item.Id,
                item.Nome,
                item.Preco,
                item.Ativo
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> AtualizarItem(int id, [FromBody] ItemUpdateDto itemDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var item = await _context.Itens.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            item.Nome = itemDto.Nome;
            item.Preco = itemDto.Preco;
            item.Ativo = itemDto.Ativo;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                item.Id,
                item.Nome,
                item.Preco,
                item.Ativo
            });
        }

    }
}
