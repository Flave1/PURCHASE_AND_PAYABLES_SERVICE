using GODP.APIsContinuation.Repository.Interface;
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
    public class UploadTaxSetupCommand : IRequest<FileUploadRespObj>
    {
        public class UploadTaxSetupCommandHandler : IRequestHandler<UploadTaxSetupCommand, FileUploadRespObj>
        {
            private readonly IHttpContextAccessor _accessor; 
            private readonly DataContext _dataContext; 
            private readonly IFinanceServerRequest _financeServer;
            public UploadTaxSetupCommandHandler(
                DataContext dataContext,
                IFinanceServerRequest financeServer,
                IHttpContextAccessor httpContextAccessor) 
            {
                _dataContext = dataContext;
                _financeServer = financeServer; 
                _accessor = httpContextAccessor; 
            }
            public async Task<FileUploadRespObj> Handle(UploadTaxSetupCommand request, CancellationToken cancellationToken)
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

                    var uploadedRecord = new List<TaxsetupObj>();
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
                                    var item = new TaxsetupObj
                                    {
                                        ExcelLineNumber = i,
                                        TaxName = workSheet.Cells[i, 1]?.Value != null ? workSheet.Cells[i, 1]?.Value.ToString() : string.Empty,
                                        Percentage = workSheet.Cells[i, 2]?.Value != null ? Convert.ToDouble(workSheet.Cells[i, 2]?.Value.ToString()) :0,
                                        Type = workSheet.Cells[i, 3]?.Value != null ? workSheet.Cells[i, 3]?.Value.ToString() : string.Empty,
                                        SubGlName = workSheet.Cells[i, 4]?.Value != null ? workSheet.Cells[i, 4]?.Value.ToString() : string.Empty,
                                    };
                                    uploadedRecord.Add(item);
                                }
                            }
                        }

                    }


                    var subgls = await _financeServer.GetAllSubglAsync();
                    cor_taxsetup db_item = new cor_taxsetup();
                    if (uploadedRecord.Count > 0)
                    {
                        foreach (var item in uploadedRecord)
                        {
                            if (string.IsNullOrEmpty(item.TaxName))
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"Empty tax name detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }
                            if (string.IsNullOrEmpty(item.SubGlName))
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"Sub Gl code is empty detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }
                            else
                            {
                                item.SubGL = subgls.SubGls.FirstOrDefault(d => d.subGLCode == item.SubGlName)?.subGLId ?? 0;
                                if(item.SubGL == 0)
                                {
                                    apiResponse.Status.Message.FriendlyMessage = $"Invalid gl detected on line {item.ExcelLineNumber}";
                                    return apiResponse;
                                }
                            }
                            if (item.Percentage < 1 || item.Percentage > 100)
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"Invalid percentage detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }
                            if (string.IsNullOrEmpty(item.Type))
                            {
                                apiResponse.Status.Message.FriendlyMessage = $"Type is empty detected on line {item.ExcelLineNumber}";
                                return apiResponse;
                            }
                            db_item = _dataContext.cor_taxsetup.FirstOrDefault(c => c.TaxName.ToLower() == item.TaxName.ToLower() && c.Deleted == false);
                            if (db_item != null)
                            {
                                db_item.TaxName = item.TaxName;
                                db_item.TaxSetupId = db_item.TaxSetupId;
                                db_item.Percentage = item.Percentage;
                                db_item.SubGL = db_item.SubGL;
                                db_item.Type = item?.Type;  
                            }
                            else
                            {
                                db_item = new cor_taxsetup();
                                db_item.TaxName = item.TaxName;
                                db_item.Percentage = item.Percentage;
                                db_item.SubGL = item.SubGL;
                                db_item.Type = item?.Type;
                                _dataContext.cor_taxsetup.Add(db_item);
                            }
                        }
                        await _dataContext.SaveChangesAsync();
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
