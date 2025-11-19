using FluentValidation;
using FTN.Dtos;

namespace FTN.Validators
{
    public class StageEntrancesDtoValidator : AbstractValidator<StageEntrancesDto>
    {
        public StageEntrancesDtoValidator()
        {
            RuleFor(x => x.Folio)
                .NotEmpty().WithMessage("El folio es obligatorio");

            RuleFor(x => x.PartNumber)
                .NotEmpty().WithMessage("El número de parte es obligatorio");            

            RuleFor(x => x.EntryDate)
                .NotEmpty().WithMessage("La fecha de entrada no puede ser vacia");
        }
    }
}