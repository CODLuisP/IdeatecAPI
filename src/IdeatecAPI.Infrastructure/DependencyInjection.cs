using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Categorias.Services;
using IdeatecAPI.Infrastructure.Persistence.UnitOfWork;
using IdeatecAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using IdeatecAPI.Application.Features.Empresas.Services;
using IdeatecAPI.Application.Features.Notas.Services;
using IdeatecAPI.Application.Features.Clientes.Services;
using IdeatecAPI.Application.Features.Direccion.Services;
using IdeatecAPI.Application.Features.Comprobante.Services;
using IdeatecAPI.Application.Features.Productos.Services;
using IdeatecAPI.Application.Features.SerieCorrelativo.Services;
using IdeatecAPI.Application.Features.ComunicacionBaja.Services;
using IdeatecAPI.Application.Features.GuiaRemision.Services;
using IdeatecAPI.Application.Features.ResumenComprobante.Services;

namespace IdeatecAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        services.AddScoped<IUnitOfWork>(provider => new UnitOfWork(connectionString));

        // HttpClient para SUNAT
        services.AddHttpClient();

        // Categorías y Clientes
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IDireccionService, DireccionService>();

        // Autenticación
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        // Empresas
        services.AddScoped<IEmpresaService, EmpresaService>();

        // Notas
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IXmlNoteBuilderService, XmlNoteBuilderService>();
        services.AddScoped<IXmlSignerService, XmlSignerService>();
        services.AddScoped<ISunatSenderService, SunatSenderService>();

        // Comunicación de Baja
        services.AddScoped<IBajaService, BajaService>();
        services.AddScoped<IXmlBajaBuilderService, XmlBajaBuilderService>();
        services.AddScoped<ISunatBajaService, SunatBajaService>();

        // JWT
        // ========================================
        // JWT AUTHENTICATION (NUEVO)
        // ========================================
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IDireccionService, DireccionService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<INoteService, NoteService>();

        // Comprobante
        services.AddScoped<IComprobanteService, ComprobanteService>();
        services.AddScoped<IComprobanteXmlService, GeneraXmlService>();

        //Resumen de comprobantes
        services.AddScoped<IResumenComprobanteService, ResumenComprobanteService>();
        services.AddScoped<IResumenXmlService, GeneraResumenXmlService>();
        services.AddScoped<ISunatResumenService, SunatResumenService>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<ISerieCorrelativoService, SerieCorrelativoService>();

        // Guía de Remisión
        services.AddScoped<IGuiaService, GuiaService>();
        services.AddScoped<IXmlGuiaBuilderService, XmlGuiaBuilderService>();
        services.AddScoped<ISunatGuiaService, SunatGuiaService>();

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