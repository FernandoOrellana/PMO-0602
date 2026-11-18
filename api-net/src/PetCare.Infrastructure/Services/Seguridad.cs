using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetCare.Application.Abstracciones;
using PetCare.Domain;

namespace PetCare.Infrastructure.Services;

public class Seguridad(IOptions<JwtOpciones> opciones) : ISeguridad
{
    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
    private readonly JwtOpciones _jwt = opciones.Value;

    public int AccessTokenSegundos => _jwt.AccessMinutes * 60;

    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, 10);

    public bool VerificarPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public string CrearAccessToken(Usuario usuario)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var token = new JwtSecurityToken(
            claims: [new Claim("sub", usuario.Id.ToString()), new Claim("rol", usuario.Rol.Nombre)],
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessMinutes),
            signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string NuevoRefreshToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(48)).ToLowerInvariant();

    public string Sha256(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor))).ToLowerInvariant();

    public string NuevoCodigo() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public string NuevaClaveTemporal() =>
        new string(Enumerable.Range(0, 8).Select(_ => Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]).ToArray());
}
