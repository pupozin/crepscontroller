namespace CrepeControladorApi.Models
{
    public class Empresa
    {
        public int Id { get; set; }
        public string Cnpj { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string RazaoSocial { get; set; } = null!;
        public string Seguimento { get; set; } = null!;

        // Navegacao
        public ICollection<Usuario>? Usuarios { get; set; }
        public ICollection<Item>? Itens { get; set; }
        public ICollection<Pedido>? Pedidos { get; set; }
        public ICollection<Mesa>? Mesas { get; set; }
    }
}
