using FluentValidation;
using PetCare.Domain;

namespace PetCare.Application.Contratos;

public class RegistroValidator : AbstractValidator<RegistroRequest>
{
    public RegistroValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres").MaximumLength(100);
        RuleFor(x => x.Telefono).MaximumLength(20);
    }
}

public class VerificarEmailValidator : AbstractValidator<VerificarEmailRequest>
{
    public VerificarEmailValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Codigo).Matches(@"^\d{6}$").WithMessage("El código debe tener 6 dígitos");
    }
}

public class EmailValidator : AbstractValidator<EmailRequest>
{
    public EmailValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefrescarValidator : AbstractValidator<RefrescarRequest>
{
    public RefrescarValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MinimumLength(20);
}

public class RestablecerValidator : AbstractValidator<RestablecerRequest>
{
    public RestablecerValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.ClaveTemporal).NotEmpty().MaximumLength(50);
        RuleFor(x => x.NuevaPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public class DispositivoValidator : AbstractValidator<DispositivoRequest>
{
    public DispositivoValidator() => RuleFor(x => x.FcmToken).NotEmpty().Length(10, 255);
}

public class MascotaValidator : AbstractValidator<MascotaRequest>
{
    public MascotaValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.EspecieId).GreaterThan(0);
        RuleFor(x => x.Raza).MaximumLength(80);
        RuleFor(x => x.PesoKg).GreaterThan(0).LessThanOrEqualTo(300).When(x => x.PesoKg is not null);
    }
}

public class AplicacionVacunaValidator : AbstractValidator<AplicacionVacunaRequest>
{
    public AplicacionVacunaValidator()
    {
        RuleFor(x => x.VacunaId).GreaterThan(0);
        RuleFor(x => x.Lote).MaximumLength(60);
    }
}

public class CitaValidator : AbstractValidator<CitaRequest>
{
    public CitaValidator()
    {
        RuleFor(x => x.MascotaId).GreaterThan(0);
        RuleFor(x => x.ServicioId).GreaterThan(0);
        RuleFor(x => x.VeterinarioId).GreaterThan(0);
        RuleFor(x => x.Hora).Matches(@"^([01]\d|2[0-3]):[0-5]\d$").WithMessage("Use el formato HH:mm");
    }
}

public class ConsultaValidator : AbstractValidator<ConsultaRequest>
{
    public ConsultaValidator()
    {
        RuleFor(x => x.Diagnostico).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Tratamiento).MaximumLength(2000);
        RuleFor(x => x.Observaciones).MaximumLength(2000);
        RuleFor(x => x.Peso).GreaterThan(0).LessThanOrEqualTo(300).When(x => x.Peso is not null);
    }
}

public class ObservacionesValidator : AbstractValidator<ObservacionesRequest>
{
    public ObservacionesValidator()
    {
        RuleFor(x => x.Observaciones).NotEmpty().MaximumLength(2000);
    }
}

public class PedidoValidator : AbstractValidator<PedidoRequest>
{
    public PedidoValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("El pedido debe tener al menos un producto").Must(i => i.Count <= 50);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductoId).GreaterThan(0);
            item.RuleFor(i => i.Cantidad).InclusiveBetween(1, 100);
        });
    }
}

public class ProductoValidator : AbstractValidator<ProductoRequest>
{
    public ProductoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Descripcion).MaximumLength(2000);
        RuleFor(x => x.Categoria).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Precio).GreaterThan(0).LessThanOrEqualTo(999999.99m);
        RuleFor(x => x.Stock).InclusiveBetween(0, 100000);
    }
}

public class ServicioValidator : AbstractValidator<ServicioRequest>
{
    public ServicioValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Precio).GreaterThan(0).LessThanOrEqualTo(999999.99m);
        RuleFor(x => x.DuracionMin).InclusiveBetween(15, 240);
    }
}

public class VeterinarioCrearValidator : AbstractValidator<VeterinarioCrearRequest>
{
    public VeterinarioCrearValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Telefono).MaximumLength(20);
    }
}

public class VeterinarioActualizarValidator : AbstractValidator<VeterinarioActualizarRequest>
{
    public VeterinarioActualizarValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Telefono).MaximumLength(20);
    }
}

public class HorariosValidator : AbstractValidator<IReadOnlyList<HorarioRequest>>
{
    public HorariosValidator()
    {
        RuleFor(x => x.Count).LessThanOrEqualTo(14);
        RuleForEach(x => x).ChildRules(h =>
        {
            h.RuleFor(x => x.DiaSemana).InclusiveBetween(0, 6);
            h.RuleFor(x => x.HoraInicio).Matches(@"^([01]\d|2[0-3]):[0-5]\d$");
            h.RuleFor(x => x.HoraFin).Matches(@"^([01]\d|2[0-3]):[0-5]\d$");
            h.RuleFor(x => x).Must(x => string.CompareOrdinal(x.HoraInicio, x.HoraFin) < 0)
                .WithMessage("La hora de inicio debe ser menor que la de fin");
        });
    }
}
