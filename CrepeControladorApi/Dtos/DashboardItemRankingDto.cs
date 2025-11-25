namespace CrepeControladorApi.Dtos
{
    public class DashboardItemRankingDto
    {
        public int ItemId { get; set; }
        public string Nome { get; set; } = null!;
        public int QuantidadeVendida { get; set; }
        public decimal Faturamento { get; set; }
    }
}
