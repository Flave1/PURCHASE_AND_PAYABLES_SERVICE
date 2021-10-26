using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Puchase_and_payables.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Report
{
    public class DashBoardCount
    {
        public long LPOCount { get; set; } = 0;
        public long BIDCount { get; set; } = 0;
        public int InvoiceCount { get; set; } = 0;
        public long Adverts { get; set; } = 0;
    }

    public class DashBoardCountResp
    {
        public DashBoardCount DashBoardCount { get; set; }
        public APIResponseStatus Status { get; set; } = new APIResponseStatus { Message = new APIResponseMessage() };
    }
    public class SupplierDashboardCountQuery : IRequest<DashBoardCountResp>
    {
       public int SupplierId { get; set; }
        public class SupplierDashboardCountQueryHandler : IRequestHandler<SupplierDashboardCountQuery, DashBoardCountResp>
        { 
            private readonly DataContext _context;
            public SupplierDashboardCountQueryHandler(DataContext dataContext)
            {
                _context = dataContext;
            }
            async Task<DashBoardCountResp> IRequestHandler<SupplierDashboardCountQuery, DashBoardCountResp>.Handle(SupplierDashboardCountQuery request, CancellationToken cancellationToken)
            {
                var respsonse = new DashBoardCountResp { DashBoardCount = new DashBoardCount() };

                respsonse.DashBoardCount.LPOCount = _context.purch_plpo.Count(s => request.SupplierId == s.WinnerSupplierId);
                respsonse.DashBoardCount.Adverts = _context.cor_bid_and_tender.Count(d => d.ApprovalStatusId == (int)ApprovalStatus.Awaiting);
                respsonse.DashBoardCount.BIDCount = _context.cor_bid_and_tender.Count(d => d.SupplierId == request.SupplierId);
                respsonse.DashBoardCount.InvoiceCount = _context.purch_invoice.Count(d => d.SupplierId == request.SupplierId);
                return respsonse;
            }
        }
    }

}
