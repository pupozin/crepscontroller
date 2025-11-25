namespace CrepeControladorApi.Dtos
{
    public class DashboardTipoPedidoDto
    {
        public string TipoPedido { get; set; } = null!;
        public int QtdePedidos { get; set; }
        public decimal Faturamento { get; set; }
    }
}
