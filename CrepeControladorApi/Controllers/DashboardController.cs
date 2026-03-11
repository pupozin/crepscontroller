using System.ComponentModel.DataAnnotations;
using CrepeControladorApi.Dtos;
using CrepeControladorApi.Security;
using CrepeControladorApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrepeControladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ICurrentUserContext _currentUser;

        public DashboardController(DashboardService dashboardService, ICurrentUserContext currentUser)
        {
            _dashboardService = dashboardService;
            _currentUser = currentUser;
        }

        [HttpGet("horarios/periodo")]
        public async Task<IActionResult> ObterHorariosPicoPeriodo([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterHorariosPicoPorPeriodo(dataInicio, dataFim, empresaId);
            return Ok(resultado);
        }

        [HttpGet("dia-semana/picos")]
        public async Task<IActionResult> ObterPicosPorDiaSemana([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterHorariosPicoDiaSemanaResumo(dataInicio, dataFim, empresaId);
            return Ok(resultado);
        }

        [HttpGet("dia-semana/distribuicao")]
        public async Task<IActionResult> ObterDistribuicaoDiaSemana([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (dataInicio > dataFim)
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterDistribuicaoDiaSemanaPorHora(dataInicio, dataFim, empresaId);
            return Ok(resultado);
        }

        [HttpGet("periodo-total")]
        public async Task<IActionResult> ObterPeriodoTotal([FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            var periodo = await _dashboardService.ObterPeriodoTotal(empresaId);
            return Ok(periodo ?? new DashboardPeriodoTotalDto());
        }

        [HttpGet("resumo")]
        public async Task<IActionResult> ObterResumoPeriodo([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (DatasInvalidas(dataInicio, dataFim))
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterResumoPeriodo(dataInicio, dataFim, empresaId);
            return Ok(resultado);
        }

        [HttpGet("itens-ranking")]
        public async Task<IActionResult> ObterItensRanking([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (DatasInvalidas(dataInicio, dataFim))
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterItensRanking(dataInicio, dataFim, empresaId);
            return Ok(resultado);
        }

        [HttpGet("tipo-pedido")]
        public async Task<IActionResult> ObterTipoPedido([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim, [FromQuery][Range(1, int.MaxValue)] int empresaId)
        {
            if (!_currentUser.EmpresaAutorizada(empresaId))
            {
                return Forbid();
            }

            if (DatasInvalidas(dataInicio, dataFim))
            {
                return BadRequest("DataInicio nao pode ser maior que DataFim.");
            }

            var resultado = await _dashboardService.ObterTipoPedido(dataInicio, dataFim, empresaId);
            return Ok(resultado);
        }

        private static bool DatasInvalidas(DateTime? inicio, DateTime? fim)
        {
            return inicio.HasValue && fim.HasValue && inicio > fim;
        }
    }
}
