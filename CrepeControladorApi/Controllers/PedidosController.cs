using System.Linq;
using System.ComponentModel.DataAnnotations;
using CrepeControladorApi.Data;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Models;
using CrepeControladorApi.Security;
using CrepeControladorApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PedidoQueryService _pedidoQueryService;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<PedidosController> _logger;

        public PedidosController(AppDbContext context, PedidoQueryService pedidoQueryService, ICurrentUserContext currentUser, ILogger<PedidosController> logger)
        {
            _context = context;
            _pedidoQueryService = pedidoQueryService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ObterPedidos([FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId)
                .ToListAsync();

            return Ok(pedidos.Select(p => new PedidoResumoDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Cliente = p.Cliente,
                TipoPedido = p.TipoPedido,
                Status = p.Status,
                Observacao = p.Observacao,
                DataCriacao = p.DataCriacao,
                DataConclusao = p.DataConclusao,
                ValorTotal = p.ValorTotal,
                EmpresaId = p.EmpresaId
            }));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPedido(int id, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Itens)
                .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);

            if (pedido == null)
            {
                return NotFound();
            }

            var itens = pedido.Itens.Select(i => new PedidoItemDetalheDto
            {
                Id = i.Id,
                PedidoId = i.PedidoId,
                ItemId = i.ItemId,
                NomeItem = i.Item.Nome,
                PrecoItem = i.Item.Preco,
                ItemAtivo = i.Item.Ativo,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                TotalItem = i.TotalItem
            }).ToList();

            return Ok(new
            {
                pedido.Id,
                pedido.Codigo,
                pedido.Cliente,
                pedido.TipoPedido,
                pedido.Status,
                pedido.Observacao,
                pedido.DataCriacao,
                pedido.DataConclusao,
                pedido.ValorTotal,
                pedido.EmpresaId,
                Itens = itens
            });
        }

        [HttpGet("pesquisar")]
        public async Task<IActionResult> PesquisarPedidos([FromQuery] string termo, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(termo))
            {
                return BadRequest("Informe um termo para pesquisa.");
            }

            var pedidos = await _pedidoQueryService.PesquisarPedidosAsync(termo.Trim(), empresaId);
            return Ok(pedidos);
        }

        [HttpGet("abertos")]
        public async Task<IActionResult> ListarPedidosAbertos([FromQuery] string tipoPedido, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(tipoPedido))
            {
                return BadRequest("Informe o tipo de pedido.");
            }

            var pedidos = await _pedidoQueryService.ListarPedidosAbertosPorTipoAsync(tipoPedido, empresaId);
            return Ok(pedidos);
        }

        [HttpGet("grupo-status")]
        public async Task<IActionResult> ListarPorGrupoStatus([FromQuery] string grupo, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(grupo))
            {
                return BadRequest("Informe o grupo de status.");
            }

            var pedidos = await _pedidoQueryService.ListarPorGrupoStatusAsync(grupo.ToUpperInvariant(), empresaId);
            return Ok(pedidos);
        }

        [HttpPost]
        public async Task<IActionResult> CriarPedido([FromBody] PedidoCreateDto pedidoDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(pedidoDto.EmpresaId))
            {
                return Forbid();
            }

            if (pedidoDto.Itens == null || pedidoDto.Itens.Count == 0)
            {
                ModelState.AddModelError(nameof(pedidoDto.Itens), "Informe ao menos um item para o pedido.");
                return ValidationProblem(ModelState);
            }

            var empresaId = _currentUser.EmpresaId!.Value;
            var codigoPedido = await GerarCodigoPedidoAsync(empresaId);

            var pedido = new Pedido
            {
                Codigo = codigoPedido,
                Cliente = pedidoDto.Cliente,
                TipoPedido = pedidoDto.TipoPedido,
                Observacao = pedidoDto.Observacao,
                DataCriacao = DateTime.UtcNow,
                EmpresaId = empresaId
            };

            decimal valorTotal = 0m;

            foreach (var itemDto in pedidoDto.Itens)
            {
                var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == itemDto.ItemId && i.EmpresaId == empresaId);
                if (item == null)
                {
                    ModelState.AddModelError(nameof(pedidoDto.Itens), $"Item com Id {itemDto.ItemId} nao foi encontrado.");
                    return ValidationProblem(ModelState);
                }

                var totalItem = item.Preco * itemDto.Quantidade;

                pedido.Itens.Add(new ItensPedido
                {
                    ItemId = item.Id,
                    Quantidade = itemDto.Quantidade,
                    PrecoUnitario = item.Preco,
                    TotalItem = totalItem
                });

                valorTotal += totalItem;
            }

            pedido.ValorTotal = valorTotal;

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Pedido {PedidoId} criado na empresa {EmpresaId} por {UsuarioId}", pedido.Id, empresaId, _currentUser.UsuarioId);

            return Created($"api/pedidos/{pedido.Id}", new
            {
                pedido.Id,
                pedido.Codigo,
                pedido.Status,
                pedido.EmpresaId,
                pedido.ValorTotal,
                Itens = pedido.Itens.Select(i => new
                {
                    i.ItemId,
                    i.Quantidade,
                    i.TotalItem
                })
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> AtualizarPedido(int id, [FromBody] PedidoUpdateDto pedidoDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!_currentUser.EmpresaAutorizada(pedidoDto.EmpresaId))
            {
                return Forbid();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == pedidoDto.EmpresaId);

            if (pedido == null)
            {
                return NotFound();
            }

            if (!StatusPermiteAlteracao(pedido.Status))
            {
                return BadRequest("Pedido so pode ser atualizado quando estiver Preparando ou Pronto.");
            }

            if (pedidoDto.Itens == null || pedidoDto.Itens.Count == 0)
            {
                ModelState.AddModelError(nameof(pedidoDto.Itens), "Informe ao menos um item para o pedido.");
                return ValidationProblem(ModelState);
            }

            var estavaAberto = StatusPermiteAlteracao(pedido.Status);

            pedido.Cliente = pedidoDto.Cliente;
            pedido.TipoPedido = pedidoDto.TipoPedido;
            pedido.Status = pedidoDto.Status;
            pedido.Observacao = pedidoDto.Observacao;

            decimal valorTotal = 0m;
            var novosItens = new List<ItensPedido>();

            foreach (var itemDto in pedidoDto.Itens)
            {
                var item = await _context.Itens.FirstOrDefaultAsync(i => i.Id == itemDto.ItemId && i.EmpresaId == pedido.EmpresaId);
                if (item == null)
                {
                    ModelState.AddModelError(nameof(pedidoDto.Itens), $"Item com Id {itemDto.ItemId} nao foi encontrado.");
                    return ValidationProblem(ModelState);
                }

                var totalItem = item.Preco * itemDto.Quantidade;

                novosItens.Add(new ItensPedido
                {
                    ItemId = item.Id,
                    Quantidade = itemDto.Quantidade,
                    PrecoUnitario = item.Preco,
                    TotalItem = totalItem
                });

                valorTotal += totalItem;
            }

            _context.ItensPedidos.RemoveRange(pedido.Itens);
            pedido.Itens.Clear();

            foreach (var novoItem in novosItens)
            {
                pedido.Itens.Add(novoItem);
            }

            pedido.ValorTotal = valorTotal;

            if (estavaAberto && StatusIndicaFechado(pedido.Status))
            {
                pedido.DataConclusao = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Pedido {PedidoId} atualizado na empresa {EmpresaId} por {UsuarioId}", pedido.Id, pedido.EmpresaId, _currentUser.UsuarioId);

            return Ok(new
            {
                pedido.Id,
                pedido.Codigo,
                pedido.Status,
                pedido.EmpresaId,
                pedido.ValorTotal,
                Itens = pedido.Itens.Select(i => new
                {
                    i.ItemId,
                    i.Quantidade,
                    i.TotalItem
                })
            });
        }

        private static bool StatusPermiteAlteracao(string status)
        {
            return string.Equals(status, "Preparando", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Pronto", StringComparison.OrdinalIgnoreCase);
        }

        private static bool StatusIndicaFechado(string status)
        {
            return string.Equals(status, "Finalizado", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Cancelado", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GerarCodigoPedidoAsync(int empresaId)
        {
            var ultimoId = await _context.Pedidos
                .Where(p => p.EmpresaId == empresaId)
                .OrderByDescending(p => p.Id)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var proximoNumero = ultimoId + 1;
            return $"Pedido #{proximoNumero:D4}";
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarPedidos([FromQuery] string termo, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var pedidos = await _pedidoQueryService.PesquisarPedidosAsync(termo, empresaId);
            return Ok(pedidos);
        }
    }
}
