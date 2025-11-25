namespace CrepeControladorApi.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public decimal Preco { get; set; }
        public bool Ativo { get; set; } = true;

        // Navegação
        public ICollection<ItensPedido>? ItensPedido { get; set; }
    }
}