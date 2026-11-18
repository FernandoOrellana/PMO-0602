namespace PetCare.Infrastructure;

public class JwtOpciones
{
    public const string Seccion = "Jwt";
    public string Secret { get; set; } = "";
    public int AccessMinutes { get; set; } = 30;
}

public class SmtpOpciones
{
    public const string Seccion = "Smtp";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool Secure { get; set; }
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "PetCare HN <no-reply@huellitas.hn>";
}

public class FirebaseOpciones
{
    public const string Seccion = "Firebase";
    public string ServiceAccountPath { get; set; } = "";
}

public class AlmacenOpciones
{
    public const string Seccion = "Almacenamiento";
    public string Carpeta { get; set; } = "uploads";
}

public class RecordatoriosOpciones
{
    public const string Seccion = "Recordatorios";
    public bool Habilitado { get; set; } = true;
    public string Hora { get; set; } = "08:00";
}
