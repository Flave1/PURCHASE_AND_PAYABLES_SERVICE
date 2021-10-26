using GODP.APIsContinuation.Repository.Interface;
using GODPAPIs.Contracts.RequestResponse.Supplier;
using MediatR;
using OfficeOpenXml;
using Puchase_and_payables.Contracts.Response.Supplier;
using Puchase_and_payables.Data;
using Puchase_and_payables.Requests;
using System; 
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Supplier.Settup.Uploads_Downloads
{
    public class DownloadSupplierTypeQuery : IRequest<byte[]>
    {
        public class DownloadSupplierTypeQueryHandler : IRequestHandler<DownloadSupplierTypeQuery, byte[]>
        {
            private readonly DataContext _dataContext;
            private readonly IFinanceServerRequest _financeServer;
            public DownloadSupplierTypeQueryHandler(DataContext dataContext, IFinanceServerRequest financeServer)
            {
                _financeServer = financeServer;
                _dataContext = dataContext; 
            }
            public async Task<byte[]> Handle(DownloadSupplierTypeQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    Byte[] File = new Byte[0];
                    var _DomainList =  _dataContext.cor_suppliertype.Where(e=> e.Deleted == false).ToList();
                    var subgls = await _financeServer.GetAllSubglAsync();
                    DataTable dt = new DataTable();
                    dt.Columns.Add("Supplier Type");
                    dt.Columns.Add("Tax Applicable");
                    dt.Columns.Add("Debit GL");
                    dt.Columns.Add("Credit GL");
                    var _ContractList = _DomainList.Select(a => new SuppliertypeObj
                    {
                        SupplierTypeName = a.SupplierTypeName,
                        TaxApplicable = a.TaxApplicable.Split(',').Select(int.Parse).ToList(), 
                        CreditGL = a.CreditGL,
                        DebitGL = a.DebitGL
                    }).ToList();

                     
                    if (_ContractList.Count() > 0)
                    {
                        foreach (var itemRow in _ContractList)
                        { 
                            var row = dt.NewRow();
                            row["Supplier Type"] = itemRow.SupplierTypeName;
                            row["Tax Applicable"] = string.Join(',', _dataContext.cor_taxsetup.Where(w => itemRow.TaxApplicable.Contains(w.TaxSetupId)).Select(e => e.TaxName).ToList()); 
                            row["Debit GL"] = subgls.SubGls.FirstOrDefault(e => e.subGLId == itemRow.DebitGL)?.subGLCode;
                            row["Credit GL"] = subgls.SubGls.FirstOrDefault(e => e.subGLId == itemRow.CreditGL)?.subGLCode;
                            dt.Rows.Add(row);
                        }
                        
                        if (_ContractList != null)
                        {
                            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                            using (ExcelPackage pck = new ExcelPackage())
                            {
                                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("SupplierType");
                                ws.DefaultColWidth = 20;
                                ws.Cells["A1"].LoadFromDataTable(dt, true, OfficeOpenXml.Table.TableStyles.None);
                                File = pck.GetAsByteArray();
                            }
                        } 
                    }
                    return File; 
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}
