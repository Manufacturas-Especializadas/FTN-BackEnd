using FluentValidation;
using FTN.Models;

namespace FTN.Validators
{
    public class StageEntrancesValidator: AbstractValidator<StageEntrances>
    {
        public StageEntrancesValidator()
        {
            RuleFor(x => x.Folio)
                .NotEmpty().WithMessage("El folio es obligatorio");

            RuleFor(x => x.PartNumbers)
                .NotEmpty().WithMessage("El número de parte es obligatorio");

            RuleFor(x => x.Platforms)
                .NotEmpty().WithMessage("El número de tarimas debe ser mayor a 0");

            RuleFor(x => x.EntryDate)
                .NotEmpty().WithMessage("La fecha de entrada no puede ser vacia");
        }
    }
}