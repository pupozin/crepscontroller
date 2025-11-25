namespace CrepeControladorApi.Dtos
{
    public class PedidoItemDetalheDto
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ItemId { get; set; }
        public string NomeItem { get; set; } = null!;
        public decimal PrecoItem { get; set; }
        public bool ItemAtivo { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal TotalItem { get; set; }
    }
}
