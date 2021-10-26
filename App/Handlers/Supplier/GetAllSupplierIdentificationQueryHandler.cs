using GODP.APIsContinuation.Repository.Interface;
using GODPAPIs.Contracts.Queries;
using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using MediatR;
using Puchase_and_payables.Contracts.Response.Supplier;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puchase_and_payables.Handlers.Supplier
{ 
    public class GetAllSupplierIdentificationQueryHandler : IRequestHandler<GetAllSupplierIdentificationQuery, IdentificationRespObj>
    {
        private readonly ISupplierRepository _supRepo;
        private readonly IIdentityServerRequest _serverRequest;
        public GetAllSupplierIdentificationQueryHandler(ISupplierRepository supplierRepository, IIdentityServerRequest serverRequest)
        { 
            _supRepo = supplierRepository;
            _serverRequest = serverRequest;
        }
        public class Commons
        {
            public int Id { get; set; }
            public int ParentId { get; set; } 
            public string Name { get; set; }
        }

        Commons ReturnCountryLookups(int Id)
        {
            if(Id != 0)
            {
                var countries = _serverRequest.GetAllCountryAsync().Result;
                var item = countries.commonLookups.FirstOrDefault(r => r.LookupId == Id);
                if (item != null)
                    return new Commons { Id = item.LookupId, Name = item.LookupName, ParentId = item.ParentId };
            }
            return new Commons();
        }
        public async Task<IdentificationRespObj> Handle(GetAllSupplierIdentificationQuery request, CancellationToken cancellationToken)
        {
            var response = new IdentificationRespObj { Indentifications = new List<IdentificationObj>(), Status = new APIResponseStatus { Message = new APIResponseMessage() } };
            var supplier = await _supRepo.GetAllSupplierIdentificationAsync(request.SupplierId);
            

            response.Indentifications = supplier.Select(d => new IdentificationObj
            {
                BusinessType = d.BusinessType,
                SupplierId = d.SupplierId,
                BusinessTypeName = Convert.ToString((BusinessType)d.BusinessType),
                ExpiryDate = d.Expiry_Date,
                HaveWorkPermit = d.HaveWorkPermit,
                Identification = d.Identification,
                IdentificationName = Convert.ToString((Identification)d.Identification),
                IdentificationNumber = d.Identification_Number,
                IdentificationId = d.IdentificationId,
                IsCorporate = d.IsCorporate,
                IncorporationDate = d.IncorporationDate,
                Nationality = d.Nationality,
                NationalityName = ReturnCountryLookups(d.Nationality).Name,
                OtherBusinessType = d.OtherBusinessType,
                Registrationnumber = d.RegistrationNumber
            }).ToList();
            response.Status.Message.FriendlyMessage = supplier.Count() > 0 ? null : "Search Complete!! No Record Found";
            return response;
        }
    }
}
