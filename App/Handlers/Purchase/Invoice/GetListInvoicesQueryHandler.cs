using GODP.APIsContinuation.Repository.Interface;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Puchase_and_payables.Contracts.Queries.Purchases;
using Puchase_and_payables.Contracts.Response.Payment;
using Puchase_and_payables.Contracts.Response.Purchase;
using Puchase_and_payables.Data;
using Puchase_and_payables.Repository.Invoice;
using Puchase_and_payables.Repository.Purchase;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase
{ 
    public class GetListInvoicesQueryHandler : IRequestHandler<GetListInvoicesQuery, InvoiceRespObj>
    { 
        private readonly ISupplierRepository _supRepo;
        private readonly IInvoiceService _invoiceService;
        private readonly DataContext _dataContext;
        public GetListInvoicesQueryHandler( 
            ISupplierRepository supplierRepository,
            DataContext dataContext,
            IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService; 
            _supRepo = supplierRepository;
            _dataContext = dataContext;
        }

     
        public async Task<InvoiceRespObj> Handle(GetListInvoicesQuery request, CancellationToken cancellationToken)
        {
            var response = new InvoiceRespObj { Invoices = new List<InvoiceObj>(), Status = new APIResponseStatus { Message = new APIResponseMessage() } };
            await Task.Run(()=> response.Invoices = _dataContext.inv_invoice.Select(d => new InvoiceObj
            {
                InvoiceId = d.InvoiceId,
                LPONumber = d.LPONumber,
                AmountPayable = d.AmountPayable,
                DescriptionOfRequest = d.Description,
                ExpectedDeliveryDate = d.DeliveryDate,
                Location = d.Address,
                RequestDate = d.RequestDate,
                InvoiceNumber = d.InvoiceNumber,
                Amount = d.Amount,
                AmountPaid = d.AmountPaid,
                SupplierId = d.SupplierId,
                Supplier = _dataContext.cor_supplier.FirstOrDefault(s => s.SupplierId == d.SupplierId).Name,
                PaymentTermId = d.PaymentTermId
            }).ToList());
      
            response.Status.IsSuccessful = true;
            return response;
        }
    }
}
