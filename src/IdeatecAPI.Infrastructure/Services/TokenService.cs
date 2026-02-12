using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IdeatecAPI.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Usuario usuario)
    {
        var secret = _configuration["JwtSettings:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret no configurado en appsettings.json");
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.UsuarioID.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim("tokenVersion", usuario.TokenVersion.ToString()),
            new Claim("nombreCompleto", usuario.NombreCompleto),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Si tiene RUC, agregarlo al token
        if (!string.IsNullOrEmpty(usuario.Ruc))
        {
            claims = claims.Append(new Claim("ruc", usuario.Ruc)).ToArray();
        }

        var expirationMinutes = int.Parse(
            _configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "30"
        );

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public (int usuarioId, int tokenVersion)? ValidateAccessToken(string token)
    {
        try
        {
            var secret = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secret))
                return null;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Sin margen de tiempo adicional
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var tokenVersionClaim = principal.FindFirst("tokenVersion")?.Value;

            if (int.TryParse(userIdClaim, out int userId) && 
                int.TryParse(tokenVersionClaim, out int tokenVersion))
            {
                return (userId, tokenVersion);
            }

            return null;
        }
        catch (Exception)
        {
            // Token inválido, expirado o manipulado
            return null;
        }
    }
}