using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Puchase_and_payables.Contracts.Queries.Purchases;
using Puchase_and_payables.Contracts.Response.ApprovalRes;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.Repository.Purchase;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{ 

    public class GetAllPRNQueryHandler : IRequestHandler<GetAllPRNQuery, RequisitionNoteRespObj>
    {
        private readonly IPurchaseService _repo;
        private readonly IIdentityServerRequest _serverRequest;
        private readonly DataContext _dataContext;

        public GetAllPRNQueryHandler(
            IPurchaseService Repository, IIdentityServerRequest identityServerRequest, DataContext dataContext)
        { 
            _repo = Repository;
            _dataContext = dataContext;
            _serverRequest = identityServerRequest; 
        } 
        public async Task<RequisitionNoteRespObj> Handle(GetAllPRNQuery request, CancellationToken cancellationToken)
        { 
            var _Department = await _serverRequest.GetAllCompanyStructureAsync();

            var user = await _serverRequest.UserDataAsync();

            return new RequisitionNoteRespObj
            {
                RequisitionNotes = _dataContext.purch_requisitionnote.Where(q => q.StaffId == user.StaffId).ToList().OrderByDescending(r => r.PurchaseReqNoteId).Select(d => new RequisitionNoteObj
                {
                    ApprovalStatusId = d.ApprovalStatusId,
                    Comment = d.Comment,
                    DeliveryLocation = d.DeliveryLocation,
                    DepartmentId = d.DepartmentId,
                    Description = d.Description,
                    DocumentNumber = d.DocumentNumber,
                    ExpectedDeliveryDate = d.ExpectedDeliveryDate,
                    IsFundAvailable = d.IsFundAvailable,
                    PurchaseReqNoteId = d.PurchaseReqNoteId,
                    RequestBy = d.RequestBy,
                    StatusName = Convert.ToString((ApprovalStatus)d.ApprovalStatusId),
                    Total = d.Total,
                    WorkflowToken = d.WorkflowToken,
                    RequestDate = d.CreatedOn,
                    PRNNumber = d.PRNNumber,
                    DepartmentName = _Department.companyStructures.FirstOrDefault(c => c.CompanyStructureId == d.DepartmentId)?.Name,
                    DetailsCount = d.purch_prndetails.Count()
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
