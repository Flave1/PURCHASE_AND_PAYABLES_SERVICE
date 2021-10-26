using GODP.APIsContinuation.Repository.Interface;
using GODPAPIs.Contracts.Commands.Supplier; 
using GOSLibraries.GOS_API_Response;
using GOSLibraries.GOS_Error_logger.Service; 
using MediatR;
using Microsoft.Data.SqlClient;
using Puchase_and_payables.Contracts.Response;
using Puchase_and_payables.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GODP.APIsContinuation.Handlers.Supplier
{
    public class DeleteSupplierFinancialDetalsCommand : IRequest<DeleteRespObj>
    {
        public int financialDetailId { get; set; }
        public class DeleteSupplierFinancialDetalsCommandHandler : IRequestHandler<DeleteSupplierFinancialDetalsCommand, DeleteRespObj>
        {
            private readonly ISupplierRepository _supRepo;
            private readonly ILoggerService _logger;
            private readonly DataContext _dataContext;
            public DeleteSupplierFinancialDetalsCommandHandler(ISupplierRepository supplierRepository, DataContext dataContext, ILoggerService loggerService)
            {
                _supRepo = supplierRepository;
                _dataContext = dataContext;
                _logger = loggerService;
            }
            public async Task<DeleteRespObj> Handle(DeleteSupplierFinancialDetalsCommand request, CancellationToken cancellationToken)
            {
                var response = new DeleteRespObj { Deleted = false, Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
                try
                { 
                    var res = _supRepo.DeleteFinancialDetail(request.financialDetailId);

                    response.Deleted = true;
                    response.Status.IsSuccessful = true;
                    response.Status.Message.FriendlyMessage = "Succcessful";
                    return await Task.Run(() =>  response);
                }
                catch (SqlException ex)
                {
                    #region Log error to file 
                    var errorCode = ErrorID.Generate(4);
                    _logger.Error($"ErrorID :  {errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}"); 
                    response.Status.Message.FriendlyMessage = "Unable to process request";
                    response.Status.Message.TechnicalMessage = ex.ToString();
                    return response;
                    #endregion
                }
            }
        }
    }
    
}
