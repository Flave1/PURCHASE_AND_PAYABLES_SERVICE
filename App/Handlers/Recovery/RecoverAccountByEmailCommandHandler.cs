using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using GOSLibraries.URI;
using MediatR; 
using Microsoft.AspNetCore.Identity;
using Puchase_and_payables.Contracts.GeneralExtension;
using Puchase_and_payables.Contracts.Response.IdentityServer;
using Puchase_and_payables.Contracts.Response.Recovery;
using Puchase_and_payables.DomainObjects.Auth;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic; 
using System.Threading;
using System.Threading.Tasks; 

namespace APIGateway.AuthGrid.Recovery
{
    public class RecoverAccountByEmailCommand : IRequest<RecoveryResp>
    {
        public string Email { get; set; }
        public class RecoverAccountByEmailCommandHandler : IRequestHandler<RecoverAccountByEmailCommand, RecoveryResp>
        {
            private async Task RecoveryMail(string email,string token)
            { 
                var path = $"{_uRIs.SelfClient}#/auth/change/password?email={email}&token={token}";
                var sm = new EmailMessageObj();
                sm.Subject = $"Account Recovery";
                sm.Content = $"Please click <a href='{path}'> here </a> to change password";
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
            public RecoverAccountByEmailCommandHandler(
                IBaseURIs uRIs,
                UserManager<ApplicationUser> userManager, 
                IIdentityServerRequest  identityServer)
            {
                _uRIs = uRIs;
                _identityServer = identityServer;
                _userManager = userManager;
            }
            public async Task<RecoveryResp> Handle(RecoverAccountByEmailCommand request, CancellationToken cancellationToken)
            {
                var response = new RecoveryResp { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
                try
                {
                    var user = await _userManager.FindByEmailAsync(request.Email);

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                    var encodedToken = CustomEncoder.Base64Encode(token);

                    await RecoveryMail(request.Email, encodedToken);
                    response.Status.IsSuccessful = true;
                    response.Status.Message.FriendlyMessage = "Link to reset password has been sent to your email";
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
