using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using GOSLibraries.GOS_Error_logger.Service;
using GOSLibraries.URI;
using MediatR;
using Newtonsoft.Json;
using Puchase_and_payables.Contracts.Response.ApprovalRes;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.Repository.Purchase;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{

    public class SendLPOToApprovalCommand : IRequest<LPORegRespObj>
    {
        public int LPOId { get; set; }
        public class SendLPOToApprovalCommandHandler : IRequestHandler<SendLPOToApprovalCommand, LPORegRespObj>
        {
            private readonly IPurchaseService _repo;
            private readonly ILoggerService _logger;
            private readonly DataContext _dataContext;
            private readonly IIdentityServerRequest _serverRequest;
            private readonly IBaseURIs _uRIs;
            public SendLPOToApprovalCommandHandler(
                IPurchaseService purchaseService,
                ILoggerService loggerService,
                DataContext dataContext,
                 IBaseURIs uRIs,
                IIdentityServerRequest serverRequest)
            {
                _repo = purchaseService;
                _dataContext = dataContext;
                _uRIs = uRIs;
                _logger = loggerService;
                _serverRequest = serverRequest; 
            }
            public async Task<LPORegRespObj> Handle(SendLPOToApprovalCommand request, CancellationToken cancellationToken)
            {
                var apiResponse = new LPORegRespObj { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
                try
                {
                    var LPOObj = await _repo.GetLPOsAsync (request.LPOId); 

                    if (LPOObj == null)
                    {
                        apiResponse.Status.Message.FriendlyMessage = $"Bid Not found";
                        return apiResponse; 
                    }
                    var enumName = (ApprovalStatus)LPOObj.ApprovalStatusId;
                    if (LPOObj.ApprovalStatusId != (int)ApprovalStatus.Pending)
                    {
                        apiResponse.Status.Message.FriendlyMessage = $"Unable to push LPO with status '{enumName.ToString()}' for approval";
                        return apiResponse; 
                    }
                    var user = await _serverRequest.UserDataAsync();

                    try
                    {
                        var targetList = new List<int>();
                        targetList.Add(request.LPOId);
                        GoForApprovalRequest wfRequest = new GoForApprovalRequest
                        {
                            Comment = "LPO",
                            OperationId = (int)OperationsEnum.PurchaseLPOApproval,
                            TargetId = targetList,
                            ApprovalStatus = (int)ApprovalStatus.Pending,
                            DeferredExecution = true,
                            StaffId = user.StaffId,
                            CompanyId = user.CompanyId,
                            EmailNotification = true,
                            ExternalInitialization = false,
                            StatusId = (int)ApprovalStatus.Processing,
                            Directory_link = $"{_uRIs.MainClient}/#/purchases-and-supplier/lpo-approvals"
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
                            LPOObj.ApprovalStatusId = (int)ApprovalStatus.Processing;
                            LPOObj.WorkflowToken = res.Status.CustomToken;

                            await _repo.AddUpdateLPOAsync(LPOObj); 

                            apiResponse.PLPOId = LPOObj.PLPOId;
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
                            LPOObj.ApprovalStatusId = (int)ApprovalStatus.Approved;
                            await _repo.AddUpdateLPOAsync(LPOObj);
                            await _repo.ShareTaxToPhasesIthereIsAsync(LPOObj);

                            apiResponse.Status.IsSuccessful = true;
                            apiResponse.Status.Message.FriendlyMessage = "LPO Updated";
                            return apiResponse;
                        }
                        apiResponse.Status = res.Status;
                        return apiResponse;

                    }
                    catch (Exception ex)
                    { 
                        #region Log error to file 
                        var errorCode = ErrorID.Generate(4);
                        _logger.Error($"ErrorID : {errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}");
                        return new LPORegRespObj
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
                    return new LPORegRespObj
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
    
}
