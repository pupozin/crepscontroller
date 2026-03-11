using System;

namespace CrepeControladorApi.Dtos
{
    public class LoginResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public int EmpresaId { get; set; }
        public string EmpresaNome { get; set; } = string.Empty;
        public int PerfilId { get; set; }
        public string PerfilNome { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
