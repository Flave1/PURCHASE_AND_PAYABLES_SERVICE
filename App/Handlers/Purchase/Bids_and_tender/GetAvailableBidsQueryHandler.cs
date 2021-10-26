using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Puchase_and_payables.Contracts.Queries.Purchases;
using Puchase_and_payables.Contracts.Response.IdentityServer.QuickType;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.Repository.Purchase;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{

    public class GetAvailableBidsQueryHandler : IRequestHandler<GetAvailableBidsQuery, BidAndTenderRespObj>
    {
        private readonly IPurchaseService _repo;
        private readonly IIdentityServerRequest _serverRequest;
        private readonly DataContext _dataContext;
        public GetAvailableBidsQueryHandler(
            DataContext dataContext,
            IPurchaseService purchaseService,
            IIdentityServerRequest _request)
        {
            _dataContext = dataContext;
            _repo = purchaseService;
            _serverRequest = _request;
        }
        public async Task<BidAndTenderRespObj> Handle(GetAvailableBidsQuery request, CancellationToken cancellationToken)
        {
            var response = new BidAndTenderRespObj { BidAndTenders = new List<BidAndTenderObj>(), Status = new APIResponseStatus { Message = new APIResponseMessage() } };
            CompanyStructureRespObj _Department = await _serverRequest.GetAllCompanyStructureAsync();

            response.BidAndTenders = _dataContext.cor_bid_and_tender
            .Where(a => a.ApprovalStatusId != (int)ApprovalStatus.Disapproved
            && (int)ApprovalStatus.Authorised != a.ApprovalStatusId && a.SupplierId != 0).Take(40)
            .OrderByDescending(q => q.BidAndTenderId).Select(d => new BidAndTenderObj
            {
                BidAndTenderId = d.BidAndTenderId,
                AmountApproved = d.AmountApproved,
                DateSubmitted = d.DateSubmitted,
                DecisionResult = d.DecisionResult,
                DescriptionOfRequest = d.DescriptionOfRequest,
                Location = d.Location,
                LPOnumber = d.LPOnumber,
                ProposedAmount = d.ProposedAmount.ToString(),
                RequestDate = d.RequestDate,
                RequestingDepartment = d.RequestingDepartment,
                SupplierName = d.SupplierName,
                Suppliernumber = d.Suppliernumber,
                DecisionReultName = Convert.ToString((DecisionResult)d.DecisionResult),
                Quantity = d.Quantity,
                Total = d.Total,
                ApprovalStatusId = d.ApprovalStatusId,
                SupplierId = d.SupplierId,
                WorkflowToken = d.WorkflowToken,
                ProposalTenderUploadType = d.ProposalTenderUploadType,
                ProposalTenderUploadPath = d.ProposalTenderUploadPath,
                ProposalTenderUploadName = d.ProposalTenderUploadName,
                ProposalTenderUploadFullPath = d.ProposalTenderUploadFullPath,
                ExpectedDeliveryDate = d.ExpectedDeliveryDate,
                StatusName = Convert.ToString((ApprovalStatus)d.ApprovalStatusId), 
                //RequestingDepartmentName = _Department.companyStructures.FirstOrDefault(e => e.CompanyStructureId == d.RequestingDepartment).Name,
            }).ToList(); 
            response.Status.IsSuccessful = true;
            if(response.BidAndTenders.Any()) response.BidAndTenders.ForEach(e => {
                e.RequestingDepartmentName = _Department.companyStructures.FirstOrDefault(r => r.CompanyStructureId == e.RequestingDepartment)?.Name;
            });

            return response;
        }

    }
}
