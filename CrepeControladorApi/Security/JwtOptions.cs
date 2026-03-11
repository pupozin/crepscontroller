namespace CrepeControladorApi.Security
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = "crepecontrolador";
        public string Audience { get; set; } = "crepecontrolador";
        public string SecretKey { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 15;
    }
}
