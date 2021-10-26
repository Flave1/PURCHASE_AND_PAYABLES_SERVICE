using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using GOSLibraries.GOS_Error_logger.Service;
using MediatR;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.Repository.Purchase;
using Puchase_and_payables.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase.LPOs
{
    public class RespondToLPOCommand : IRequest<LPORegRespObj>
    {
        public int LPOId { get; set; }
        public bool IsRejected { get; set; }
        public class RespondToLPOCommandHandler : IRequestHandler<RespondToLPOCommand, LPORegRespObj>
        {
            private readonly DataContext _dataContext;
            public readonly IPurchaseService _purchaseService;
            public readonly IIdentityServerRequest _serverRequest;
            public readonly ILoggerService _logger;
            public RespondToLPOCommandHandler(
                DataContext dataContext,
                IPurchaseService purchaseService,
                IIdentityServerRequest request,
                ILoggerService loggerService)
            {
                _purchaseService = purchaseService;
                _serverRequest = request;
                _logger = loggerService;
                _dataContext = dataContext;
            }
            public async Task<LPORegRespObj> Handle(RespondToLPOCommand request, CancellationToken cancellationToken)
            {
                var response = new LPORegRespObj { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
                try
                {
                    var rejectedlpo = _dataContext.purch_plpo.Find(request.LPOId);
                    if (rejectedlpo == null)
                    {
                        response.Status.Message.FriendlyMessage = "Unable to identify this LPO";
                        return response;
                    }
                    var rejectedLpoBid = await _purchaseService.GetBidAndTender(rejectedlpo.BidAndTenderId);
                    if (rejectedLpoBid == null)
                    {
                        response.Status.Message.FriendlyMessage = "Error Occurred";
                        return response;
                    }

                    if (rejectedlpo.ApprovalStatusId == (int)ApprovalStatus.Approved)
                    {
                        response.Status.Message.FriendlyMessage = "LPO Already approved";
                        return response;
                    }


                    await _purchaseService.SendEmailToSuppliersWhenBidIsRejectedAsync(rejectedlpo.WinnerSupplierId, rejectedlpo.Description);

                    rejectedlpo.WinnerSupplierId = 0;
                    await _purchaseService.RejectThisBid(rejectedLpoBid);

                    await _purchaseService.ReactivateOtherBids(rejectedLpoBid, request.LPOId);

                    response.Status.IsSuccessful = true;
                    response.Status.Message.FriendlyMessage = "Successfully rejected this LPO";
                    return response; 
                }
                catch (Exception ex)
                {
                    response.Status.Message.FriendlyMessage = ex.Message;
                    return response;
                }
            }
        }
    }

}
