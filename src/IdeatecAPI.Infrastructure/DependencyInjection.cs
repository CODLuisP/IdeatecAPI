using System.Text;                                                         
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Categorias.Services;
using IdeatecAPI.Infrastructure.Persistence.UnitOfWork;
<<<<<<< HEAD
using IdeatecAPI.Infrastructure.Services;                                   // ← NUEVO
using Microsoft.AspNetCore.Authentication.JwtBearer;                        // ← NUEVO
using Microsoft.IdentityModel.Tokens;
using IdeatecAPI.Application.Features.Clientes.Services;                                       // ← NUEVO
=======
using IdeatecAPI.Infrastructure.Services;                              
using Microsoft.AspNetCore.Authentication.JwtBearer;                       
using Microsoft.IdentityModel.Tokens;
using IdeatecAPI.Application.Features.Empresas.Services;                                       
>>>>>>> da9220907266fa87052a45d1d39c9f0da1a8a1b2

namespace IdeatecAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        // Registrar UnitOfWork
        services.AddScoped<IUnitOfWork>(provider => new UnitOfWork(connectionString));
        
        // Registrar Servicios de Categorías
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IClienteService, ClienteService>();

        // ========================================
        // SERVICIOS DE AUTENTICACIÓN (NUEVO)
        // ========================================
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>(); 
        

        services.AddScoped<IEmpresaService, EmpresaService>();
        // ========================================
        // JWT AUTHENTICATION (NUEVO)
        // ========================================
        var jwtSecret = configuration["JwtSettings:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret not configured in appsettings.json");

        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();                                        

        return services;
    }
}