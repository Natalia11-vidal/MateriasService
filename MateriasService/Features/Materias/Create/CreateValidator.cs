namespace MateriasService.Features.Materias.Create;

// Corre automáticamente en el ValidationBehavior antes del handler
internal class CreateMateriaValidator : AbstractValidator<CreateMateriaCommand>
{
    public CreateMateriaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");

        RuleFor(x => x.Nrc)
            .NotEmpty().WithMessage("El NRC es obligatorio")
            .MaximumLength(20).WithMessage("El NRC no puede superar 20 caracteres");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres");
    }
}