using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Infrastructure.Jobs;
using PetCare.Infrastructure.Persistence;
using PetCare.Infrastructure.Services;

namespace PetCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<PetCareDbContext>(o => o.UseMySQL(config.GetConnectionString("MySql")!));

        services.Configure<OpcionesApp>(config.GetSection(OpcionesApp.Seccion));
        services.Configure<JwtOpciones>(config.GetSection(JwtOpciones.Seccion));
        services.Configure<SmtpOpciones>(config.GetSection(SmtpOpciones.Seccion));
        services.Configure<FirebaseOpciones>(config.GetSection(FirebaseOpciones.Seccion));
        services.Configure<AlmacenOpciones>(config.GetSection(AlmacenOpciones.Seccion));
        services.Configure<RecordatoriosOpciones>(config.GetSection(RecordatoriosOpciones.Seccion));

        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IRefreshTokenRepositorio, RefreshTokenRepositorio>();
        services.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        services.AddScoped<IVeterinarioRepositorio, VeterinarioRepositorio>();
        services.AddScoped<IMascotaRepositorio, MascotaRepositorio>();
        services.AddScoped<IVacunacionRepositorio, VacunacionRepositorio>();
        services.AddScoped<ICitaRepositorio, CitaRepositorio>();
        services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
        services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
        services.AddScoped<ICobroRepositorio, CobroRepositorio>();
        services.AddScoped<INotificacionRepositorio, NotificacionRepositorio>();
        services.AddScoped<IReporteRepositorio, ReporteRepositorio>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<ISeguridad, Seguridad>();
        services.AddSingleton<IReloj, RelojLocal>();
        services.AddSingleton<INotificadorCorreo, CorreoSmtp>();
        services.AddSingleton<IPushSender, PushFcm>();
        services.AddSingleton<IAlmacenArchivos, AlmacenLocal>();
        services.AddScoped<Seed>();

        services.AddHostedService<RecordatoriosHostedService>();
        return services;
    }
}
