using AutoMapper;
using GODP.APIsContinuation.Repository.Interface;
using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Puchase_and_payables.Contracts.Queries.Purchases;
using Puchase_and_payables.Contracts.Response.ApprovalRes;
using Puchase_and_payables.Contracts.Response.IdentityServer.QuickType;
using Puchase_and_payables.Contracts.Response.Payment;
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
    public class GetLPOAwaitingApprovalQueryHandler : IRequestHandler<GetLPOAwaitingApprovalQuery, LPORespObj>
    {
        private readonly IPurchaseService _repo; 
        private readonly IIdentityServerRequest _serverRequest;
        private readonly DataContext _dataContext;

        public GetLPOAwaitingApprovalQueryHandler(
            IPurchaseService Repository, 
            DataContext dataConext,
            IIdentityServerRequest identityServerRequest)
        { 
            _repo = Repository;
            _dataContext = dataConext;
            _serverRequest = identityServerRequest;
        }

         
        public async Task<LPORespObj> Handle(GetLPOAwaitingApprovalQuery request, CancellationToken cancellationToken)
        {
            var response = new LPORespObj 
            {  
                Status = new APIResponseStatus 
                { 
                    Message = new APIResponseMessage() 
                } 
            };
            try
            {
                var result = await _serverRequest.GetAnApproverItemsFromIdentityServer();
                var user = await _serverRequest.UserDataAsync();
                if (!result.IsSuccessStatusCode)
                {
                    var data1 = await result.Content.ReadAsStringAsync();
                    var res1 = JsonConvert.DeserializeObject<WorkflowTaskRespObj>(data1);
                    return new LPORespObj
                    {
                        Status = new APIResponseStatus
                        {
                            IsSuccessful = false,
                            Message = new APIResponseMessage { FriendlyMessage = $"{result.ReasonPhrase} {result.StatusCode}" }
                        }
                    };
                }

                var data = await result.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<WorkflowTaskRespObj>(data);

                if (res == null)
                {
                    return new LPORespObj
                    {
                        Status = res.Status
                    };
                }

                if (res.workflowTasks.Count() < 1)
                {
                    return new LPORespObj
                    {
                        Status = new APIResponseStatus
                        {
                            IsSuccessful = true,
                            Message = new APIResponseMessage
                            {
                                FriendlyMessage = "No Pending Approval"
                            }
                        }
                    };
                }

                var bids = await _dataContext.cor_bid_and_tender.Where(e => e.ApprovalStatusId == (int)ApprovalStatus.Approved).ToListAsync();
              
                var staffLPOawaiting = await _repo.GetLPOAwaitingApprovalAsync(res.workflowTasks.Select(x => x.TargetId).ToList(), res.workflowTasks.Select(s => s.WorkflowToken).ToList());
                 
               var _Department = await _serverRequest.GetAllCompanyStructureAsync(); 
                 
                response.LPOs = staffLPOawaiting?.Where(d => user.Staff_limit >= d.AmountPayable).Select(d => new LPOObj
                {
                    AmountPayable = d.AmountPayable,
                    ApprovalStatusId = d.ApprovalStatusId,
                    BidAndTenderId = d.BidAndTenderId,
                    DeliveryDate = d.DeliveryDate,
                    Description = d.Description,
                    GrossAmount = d.GrossAmount,
                    JobStatus = d.JobStatus,
                    JobStatusName = Convert.ToString((JobProgressStatus)d.JobStatus),
                    LPONumber = d.LPONumber,
                    Name = d.Name,
                    PLPOId = d.PLPOId,
                    RequestDate = d.RequestDate,
                    SupplierAddress = d.SupplierAddress,
                    SupplierId = d.SupplierIds,
                    SupplierNumber = d.SupplierNumber,
                    Tax = d.Tax,
                    Total = d.Total,
                    WorkflowToken = d.WorkflowToken,
                    WinnerSupplierId = d.WinnerSupplierId,
                    BidAndTender = bids.Where(w => w.BidAndTenderId == d.BidAndTenderId).Select(s => new BidAndTenderObj
                    {
                        BidAndTenderId = s.BidAndTenderId,
                        AmountApproved = s.AmountApproved,
                        ApprovalStatusId = s.ApprovalStatusId,
                        DateSubmitted = s.DateSubmitted,
                        DecisionResult = s.DecisionResult,
                        DecisionReultName = Convert.ToString((DecisionResult)s.DecisionResult),
                        DescriptionOfRequest = s.DescriptionOfRequest,
                        ExpectedDeliveryDate = s.ExpectedDeliveryDate,
                        Location = s.Location,
                        LPOnumber = s.LPOnumber,
                        PLPOId = s.PLPOId,
                        PRNId = s.PLPOId,
                        ProposedAmount = s.ProposedAmount.ToString(),
                        Quantity = s.Quantity,
                        RequestDate = s.RequestDate,
                        RequestingDepartmentName = _Department.companyStructures.FirstOrDefault(e => e.CompanyStructureId == s.RequestingDepartment).Name,
                        SupplierAddress = s.SupplierAddress,
                        Suppliernumber = s.Suppliernumber,
                        SupplierName = s.SupplierName,
                        RequestingDepartment = s.RequestingDepartment,
                        Total = s.Total, 
                    }).ToList(),
                    RequisitionNotes = _dataContext.purch_requisitionnote.Where(w => w.PurchaseReqNoteId == d.PurchaseReqNoteId && d.Deleted == false).Select(q => new RequisitionNoteObj
                    {
                        PurchaseReqNoteId = q.PurchaseReqNoteId,
                        ApprovalStatusId = q.ApprovalStatusId,
                        Comment = q.Comment,
                        DeliveryLocation = q.DeliveryLocation,
                        DepartmentId = q.DepartmentId,
                        Description = q.Description,
                        DocumentNumber = q.DocumentNumber,
                        ExpectedDeliveryDate = q.ExpectedDeliveryDate,
                        IsFundAvailable = q.IsFundAvailable,
                        PRNNumber = q.PRNNumber,
                        RequestBy = q.RequestBy,
                        Total = q.Total,
                        RequestDate = q.CreatedOn,

                    }).ToList(),
                    PaymentTerms = _dataContext.cor_paymentterms.Where(e => e.BidAndTenderId == d.BidAndTenderId).Select(p => new PaymentTermsObj
                    {
                        BidAndTenderId = p.BidAndTenderId,
                        Comment = p.Comment,
                        Completion = p.Completion,
                        Amount = p.Amount,
                        NetAmount = p.NetAmount,
                        Payment = p.Payment,
                        PaymentStatus = p.PaymentStatus,
                        PaymentTermId = p.PaymentTermId,
                        Phase = p.Phase,
                        ProjectStatusDescription = p.ProjectStatusDescription,
                        Status = p.Status,
                        ProposedBy = p.ProposedBy,
                        StatusName = Convert.ToString((JobProgressStatus)p.Status),
                        PaymentStatusName = Convert.ToString((PaymentStatus)p.PaymentStatus),
                    }).ToList(),
                }).ToList() ?? new List<LPOObj>();
                 
                response.Status.IsSuccessful = true;
                response.Status.Message.FriendlyMessage = staffLPOawaiting.Count() > 0 ? null : "Search Complete! No Record found";
                return response;
                     
            }
            catch (SqlException ex)
            {
                throw ex;
            }

        }
    }
}
