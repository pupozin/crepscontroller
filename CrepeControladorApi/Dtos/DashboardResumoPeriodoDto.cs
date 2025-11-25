namespace CrepeControladorApi.Dtos
{
    public class DashboardResumoPeriodoDto
    {
        public int QtdePedidos { get; set; }
        public decimal FaturamentoTotal { get; set; }
        public decimal TicketMedio { get; set; }
        public int QtdeDiasPeriodo { get; set; }
        public decimal MediaClientesPorDia { get; set; }
    }
}
