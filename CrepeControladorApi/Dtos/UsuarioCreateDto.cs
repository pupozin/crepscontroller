using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class UsuarioCreateDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = null!;

        [Required]
        public int PerfilId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int EmpresaId { get; set; }
    }
}
