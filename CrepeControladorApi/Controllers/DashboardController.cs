using System;
using CrepeControladorApi.Dtos;
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

        [HttpGet("horarios/periodo")]
        public async Task<IActionResult> ObterHorariosPicoPeriodo([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterHorariosPicoPorPeriodo(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("dia-semana/picos")]
        public async Task<IActionResult> ObterPicosPorDiaSemana([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterHorariosPicoDiaSemanaResumo(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("dia-semana/distribuicao")]
        public async Task<IActionResult> ObterDistribuicaoDiaSemana([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterDistribuicaoDiaSemanaPorHora(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("periodo-total")]
        public async Task<IActionResult> ObterPeriodoTotal()
        {
            var periodo = await _dashboardService.ObterPeriodoTotal();
            return Ok(periodo ?? new DashboardPeriodoTotalDto());
        }

        [HttpGet("resumo")]
        public async Task<IActionResult> ObterResumoPeriodo([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            if (DatasInvalidas(dataInicio, dataFim))
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterResumoPeriodo(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("itens-ranking")]
        public async Task<IActionResult> ObterItensRanking([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            if (DatasInvalidas(dataInicio, dataFim))
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterItensRanking(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("tipo-pedido")]
        public async Task<IActionResult> ObterTipoPedido([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            if (DatasInvalidas(dataInicio, dataFim))
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterTipoPedido(dataInicio, dataFim);
            return Ok(resultado);
        }

        private static bool DatasInvalidas(DateTime? inicio, DateTime? fim)
        {
            return inicio.HasValue && fim.HasValue && inicio > fim;
        }
    }
}
