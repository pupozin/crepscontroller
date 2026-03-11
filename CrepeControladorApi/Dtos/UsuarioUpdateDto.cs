using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class UsuarioUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = null!;
    }
}
