using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using PetCare.Application.Contratos;
using PetCare.Application.Servicios;

namespace PetCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<Mapeador>();
        services.AddScoped<AuthService>();
        services.AddScoped<MascotasService>();
        services.AddScoped<CitasService>();
        services.AddScoped<TiendaService>();
        services.AddScoped<CatalogosService>();
        services.AddScoped<AdminService>();
        services.AddScoped<NotificacionesService>();
        services.AddScoped<RecordatoriosService>();
        services.AddValidatorsFromAssemblyContaining<RegistroValidator>();
        return services;
    }
}
