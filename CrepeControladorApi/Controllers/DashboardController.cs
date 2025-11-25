using System;
using CrepeControladorApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("horarios/dia-semana")]
        public async Task<IActionResult> ObterHorariosPicoDiaSemana([FromQuery] byte diaSemana, [FromQuery] int? ano)
        {
            if (diaSemana < 1 || diaSemana > 7)
            {
                return BadRequest("Dia da semana deve estar entre 1 (segunda) e 7 (domingo).");
            }

            var resultado = await _dashboardService.ObterHorariosPicoPorDiaSemana(diaSemana, ano);
            return Ok(resultado);
        }

        [HttpGet("horarios/mes")]
        public async Task<IActionResult> ObterHorariosPicoMes([FromQuery] int ano, [FromQuery] int mes)
        {
            if (mes < 1 || mes > 12)
            {
                return BadRequest("Mes deve estar entre 1 e 12.");
            }

            var resultado = await _dashboardService.ObterHorariosPicoPorMes(ano, mes);
            return Ok(resultado);
        }

        [HttpGet("resumo")]
        public async Task<IActionResult> ObterResumoPeriodo([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterResumoPeriodo(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("itens-ranking")]
        public async Task<IActionResult> ObterItensRanking([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterItensRanking(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("tipo-pedido")]
        public async Task<IActionResult> ObterTipoPedido([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterTipoPedido(dataInicio, dataFim);
            return Ok(resultado);
        }
    }
}
