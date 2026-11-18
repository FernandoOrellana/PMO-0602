namespace PetCare.Application.Common;

public class OpcionesApp
{
    public const string Seccion = "App";

    public string PublicBaseUrl { get; set; } = "http://localhost:3000";
    public int CodigoExpiraMinutos { get; set; } = 15;
    public int RefreshTokenDias { get; set; } = 7;
    public int RecordatorioDias { get; set; } = 7;
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
}
