using FluentValidation;
using Puchase_and_payables.Contracts.Commands.Purchase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Validators.Purchase
{
     

    public class AddUpdateBidAndTenderByStaffCommandVal : AbstractValidator<AddUpdateBidAndTenderByStaffCommand>
    {
        public AddUpdateBidAndTenderByStaffCommandVal()
        {
            RuleFor(d => d.AmountApproved).NotEmpty();
            RuleFor(d => d.BidAndTenderId).NotEmpty();
            RuleFor(d => d.DescriptionOfRequest).NotEmpty();
            RuleFor(d => d.Location).NotEmpty();
            RuleFor(d => d.LPONumber).NotEmpty();
            RuleFor(d => d.ProposedAmount).NotEmpty();
            RuleFor(d => d.Quantity).NotEmpty();
            RuleFor(d => d.RequestDate).NotEmpty();
            RuleFor(d => d.RequestingDepartment).NotEmpty();
            RuleFor(d => d.SupplierId).NotEmpty();
            RuleFor(d => d.SupplierName).NotEmpty();
            RuleFor(d => d.Suppliernumber).NotEmpty();
            RuleFor(d => d.Total).NotEmpty();
            RuleFor(d => d.PurchaseReqNoteId).NotEmpty();
            RuleFor(d => d).MustAsync(NoDuplcatePhaseAsync).WithMessage("Duplicate Phase Detected");
            RuleFor(d => d).MustAsync(MustBearProposalsAsync).WithMessage("No Bid Found");
            RuleFor(d => d).MustAsync(ValidProposalBreakDown).WithMessage("PLease Confirm Proposed amount break down");
        }

        private async Task<bool> ValidProposalBreakDown(AddUpdateBidAndTenderByStaffCommand request, CancellationToken cancellationToken)
        {
            if (request.Paymentterms.Count() > 0)
            {

                if (request.ProposedAmount != request.Paymentterms.Sum(q => q.Amount))
                {
                    return await Task.Run(() => false);
                }
            }
            return await Task.Run(() => true);
        }

        private async Task<bool> NoDuplcatePhaseAsync(AddUpdateBidAndTenderByStaffCommand request, CancellationToken cancellationToken)
        {
            if (request.Paymentterms.Count() > 0)
            {
                if (request.Paymentterms.GroupBy(q => q.Phase).Any(a => a.Count() > 1))
                {
                    return await Task.Run(() => false);
                }
            }
            return await Task.Run(() => true);
        }
        private async Task<bool> MustBearProposalsAsync(AddUpdateBidAndTenderByStaffCommand request, CancellationToken cancellationToken)
        {
            if (request.Paymentterms.Count() == 0)
            {
                return await Task.Run(() => false);
            }
            return await Task.Run(() => true);
        }
    }
}
