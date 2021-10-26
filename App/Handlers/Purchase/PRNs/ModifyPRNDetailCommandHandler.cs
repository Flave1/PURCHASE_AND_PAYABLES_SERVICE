using GOSLibraries.GOS_API_Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Puchase_and_payables.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Purchase.PRNs
{
    public class ModifyPRNDetailCommand : IRequest<DetailResp>
    {
        public int PRNDetailsId { get; set; } 
        public string Description { get; set; }

        public int Quantity { get; set; }

        public string Comment { get; set; }
        public decimal UnitPrice { get; set; }

        public IEnumerable<int> SuggestedSupplierId { get; set; }
        public decimal SubTotal { get; set; } 
        public bool? IsBudgeted { get; set; } 
        public class ModifyPRNDetailCommandHandler : IRequestHandler<ModifyPRNDetailCommand, DetailResp>
        {
            private readonly DataContext _context;
            public ModifyPRNDetailCommandHandler(DataContext context)
            {
                _context = context;
            }
           
            async Task<DetailResp> IRequestHandler<ModifyPRNDetailCommand, DetailResp>.Handle(ModifyPRNDetailCommand request, CancellationToken cancellationToken)
            {
                var resp = new DetailResp { Status = new APIResponseStatus { Message = new APIResponseMessage() } };
                try
                { 
                    var detail = await _context.purch_prndetails.FirstOrDefaultAsync(e => e.PRNDetailsId == request.PRNDetailsId);
                    if (detail == null)
                    {
                        resp.Status.Message.FriendlyMessage = "Detail not found";
                        return resp;
                    }
                    detail.Description = request.Description;
                    detail.IsBudgeted = request.IsBudgeted;
                    detail.PRNDetailsId = request.PRNDetailsId;
                    detail.Quantity = request.Quantity;
                    detail.SubTotal = request.SubTotal;
                    detail.SuggestedSupplierId = string.Join(',', request.SuggestedSupplierId);
                    detail.UnitPrice = request.UnitPrice;
                    detail.Comment = request.Comment;

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


    public class DetailResp
    { 
        public APIResponseStatus Status { get; set; }
    }

    //public class Detail 
    //{
    //    public int PRNDetailsId { get; set; }
    //    public string Description { get; set; }

    //    public int Quantity { get; set; }

    //    public decimal UnitPrice { get; set; }

    //    public decimal SubTotal { get; set; }

    //    public int PurchaseReqNoteId { get; set; }
    //    public IEnumerable<int> SuggestedSupplierId { get; set; }

    //    public bool? IsBudgeted { get; set; }
    //    public string LPONumber { get; set; }
    //    public string Comment { get; set; }
    //    public string Suppliers { get; set; }
    //    public Detail(purch_prndetails db, List<cor_supplier> suppliers)
    //    {
            
    //        Description = db.Description;
    //        IsBudgeted = db.IsBudgeted;
    //        PRNDetailsId = db.PRNDetailsId;
    //        Quantity = db.Quantity;
    //        SubTotal = db.SubTotal;
    //        SuggestedSupplierId = db.SuggestedSupplierId.Split(',').Select(int.Parse);
    //        Suppliers = string.Join(" , ", suppliers.Where(e => db.SuggestedSupplierId.Contains(db.SuggestedSupplierId)).Select(q => q.Name).ToList());
    //        SuggestedSupplierId = string.Join(',', suppliers.Select(e => e.SupplierId).Select(int.Parse);
    //        UnitPrice = db.UnitPrice;
    //        Comment = db.Comment;
    //    }
    }


