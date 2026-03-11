using System.ComponentModel.DataAnnotations;

namespace CrepeControladorApi.Dtos
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Senha { get; set; } = null!;
    }
}
