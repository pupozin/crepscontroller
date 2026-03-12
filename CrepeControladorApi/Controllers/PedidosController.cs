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
                .Include(p => p.Mesa)
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
                EmpresaId = p.EmpresaId,
                Endereco = p.Endereco,
                MesaId = p.MesaId,
                MesaNumero = p.Mesa?.Numero
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
                .Include(p => p.Mesa)
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
                pedido.Endereco,
                pedido.MesaId,
                MesaNumero = pedido.Mesa?.Numero,
                Itens = itens
            });
        }

        [HttpGet("por-mesa")]
        public async Task<IActionResult> ListarPorMesa([FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync();
                await connection.EnsureApplicationTimeZoneAsync();
            }

            var grupos = new Dictionary<string, List<PedidoResumoDto>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM \"fn_Pedidos_Abertos_PorMesa\"(@EmpresaId)";
                var p = command.CreateParameter();
                p.ParameterName = "@EmpresaId";
                p.Value = empresaId;
                command.Parameters.Add(p);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var mesaId = reader.IsDBNull(reader.GetOrdinal("MesaId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("MesaId"));
                    var mesaNumero = reader.IsDBNull(reader.GetOrdinal("MesaNumero")) ? "Sem mesa" : reader.GetString(reader.GetOrdinal("MesaNumero"));
                    var pedido = new PedidoResumoDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
                        Cliente = reader.IsDBNull(reader.GetOrdinal("Cliente")) ? null : reader.GetString(reader.GetOrdinal("Cliente")),
                        TipoPedido = reader.GetString(reader.GetOrdinal("TipoPedido")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        Observacao = reader.IsDBNull(reader.GetOrdinal("Observacao")) ? null : reader.GetString(reader.GetOrdinal("Observacao")),
                        ValorTotal = reader.GetDecimal(reader.GetOrdinal("ValorTotal")),
                        DataCriacao = reader.GetDateTime(reader.GetOrdinal("DataCriacao")),
                        EmpresaId = empresaId,
                        MesaId = mesaId,
                        MesaNumero = mesaNumero
                    };

                    if (!grupos.ContainsKey(mesaNumero))
                    {
                        grupos[mesaNumero] = new List<PedidoResumoDto>();
                    }
                    grupos[mesaNumero].Add(pedido);
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }

            var resposta = grupos.Select(g => new
            {
                Mesa = g.Key,
                Pedidos = g.Value
            });

            return Ok(resposta);
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

            var tipoNormalizado = pedidoDto.TipoPedido.Trim();
            var (enderecoValido, mesaValida) = await ValidarEntregaMesaAsync(tipoNormalizado, pedidoDto.Endereco, pedidoDto.MesaId, pedidoDto.EmpresaId);
            if (!enderecoValido)
            {
                ModelState.AddModelError(nameof(pedidoDto.Endereco), "Endereco e obrigatorio para pedidos de entrega.");
                return ValidationProblem(ModelState);
            }
            if (!mesaValida)
            {
                ModelState.AddModelError(nameof(pedidoDto.MesaId), "Mesa informada invalida para esta empresa.");
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
                EmpresaId = empresaId,
                Endereco = tipoNormalizado.Equals("Entrega", StringComparison.OrdinalIgnoreCase) ? pedidoDto.Endereco : null,
                MesaId = tipoNormalizado.Equals("Restaurante", StringComparison.OrdinalIgnoreCase) ? pedidoDto.MesaId : null
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
            var tipoNormalizado = pedidoDto.TipoPedido.Trim();
            var (enderecoValido, mesaValida) = await ValidarEntregaMesaAsync(tipoNormalizado, pedidoDto.Endereco, pedidoDto.MesaId, pedidoDto.EmpresaId);
            if (!enderecoValido)
            {
                ModelState.AddModelError(nameof(pedidoDto.Endereco), "Endereco e obrigatorio para pedidos de entrega.");
                return ValidationProblem(ModelState);
            }
            if (!mesaValida)
            {
                ModelState.AddModelError(nameof(pedidoDto.MesaId), "Mesa informada invalida para esta empresa.");
                return ValidationProblem(ModelState);
            }

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
            pedido.Endereco = tipoNormalizado.Equals("Entrega", StringComparison.OrdinalIgnoreCase) ? pedidoDto.Endereco : null;
            pedido.MesaId = tipoNormalizado.Equals("Restaurante", StringComparison.OrdinalIgnoreCase) ? pedidoDto.MesaId : null;

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
                pedido.Endereco,
                pedido.MesaId,
                MesaNumero = pedido.Mesa?.Numero,
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

        private async Task<(bool enderecoOk, bool mesaOk)> ValidarEntregaMesaAsync(string tipoPedido, string? endereco, int? mesaId, int empresaId)
        {
            var isEntrega = string.Equals(tipoPedido, "Entrega", StringComparison.OrdinalIgnoreCase);
            var isRestaurante = string.Equals(tipoPedido, "Restaurante", StringComparison.OrdinalIgnoreCase);

            var enderecoOk = !isEntrega || (!string.IsNullOrWhiteSpace(endereco) && endereco.Length <= 250);

            if (!isRestaurante || mesaId == null)
            {
                return (enderecoOk, !isRestaurante || mesaId == null);
            }

            var mesaExiste = await _context.Mesas.AnyAsync(m => m.Id == mesaId && m.EmpresaId == empresaId);
            return (enderecoOk, mesaExiste);
        }
    }
}
