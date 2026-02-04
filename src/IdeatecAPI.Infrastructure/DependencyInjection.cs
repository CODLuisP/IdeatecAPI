using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Categorias.Services;
using IdeatecAPI.Infrastructure.Persistence.UnitOfWork;

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
        
        // Registrar Servicios
        services.AddScoped<ICategoriaService, CategoriaService>();

        return services;
    }
}