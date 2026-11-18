using Microsoft.Extensions.Options;
using Moq;
using PetCare.Application.Abstracciones;
using PetCare.Application.Common;
using PetCare.Application.Contratos;
using PetCare.Domain;

namespace PetCare.Tests.Unitarias;

public static class Datos
{
    public static Rol Rol(string nombre) => new() { Id = nombre.Length, Nombre = nombre };

    public static Usuario Usuario(int id, string rol, bool verificado = true) => new()
    {
        Id = id,
        Email = $"u{id}@test.hn",
        Nombre = $"Usuario {id}",
        Rol = Rol(rol),
        RolId = 1,
        PasswordHash = "hash",
        EmailVerificado = verificado,
        Activo = true,
    };

    public static Mascota Mascota(int id, int duenoId) => new()
    {
        Id = id,
        DuenoId = duenoId,
        Dueno = Usuario(duenoId, Domain.Roles.Dueno),
        Especie = new Especie { Id = 1, Nombre = "Perro" },
        Nombre = "Firulais",
        Activo = true,
    };

    public static Servicio Servicio(int id = 1, int duracion = 30) =>
        new() { Id = id, Nombre = "Consulta general", Precio = 350, DuracionMin = duracion, Activo = true };
}

public static class Mocks
{
    public static Mock<ISeguridad> Seguridad()
    {
        var m = new Mock<ISeguridad>();
        m.Setup(s => s.Sha256(It.IsAny<string>())).Returns<string>(v => "sha:" + v);
        m.Setup(s => s.HashPassword(It.IsAny<string>())).Returns<string>(v => "hash:" + v);
        m.Setup(s => s.VerificarPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((p, h) => h == "hash:" + p);
        m.Setup(s => s.CrearAccessToken(It.IsAny<Usuario>())).Returns("access-token");
        m.Setup(s => s.NuevoRefreshToken()).Returns("refresh-token");
        m.Setup(s => s.NuevoCodigo()).Returns("123456");
        m.Setup(s => s.NuevaClaveTemporal()).Returns("Temp1234");
        m.SetupGet(s => s.AccessTokenSegundos).Returns(1800);
        return m;
    }

    public static Mock<IReloj> Reloj(DateTime ahora)
    {
        var m = new Mock<IReloj>();
        m.SetupGet(r => r.Ahora).Returns(ahora);
        return m;
    }

    public static Mock<IAlmacenArchivos> Almacen()
    {
        var m = new Mock<IAlmacenArchivos>();
        m.Setup(a => a.UrlPublica(It.IsAny<string?>())).Returns<string?>(r => r is null ? null : "https://x/" + r);
        return m;
    }

    public static IOptions<OpcionesApp> Opciones() => Options.Create(new OpcionesApp());

    public static Mapeador Mapeador() => new(Almacen().Object);
}
