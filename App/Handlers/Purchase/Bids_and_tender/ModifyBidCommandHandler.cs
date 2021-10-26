using GOSLibraries.GOS_API_Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Puchase_and_payables.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase.PRNs
{
    public class Request
    {
        public int PaymentTermId { get; set; }
        public int Phase { get; set; }
        public double Payment { get; set; }
        public decimal Amount { get; set; }
        public string ProjectStatusDescription { get; set; }
        public double Completion { get; set; }
        public string Comment { get; set; }
    }
    public class ModifyBidCommand : IRequest<bidResp>
    {
        public List<Request> Request { get; set; }
        public class ModifyBidCommandHandler : IRequestHandler<ModifyBidCommand, bidResp>
        {
            private readonly DataContext _context;
            public ModifyBidCommandHandler(DataContext context)
            {
                _context = context;
            }
           
            async Task<bidResp> IRequestHandler<ModifyBidCommand, bidResp>.Handle(ModifyBidCommand request, CancellationToken cancellationToken)
            {
                var resp = new bidResp { Status = new APIResponseStatus { Message = new APIResponseMessage() } };
                if (!request.Request.Any())
                {
                    resp.Status.Message.FriendlyMessage = "No Bid found";
                    return resp;
                }
                if(request.Request.Sum(e => e.Payment) != 100)
                {
                    resp.Status.Message.FriendlyMessage = "Invalid completion value detected";
                    return resp;
                }
                try
                {  
                    foreach(var item in request.Request)
                    {
                        var term = await _context.cor_paymentterms.FirstOrDefaultAsync(e => e.PaymentTermId == item.PaymentTermId);
                        if (term == null)
                        {
                            resp.Status.Message.FriendlyMessage = "bid not found";
                            return resp;
                        }

                        var thisBid = _context.cor_bid_and_tender.FirstOrDefault(e => e.BidAndTenderId == term.BidAndTenderId);
                        if(thisBid != null)
                        {
                            thisBid.AmountApproved = request.Request.Sum(d => d.Amount);
                        }
                        
                        term.Phase = item.Phase;
                        term.Payment = item.Payment;
                        term.ProjectStatusDescription = item.ProjectStatusDescription;
                        term.Completion = item.Completion;
                        term.Comment = item.Comment;
                        term.Amount = item.Amount;
                    }

                    await _context.SaveChangesAsync();

                    resp.Status.IsSuccessful = true;
                    resp.Status.Message.FriendlyMessage = "Successful";
                    return resp;
                }
                catch (Exception e)
                { 
                    throw e;
                }
            }
        }
    }

    public class bidResp
    {
        public APIResponseStatus Status { get; set; }
    }



}


