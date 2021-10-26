using FluentValidation;
using GODPAPIs.Contracts.Commands.Supplier;
using Puchase_and_payables.Contracts.Commands.Supplier;
using Puchase_and_payables.Contracts.GeneralExtension;
using Puchase_and_payables.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GODP.APIsContinuation.Validations
{
    public class UpdateSupplierBuisnessOwnerCommandVal : AbstractValidator<UpdateSupplierBuisnessOwnerCommand>
    {
        public UpdateSupplierBuisnessOwnerCommandVal()
        {
            RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
            RuleFor(x => x.PhoneNo).NotEmpty().MinimumLength(11);
            RuleFor(x => x.Email).EmailAddress().NotEmpty();
            RuleFor(x => x.Address).NotEmpty();
        }
    }

    public class AddUpdateSupplierFinancialDetalCommandVal : AbstractValidator<AddUpdateSupplierFinancialDetalCommand>
    {
        private readonly DataContext _dataContext;
        public AddUpdateSupplierFinancialDetalCommandVal(DataContext dataContext)
        {
            _dataContext = dataContext;
            RuleFor(s => s.BusinessSize).NotEmpty();
            RuleFor(s => s.SupplierId).NotEmpty().WithMessage("Unable to Identify Supplier").MustAsync(NotMoreThanThree).WithMessage("Financial years must not be more than 3") ;
            RuleFor(w => w.Value).NotEmpty();
            RuleFor(w => w.Year).NotEmpty()
                .MustAsync(IsNumericAsync).WithMessage("Invalid Year Detected")
                .MustAsync(InvalidFinancialYearAsync).WithMessage("Financial year cannot be in the future");
            RuleFor(w => w).NotEmpty()
               .MustAsync(NoDuplicateYear).WithMessage("Duplicate Financial Year detected");
        }

        private async Task<bool> NotMoreThanThree(int SupplierId, CancellationToken cancellationToken)
        {
            if (_dataContext.cor_financialdetail.Count(w => w.SupplierId == SupplierId) > 3)
            {
                return await Task.Run(() => false);
            } 
            return await Task.Run(() => true);
        }

        private async Task<bool> NoDuplicateYear(AddUpdateSupplierFinancialDetalCommand request, CancellationToken cancellationToken)
        {
            if (CustomValidators.IsNumeric(request.Year))
            {
                if (_dataContext.cor_financialdetail.Count(w => w.SupplierId == request.SupplierId && request.Year == w.Year && w.FinancialdetailId != request.FinancialdetailId) >= 1)
                {
                    return await Task.Run(() => false);
                }
            }
            return await Task.Run(() => true);
        }
        private async Task<bool> InvalidFinancialYearAsync(string year, CancellationToken cancellationToken)
        { 
            if (CustomValidators.IsNumeric(year))
            {
                if (Convert.ToInt32(year) > DateTime.UtcNow.Year)
                {
                    return await Task.Run(() => false);
                }
            }
            return await Task.Run(() => true);
        }


        private async Task<bool> IsNumericAsync(string year, CancellationToken cancellationToken)
        {
            if (!CustomValidators.IsNumeric(year))
            {
                return await Task.Run(() => false);
            }
            return await Task.Run(() => true);
        } 
    }
}
