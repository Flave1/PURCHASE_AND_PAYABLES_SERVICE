using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using GOSLibraries.GOS_Error_logger.Service;
using GOSLibraries.URI;
using MediatR;
using Newtonsoft.Json;
using Puchase_and_payables.Contracts.Commands.Purchase;
using Puchase_and_payables.Contracts.Response.ApprovalRes;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.DomainObjects.Bid_and_Tender;
using Puchase_and_payables.DomainObjects.Purchase;
using Puchase_and_payables.Repository.Purchase;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{
  
    public class SendSupplierBidAndTenderToApprovalCommandHandler : IRequestHandler<SendSupplierBidAndTenderToApprovalCommand, BidAndTenderRegRespObj>
    {
        private readonly IPurchaseService _repo;
        private readonly ILoggerService _logger;
        private readonly DataContext _dataContext;
        private readonly IIdentityServerRequest _serverRequest;
        private readonly IBaseURIs _uRIs;
        public SendSupplierBidAndTenderToApprovalCommandHandler(
            IPurchaseService purchaseService,
            ILoggerService loggerService,
            DataContext dataContext,
            IBaseURIs uRIs,
            IIdentityServerRequest serverRequest)
        {
            _uRIs = uRIs;
            _repo = purchaseService;
            _dataContext = dataContext;
            _logger = loggerService;
            _serverRequest = serverRequest; 
        }
        public async Task<BidAndTenderRegRespObj> Handle(SendSupplierBidAndTenderToApprovalCommand request, CancellationToken cancellationToken)
        {
            var apiResponse = new BidAndTenderRegRespObj { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
            try
            {
                var bidAndTenderObj = await _repo.GetBidAndTender(request.BidAndTenderId);

                if (bidAndTenderObj == null)
                {
                    apiResponse.Status.Message.FriendlyMessage = $"Bid Not found";
                    return apiResponse; 
                }
                var enumName = (ApprovalStatus)bidAndTenderObj.ApprovalStatusId;
                if (bidAndTenderObj.ApprovalStatusId != (int)ApprovalStatus.Pending)
                {
                    apiResponse.Status.Message.FriendlyMessage = $"Unable to push supplier bid with status '{enumName.ToString()}' for approval";
                    return apiResponse; 
                }

                var _ThisBidLPO = await _repo.GetLPOByNumberAsync(bidAndTenderObj.LPOnumber);

                if (_ThisBidLPO.WinnerSupplierId > 0)
                {
                    apiResponse.Status.Message.FriendlyMessage = $"Supplier already taken for the item associated to this bid";
                    return apiResponse; 
                }

                if(bidAndTenderObj.Paymentterms.Count() > 0 && bidAndTenderObj.Paymentterms.Count(q => q.ProposedBy == (int)Proposer.STAFF) == 0)
                {
                    apiResponse.Status.Message.FriendlyMessage = $"No Payment Plan Found";
                    return apiResponse;
                }

                var user = await _serverRequest.UserDataAsync();

                try
                {
                    var targetList = new List<int>();
                    targetList.Add(bidAndTenderObj.BidAndTenderId);
                    GoForApprovalRequest wfRequest = new GoForApprovalRequest
                    {
                        Comment = "Bid and tender",
                        OperationId = (int)OperationsEnum.BidAndTenders,
                        TargetId = targetList,
                        ApprovalStatus = (int)ApprovalStatus.Pending,
                        DeferredExecution = true,
                        StaffId = user.StaffId,
                        CompanyId = user.CompanyId,
                        EmailNotification = true,
                        ExternalInitialization = false,
                        StatusId = (int)ApprovalStatus.Processing,
                        Directory_link = $"{_uRIs.MainClient}/#/purchases-and-supplier/bids-approvals"
                    };

                    var result = await _serverRequest.GotForApprovalAsync(wfRequest);

                    if (!result.IsSuccessStatusCode)
                    {
                        apiResponse.Status.Message.FriendlyMessage = $"{result.ReasonPhrase} {result.StatusCode}";
                        return apiResponse;
                    }
                    var stringData = await result.Content.ReadAsStringAsync();
                    GoForApprovalRespObj res = JsonConvert.DeserializeObject<GoForApprovalRespObj>(stringData);

                    if (res.ApprovalProcessStarted)
                    {
                        bidAndTenderObj.ApprovalStatusId = (int)ApprovalStatus.Processing;
                        bidAndTenderObj.WorkflowToken = res.Status.CustomToken;
                        await _repo.AddUpdateBidAndTender(bidAndTenderObj); 

                        apiResponse.BidAndTenderId = bidAndTenderObj.BidAndTenderId;
                        apiResponse.Status = res.Status;
                        return apiResponse;
                    }

                    if (res.EnableWorkflow || !res.HasWorkflowAccess)
                    { 
                        apiResponse.Status.Message = res.Status.Message;
                        return apiResponse;
                    }
                    if (!res.EnableWorkflow)
                    {
                        await _repo.ApproveCurrentBidForThisLPO(bidAndTenderObj, _ThisBidLPO);

                        await _repo.SendEmailToSuppliersSelectedAsync(bidAndTenderObj.SupplierId, bidAndTenderObj.DescriptionOfRequest, _ThisBidLPO.PLPOId);

                        await _repo.LinkThisProposalsToCurrentLPOAsync(bidAndTenderObj);

                        await _repo.DisapproveOtherLostbids(bidAndTenderObj);

                        var thisBidLpo = _repo.BuildThisBidLPO(_ThisBidLPO, bidAndTenderObj);

                        await _repo.AddUpdateLPOAsync(thisBidLpo); 
                    }
                    apiResponse.Status.IsSuccessful = true;
                    apiResponse.Status.Message.FriendlyMessage = "LPO Generated";
                    return apiResponse;

                }
                catch (Exception ex)
                { 
                    #region Log error to file 
                    var errorCode = ErrorID.Generate(4);
                    _logger.Error($"ErrorID : {errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}");
                    return new BidAndTenderRegRespObj
                    {

                        Status = new APIResponseStatus
                        {
                            Message = new APIResponseMessage
                            {
                                FriendlyMessage = "Error occured!! Please try again later",
                                MessageId = errorCode,
                                TechnicalMessage = $"ErrorID : {errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}"
                            }
                        }
                    };
                    #endregion
                } 
            }
            catch (Exception ex)
            {
                #region Log error to file 
                var errorCode = ErrorID.Generate(4);
                _logger.Error($"ErrorID : {errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}");
                return new BidAndTenderRegRespObj
                {

                    Status = new APIResponseStatus
                    {
                        Message = new APIResponseMessage
                        {
                            FriendlyMessage = "Error occured!! Please try again later",
                            MessageId = errorCode,
                            TechnicalMessage = $"ErrorID : {errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}"
                        }
                    }
                };
                #endregion
            }
        }
        
    }
}
