using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PetCare.Domain;

namespace PetCare.Tests.Integracion;

public class AutorizacionTests(AutorizacionTests.ApiFabrica fabrica) : IClassFixture<AutorizacionTests.ApiFabrica>
{
    public const string Secreto = "secreto-de-pruebas-con-longitud-suficiente-123456";

    public class ApiFabrica : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MySql"] = "Server=127.0.0.1;Port=1;Database=x;User ID=x;Password=x;",
                ["Jwt:Secret"] = Secreto,
                ["Recordatorios:Habilitado"] = "false",
            }));
        }
    }

    private static string TokenPara(string rol)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secreto));
        var token = new JwtSecurityToken(
            claims: [new Claim("sub", "3"), new Claim("rol", rol)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task SinToken_RespondeConUnauthorizedEnFormatoJson()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsync("/api/mascotas", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("UNAUTHORIZED", cuerpo.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task VeterinarioNoPuedeCrearMascotas_RespondeForbidden()
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPara(Roles.Veterinario));

        var respuesta = await cliente.PostAsync("/api/mascotas", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("FORBIDDEN", cuerpo.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task DuenoNoPuedeEntrarAlModuloAdministrador_RespondeForbidden()
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPara(Roles.Dueno));

        var respuesta = await cliente.GetAsync("/api/admin/reportes/ventas");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
