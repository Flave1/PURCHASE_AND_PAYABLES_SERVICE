using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Puchase_and_payables.Contracts.Queries.Purchases;
using Puchase_and_payables.Contracts.Response.IdentityServer.QuickType;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.Requests;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{
    public class GetSupplierAdvertsNotBiddenForQueryHandler : IRequestHandler<GetSupplierAdvertsNotBiddenForQuery, BidAndTenderRespObj>
    {
        private readonly IIdentityServerRequest _serverRequest;
        private readonly DataContext _dataContext;
        public GetSupplierAdvertsNotBiddenForQueryHandler(
            IIdentityServerRequest serverRequest,
            DataContext dataContext)
        { 
            _dataContext = dataContext;
            _serverRequest = serverRequest;
        }
        public async Task<BidAndTenderRespObj> Handle(GetSupplierAdvertsNotBiddenForQuery request, CancellationToken cancellationToken)
        {
            var response = new BidAndTenderRespObj { Status = new APIResponseStatus { Message = new APIResponseMessage() } };
                
            var resp = new List<BidAndTenderObj>();

            CompanyStructureRespObj _Department = await _serverRequest.GetAllCompanyStructureAsync(); 

            var domainList =  _dataContext.cor_bid_and_tender.ToList().GroupBy(w => w.BidAndTenderId).Select(q => q.First()).Where(r => r.ApprovalStatusId == (int)ApprovalStatus.Awaiting).ToArray();

            response.BidAndTenders = domainList?.OrderByDescending(a => a.BidAndTenderId)
                .Where(r => r.ApprovalStatusId == (int)ApprovalStatus.Awaiting && r.SupplierId == 0)
                .Select(d => new BidAndTenderObj
                {
                    BidAndTenderId = d.BidAndTenderId,
                    AmountApproved = d.AmountApproved,
                    DateSubmitted = d.DateSubmitted,
                    DecisionResult = d.DecisionResult,
                    DescriptionOfRequest = d?.DescriptionOfRequest,
                    Location = d?.Location,
                    LPOnumber = d?.LPOnumber, 
                    ExpectedDeliveryDate = d.ExpectedDeliveryDate, 
                    RequestDate = d.RequestDate,
                    RequestingDepartment = d.RequestingDepartment,
                    SupplierName = d?.SupplierName,
                    Suppliernumber = d?.Suppliernumber, 
                    Quantity = d.Quantity,
                    Total = d.Total,
                    ApprovalStatusId = d.ApprovalStatusId,
                    SupplierId = d.SupplierId,
                    WorkflowToken = d.WorkflowToken,
                    Comment = d.Comment,
                    SupplierAddress = d.SupplierAddress,
                    RequestingDepartmentName = d.RequestingDepartment > 0? _Department.companyStructures.FirstOrDefault(e => e.CompanyStructureId == d.RequestingDepartment)?.Name : string.Empty,
                    PLPOId = d.PLPOId,
                    PRNId = d.PurchaseReqNoteId, 
                    
                }).ToList() ?? new List<BidAndTenderObj>();

            return response;
        }
        
    }
}
