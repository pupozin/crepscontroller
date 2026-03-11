namespace CrepeControladorApi.Models
{
    public class Perfil
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;

        // Navegacao
        public ICollection<Usuario>? Usuarios { get; set; }
    }
}
