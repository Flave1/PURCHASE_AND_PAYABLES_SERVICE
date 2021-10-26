using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Puchase_and_payables.Contracts.Queries.Purchases;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.Repository.Purchase;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{
    public class GetAllLPOQueryHandler : IRequestHandler<GetAllLPOQuery, LPORespObj>
    {
        private readonly IPurchaseService _repo;
        private readonly DataContext _dataContext;
        public GetAllLPOQueryHandler(IPurchaseService purchaseService, DataContext dataContext)
        {
            _dataContext = dataContext;
            _repo = purchaseService;
        }
      
        public async Task<LPORespObj> Handle(GetAllLPOQuery request, CancellationToken cancellationToken)
        {  
            return new LPORespObj
            {
                LPOs = _dataContext.purch_plpo.Where(s => s.JobStatus != 0 && s.WinnerSupplierId > 0 )
                .Take(60)
                .OrderByDescending(q => q.LPONumber).Select(d => new LPOObj
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
                }).ToList(),
                Status = new APIResponseStatus
                {
                    IsSuccessful = true,
                    Message = new APIResponseMessage()
                }
            };
        }
    }
}


