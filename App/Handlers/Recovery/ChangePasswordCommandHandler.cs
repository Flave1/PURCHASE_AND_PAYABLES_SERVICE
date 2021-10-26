using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using GOSLibraries.URI;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Puchase_and_payables.Contracts.GeneralExtension;
using Puchase_and_payables.Contracts.Response.IdentityServer;
using Puchase_and_payables.Contracts.Response.Recovery;
using Puchase_and_payables.DomainObjects.Auth;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace APIGateway.AuthGrid.Recovery
{
    public class ChangePasswordCommand : IRequest<RecoveryResp>
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string Token { get; set; }
        public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, RecoveryResp>
        {
            private async Task RecoveryMail(string email)
            { 

                var path = $"{_uRIs.SelfClient}#/auth/login";
                var sm = new EmailMessageObj();
                sm.Subject = $"Account Recovery";
                sm.Content = $"Account recovery was successful. <br> click <a href='{path}'> here </a> to login into your account";
                sm.SendIt = true;
                sm.SaveIt = true;
                sm.Template = (int)EmailTemplate.LoginDetails;
                sm.ToAddresses = new List<EmailAddressObj>();
                sm.FromAddresses = new List<EmailAddressObj>();
                sm.ToAddresses.Add(new EmailAddressObj { Address = email, Name = email });
                await _identityServer.SendMessageAsync(sm);
            }

            private readonly IBaseURIs _uRIs;
            private readonly IIdentityServerRequest _identityServer; 
            private readonly UserManager<ApplicationUser> _userManager;
            public ChangePasswordCommandHandler(IBaseURIs uRIs, IIdentityServerRequest identityServerRequest,
                  UserManager<ApplicationUser> userManager)
            {
                _uRIs = uRIs;
                _identityServer = identityServerRequest;
                _userManager = userManager; 
            }
            public async Task<RecoveryResp> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
            {
                var response = new RecoveryResp { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
                try
                {
                    var user = await _userManager.FindByEmailAsync(request.Email);
                    if(user != null)
                    {
                        var encodedToken = CustomEncoder.Base64Decode(request.Token);
                        var passChanged = await _userManager.ResetPasswordAsync(user, encodedToken, request.NewPassword);
                        if (!passChanged.Succeeded)
                        {
                            response.Status.Message.FriendlyMessage = passChanged.Errors.FirstOrDefault().Description;
                            return response;
                        }
                    }
                    await RecoveryMail(request.Email);
                    response.Status.IsSuccessful = true;
                    response.Status.Message.FriendlyMessage = "Password has successfully been changed";
                    return response;
                }
                catch (Exception ex)
                {
                    response.Status.Message.FriendlyMessage = "Unable to process request";
                    response.Status.Message.TechnicalMessage = ex.ToString();
                    return response;
                }
            }
        }
    }
   
}
