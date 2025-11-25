namespace CrepeControladorApi.Models
{
    public class ItensPedido
    {
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal TotalItem { get; set; }
    }
}