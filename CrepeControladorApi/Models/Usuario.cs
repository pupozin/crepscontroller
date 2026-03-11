namespace CrepeControladorApi.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string Senha { get; set; } = null!;
        public int EmpresaId { get; set; }
        public int PerfilId { get; set; }

        // Navegacao
        public Empresa? Empresa { get; set; }
        public Perfil? Perfil { get; set; }
    }
}
