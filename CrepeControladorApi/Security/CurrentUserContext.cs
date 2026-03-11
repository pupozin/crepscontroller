using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CrepeControladorApi.Security
{
    public interface ICurrentUserContext
    {
        int? UsuarioId { get; }
        int? EmpresaId { get; }
        int? PerfilId { get; }
        string? PerfilNome { get; }
        bool IsAdmin { get; }
        bool EmpresaAutorizada(int empresaId);
    }

    public class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UsuarioId => ReadIntClaim(ClaimTypes.NameIdentifier);
        public int? EmpresaId => ReadIntClaim("empresaId");
        public int? PerfilId => ReadIntClaim("perfilId");
        public string? PerfilNome => ReadClaim("perfilNome") ?? ReadClaim(ClaimTypes.Role);
        public bool IsAdmin => string.Equals(PerfilNome, "Admin", StringComparison.OrdinalIgnoreCase);

        public bool EmpresaAutorizada(int empresaId)
        {
            return EmpresaId.HasValue && EmpresaId.Value == empresaId;
        }

        private string? ReadClaim(string type)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(type);
        }

        private int? ReadIntClaim(string type)
        {
            var value = ReadClaim(type);
            return int.TryParse(value, out var parsed) ? parsed : null;
        }
    }
}
