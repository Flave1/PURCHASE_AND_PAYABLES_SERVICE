using AutoMapper;
using GODP.APIsContinuation.Repository.Interface;
using GOSLibraries.GOS_API_Response;
using GOSLibraries.GOS_Error_logger.Service;
using MediatR;
using Microsoft.AspNetCore.Http;
using Puchase_and_payables.AuthHandler;
using Puchase_and_payables.Contracts.Commands.Supplier;
using Puchase_and_payables.Contracts.Queries.Finanace;
using Puchase_and_payables.Contracts.Response;
using Puchase_and_payables.Contracts.Response.Supplier;
using Puchase_and_payables.DomainObjects.Supplier;
using Puchase_and_payables.Requests;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Supplier
{
    
    public class AddUpdateBankAccountdetailCommandHandler : IRequestHandler<AddUpdateBankAccountdetailCommand, SupplierAccountRegRespObj>
    {
        private readonly ILoggerService _logger;
        private readonly ISupplierRepository _supRepo;    
        private readonly IFinanceServerRequest _financeServer;
        public AddUpdateBankAccountdetailCommandHandler(
            ILoggerService loggerService, ISupplierRepository supplierRepository,
             IIdentityServerRequest serverRequest, IFinanceServerRequest financeServerRequest )
        {
            _financeServer = financeServerRequest;
            _logger = loggerService; 
            _supRepo = supplierRepository; 
        }
        public async Task<SupplierAccountRegRespObj> Handle(AddUpdateBankAccountdetailCommand request, CancellationToken cancellationToken)
        {
            var response = new SupplierAccountRegRespObj { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
            try
            { 
                var verificationObj = new VerifyAccount();
                verificationObj.account_bank = request.Bank;
                verificationObj.account_number = request.AccountNumber;
                var verificationRresponse = await _financeServer.VerifyAccountNumber(verificationObj);

                if (string.IsNullOrEmpty(verificationRresponse.status))
                {
                    response.Status.Message.FriendlyMessage = verificationRresponse.message;
                    //return response;
                }
                if(verificationRresponse.status != "success")
                {
                    response.Status.Message.FriendlyMessage = verificationRresponse.message;
                    //return response;
                }

                cor_bankaccountdetail item = new cor_bankaccountdetail(); 
                item.AccountName = request.AccountName;
                item.AccountNumber = request.AccountNumber;
                item.BankAccountDetailId = request.BankAccountDetailId;
                item.BVN = request.BVN;
                item.SupplierId = request.SupplierId;
                item.BankCode = request.Bank;

                await _supRepo.AddUpdateBankAccountdetailsAsync(item);
                response.Status.Message.FriendlyMessage = "Successful";
                response.Status.IsSuccessful = true;
                return response;
            }
            catch (Exception ex)
            {
                #region Log error to file 
                var errorCode = ErrorID.Generate(4);
                _logger.Error($"ErrorID : {errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}");
                return new SupplierAccountRegRespObj
                {
                    Status = new APIResponseStatus
                    {
                        IsSuccessful = false,
                        Message = new APIResponseMessage
                        {
                            FriendlyMessage = "Error occured!! Unable to process item",
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
