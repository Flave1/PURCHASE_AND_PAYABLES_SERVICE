using GODP.APIsContinuation.DomainObjects.Supplier;
using GODP.APIsContinuation.Repository.Interface;
using GODPAPIs.Contracts.RequestResponse.Supplier;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Puchase_and_payables.Contracts.GeneralExtension;
using Puchase_and_payables.Contracts.Response.Supplier;
using Puchase_and_payables.Data;
using Puchase_and_payables.DomainObjects.Supplier;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Supplier.Settup
{
    public class UploadSupplierTypeCommand : IRequest<FileUploadRespObj>
    {
        public class UploadSupplierTypeCommandHandler : IRequestHandler<UploadSupplierTypeCommand, FileUploadRespObj>
        {
            private readonly IHttpContextAccessor _accessor; 
            private readonly IFinanceServerRequest _financeServer;
            private readonly DataContext _dataContext;
            public UploadSupplierTypeCommandHandler(
                IHttpContextAccessor httpContextAccessor,
                DataContext dataContext,
                IFinanceServerRequest financeServer) 
            {
                _financeServer = financeServer;
                _dataContext = dataContext;
                _accessor = httpContextAccessor;
            }
            public async Task<FileUploadRespObj> Handle(UploadSupplierTypeCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    var apiResponse = new FileUploadRespObj { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };

                    var files = _accessor.HttpContext.Request.Form.Files;

                    var byteList = new List<byte[]>();
                    foreach (var fileBit in files)
                    {
                        if (fileBit.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                await fileBit.CopyToAsync(ms);
                                byteList.Add(ms.ToArray());
                            }
                        }

                    }  
                    var subgls = await _financeServer.GetAllSubglAsync();
                    var uploadedRecord = new List<SuppliertypeObj>();
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    if (byteList.Count() > 0)
                    {
                        foreach (var byteItem in byteList)
                        {
                            using (MemoryStream stream = new MemoryStream(byteItem))
                            using (ExcelPackage excelPackage = new ExcelPackage(stream))
                            { 
                                ExcelWorksheet workSheet = excelPackage.Workbook.Worksheets[0];
                                int totalRows = workSheet.Dimension.Rows;
                                int totalColumns = workSheet.Dimension.Columns;

                                if (totalColumns != 4)
                                {
                                    apiResponse.Status.Message.FriendlyMessage = "4 Columns Expected";
                                    return apiResponse;
                                }

                                for (int i = 2; i <= totalRows; i++)
                                {
                                    var item = new SuppliertypeObj
                                    {
                                        ExcelLineNumber = i,
                                        SupplierTypeName = workSheet.Cells[i, 1]?.Value != null ? workSheet.Cells[i, 1]?.Value.ToString() : string.Empty,
                                        TaxApplicableName = workSheet.Cells[i, 2]?.Value != null ? workSheet.Cells[i, 2]?.Value.ToString() : string.Empty,
                                        DebitSubGlCode = workSheet.Cells[i, 3]?.Value != null ? workSheet.Cells[i, 3]?.Value.ToString() : string.Empty,
                                        CreditSubGlCode = workSheet.Cells[i, 4]?.Value != null ? workSheet.Cells[i, 4]?.Value.ToString() : string.Empty,
                                    };
                                    uploadedRecord.Add(item);
                                }
                            }
                        } 
                    }
                     
                    if (uploadedRecord.Count > 0)
                    {
                        var listOftaxt = new List<int>();

                        foreach (var item in uploadedRecord)
                        {
                            if (string.IsNullOrEmpty(item.TaxApplicableName))
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"No Tax Applicable found Detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }
                            else
                            {
                                var taxNames = item.TaxApplicableName.Trim().ToLower().Split(','); 
                                foreach(var tx in taxNames)
                                { 
                                    var taxes = _dataContext.cor_taxsetup.FirstOrDefault(a => taxNames.Contains(a.TaxName));
                                    if(taxes == null)
                                    {
                                        apiResponse.Status.Message.FriendlyMessage = $"Unidentified Tax name Detected on line {item.ExcelLineNumber}";
                                        return apiResponse;
                                    }
                                    listOftaxt.Add(taxes.TaxSetupId);
                                } 
                            }
                            if (string.IsNullOrEmpty(item.SupplierTypeName))
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"Empty Supplier Type Name Detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }

                            if (string.IsNullOrEmpty(item.DebitSubGlCode))
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"Debit Sub Gl code is empty detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }
                            else
                            {
                                item.DebitGL = subgls.SubGls.FirstOrDefault(d => d.subGLCode == item.DebitSubGlCode)?.subGLId ?? 0;
                                if (item.DebitGL == 0)
                                {
                                    apiResponse.Status.Message.FriendlyMessage = $"Invalid Debit gl detected on line {item.ExcelLineNumber}";
                                    return apiResponse;
                                }
                            }


                            if (string.IsNullOrEmpty(item.CreditSubGlCode))
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"Credit Sub Gl code is empty detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }
                            else
                            {
                                item.CreditGL = subgls.SubGls.FirstOrDefault(d => d.subGLCode == item.DebitSubGlCode)?.subGLId ?? 0;
                                if (item.DebitGL == 0)
                                {
                                    apiResponse.Status.Message.FriendlyMessage = $"Invalid Credit gl detected on line {item.ExcelLineNumber}";
                                    return apiResponse;
                                }
                            }

                            var db_item = _dataContext.cor_suppliertype.FirstOrDefault(c => c.SupplierTypeName.ToLower() == item.SupplierTypeName.ToLower() && c.Deleted == false);

                            if (db_item != null)
                            {
                                db_item.SupplierTypeName = item?.SupplierTypeName;
                                db_item.TaxApplicable = string.Join(',', listOftaxt);
                                db_item.CreditGL = item.CreditGL;
                                db_item.DebitGL = item.DebitGL;
                            }
                            else
                            {
                                db_item = new cor_suppliertype();
                                db_item.SupplierTypeName = item?.SupplierTypeName;
                                db_item.TaxApplicable = string.Join(',', listOftaxt);
                                db_item.CreditGL = item.CreditGL;
                                db_item.DebitGL = item.DebitGL;
                                _dataContext.cor_suppliertype.Add(db_item);
                            }
                        }
                        _dataContext.SaveChanges();
                    }
                    apiResponse.Status.IsSuccessful = true;
                    apiResponse.Status.Message.FriendlyMessage = "Successful";
                    return apiResponse;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}
