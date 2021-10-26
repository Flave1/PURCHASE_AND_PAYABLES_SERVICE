using GODP.APIsContinuation.Repository.Interface;
using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Puchase_and_payables.Contracts.Commands.Purchase;
using Puchase_and_payables.Contracts.Queries.Purchases;
using Puchase_and_payables.Contracts.Response.Payment;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.DomainObjects.Auth;
using Puchase_and_payables.Repository.Purchase;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{
    public class GetAllSupplierPOQuery : IRequest<LPORespObj>
    {
        public class GetAllSupplierPOQueryHandler : IRequestHandler<GetAllSupplierPOQuery, LPORespObj>
        {
            private readonly IPurchaseService _repo;
            private readonly IHttpContextAccessor _httpContextAccessor; 
            public readonly UserManager<ApplicationUser> _userManager;
            private readonly ISupplierRepository _suprepo;
            private readonly DataContext _context;
            public GetAllSupplierPOQueryHandler(
                DataContext context,
                ISupplierRepository supplierRepository,
                IPurchaseService purchaseService,
                IHttpContextAccessor httpContextAccessor,
                UserManager<ApplicationUser> user)
            {
                _context = context;
                _suprepo = supplierRepository;
                _repo = purchaseService; 
                _httpContextAccessor = httpContextAccessor;
                _userManager = user;
            }
            public async Task<LPORespObj> Handle(GetAllSupplierPOQuery request, CancellationToken cancellationToken)
            { 
                var userEmail = _httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty; 
                var thisSupplierDeatil = await _suprepo.GetSupplierByEmailAsync(userEmail); 

                return new LPORespObj
                {
                    LPOs = _context.purch_plpo.OrderByDescending(q => q.LPONumber)
                    .Where(s => thisSupplierDeatil.SupplierId == s.WinnerSupplierId)
                    .Select(d => new LPOObj
                    {
                        SupplierAddress = d.Address,
                        ApprovalStatusId = d.ApprovalStatusId,
                        DeliveryDate = d.DeliveryDate,
                        Description = d.Description,
                        LPONumber = d.LPONumber,
                        Name = d.Name,
                        PLPOId = d.PLPOId,
                        SupplierId = d.SupplierIds,
                        Tax = d.Tax,
                        Total = d.Total,
                        AmountPayable = d.AmountPayable,
                        BidAndTenderId = d.BidAndTenderId,
                        GrossAmount = d.GrossAmount,
                        JobStatus = d.JobStatus,
                        JobStatusName = Convert.ToString((JobProgressStatus)d.JobStatus),
                        RequestDate = d.RequestDate,
                        SupplierNumber = d.SupplierNumber,
                        Location = d.Address,
                        Quantity = d.Quantity,
                        WorkflowToken = d.WorkflowToken,
                        
                        PaymentTerms = _context.cor_paymentterms.Where(r => r.BidAndTenderId == d.BidAndTenderId).Select(c => new PaymentTermsObj
                        {
                            BidAndTenderId = c.BidAndTenderId, 
                            Comment = c.Comment,
                            Completion = c.Completion,
                            Amount = c.Amount,
                            NetAmount = c.NetAmount,
                            Payment = c.Payment,
                            PaymentStatus = c.PaymentStatus,
                            PaymentTermId = c.PaymentTermId,
                            Phase = c.Phase,
                            ProjectStatusDescription = c.ProjectStatusDescription,
                            Status = c.Status,
                            StatusName = Convert.ToString((JobProgressStatus)d.JobStatus)
                        }).ToList(),
                    }).ToList(),
                    Status = new APIResponseStatus
                    {
                        IsSuccessful = true,
                        Message = new APIResponseMessage
                        {
                            
                        }
                    }
                };
            }
        }
    }
   
}
