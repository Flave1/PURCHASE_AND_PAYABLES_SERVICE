using GOSLibraries;
using GOSLibraries.Enums;
using GOSLibraries.GOS_API_Response;
using GOSLibraries.GOS_Error_logger.Service;
using GOSLibraries.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Puchase_and_payables.Data;
using Puchase_and_payables.Requests;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Wangkanai.Detection.Services;

namespace Puchase_and_payables.Handlers.Requirement
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ERPActivityAttribute : Attribute, IAsyncActionFilter
    {
        public int Activity { get; set; }
        public UserActions Action { get; set; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var response = new MiddlewareResponse { Status = new APIResponseStatus { Message = new APIResponseMessage()  } };
            var userId = context.HttpContext.User?.FindFirst("userId")?.Value ?? string.Empty;
            List<string> thisUserRoleIds = new List<string>();
            List<string> thisUserRoleNames = new List<string>();
            List<string> roleActivities = new List<string>();
            var thisUserRoleCan = false;

            if (string.IsNullOrEmpty(userId))
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedObjectResult(response);
                return;
            }

            using (var scope = context.HttpContext.RequestServices.CreateScope())
            {
                try
                {
                    var scopedServices = scope.ServiceProvider;
                    var logger = scopedServices.GetRequiredService<ILoggerService>();
                    var serverRequest = scopedServices.GetRequiredService<IIdentityServerRequest>(); 
                     var userroles = await serverRequest.GetUserRolesAsync();

                    if (userroles.Status.Message.FriendlyMessage ==  GenericMiddlwareMessages.DUPLICATE_LOGIN)
                    {
                        response.Status.Message.FriendlyMessage = userroles.Status.Message.FriendlyMessage;
                        var contentResponse = new ContentResult
                        {
                            Content = JsonConvert.SerializeObject(response),
                            ContentType = "application/json",
                            StatusCode = 401
                        };
                        context.HttpContext.Response.StatusCode = 401;
                        context.Result = contentResponse;
                        return;
                    }

                    var activities = await serverRequest.GetAllActivityAsync(); 

                    thisUserRoleIds = userroles?.UserRoles?.Where(x => x?.UserId == userId).Select(x => x.RoleId).ToList();
                     
                    thisUserRoleNames = (from userRole in userroles?.UserRoles select userRole?.RoleName)?.ToList(); 

                    roleActivities = (from activity in activities.Activities
                                      join userActivityRole
                                        in userroles.UserRoleActivities on activity.ActivityId equals userActivityRole.ActivityId
                                      select userActivityRole.RoleName).ToList();
                     
                    bool hasMatch = roleActivities.Select(x => x).Intersect(thisUserRoleNames).Any(); 
                    if (hasMatch)
                    { 
                        if (Action == UserActions.Add)
                            thisUserRoleCan = userroles.UserRoleActivities.Any(x => thisUserRoleIds.Contains(x.RoleId) && x.ActivityId == Activity && x.CanAdd == true);
                        if (Action == UserActions.Approve)
                            thisUserRoleCan = userroles.UserRoleActivities.Any(x => thisUserRoleIds.Contains(x.RoleId) && x.ActivityId == Activity && x.CanApprove == true);
                        if (Action == UserActions.Delete)
                            thisUserRoleCan = userroles.UserRoleActivities.Any(x => thisUserRoleIds.Contains(x.RoleId) && x.ActivityId == Activity && x.CanDelete == true);
                        if (Action == UserActions.Update)
                            thisUserRoleCan = userroles.UserRoleActivities.Any(x => thisUserRoleIds.Contains(x.RoleId) && x.ActivityId == Activity && x.CanEdit == true);
                        if (Action == UserActions.View)
                            thisUserRoleCan = userroles.UserRoleActivities.Any(x => thisUserRoleIds.Contains(x.RoleId) && x.ActivityId == Activity && x.CanView == true);
                    } 
                    if (!thisUserRoleNames.Contains(StaticRoles.GODP))
                    {
                        if (!thisUserRoleCan)
                        {
                            response.Status.Message.FriendlyMessage = GenericMiddlwareMessages.NO_PRIVILEGE;
                            var contentResponse = new ContentResult
                            {
                                Content = JsonConvert.SerializeObject(response),
                                ContentType = "application/json",
                                StatusCode = 403
                            };
                            context.HttpContext.Response.StatusCode = 403;
                            context.Result = contentResponse;
                            return;
                        }
                    } 
                    await next();
                    return;
                }
                catch (Exception ex)
                {
                    var contentResponse = new MiddlewareResponse
                    {
                        Status = new APIResponseStatus
                        {
                            IsSuccessful = false,
                            Message = new APIResponseMessage { FriendlyMessage = ex?.Message, TechnicalMessage = ex.InnerException?.Message }
                        }
                    };
                    context.HttpContext.Response.StatusCode = 500;
                    context.Result = contentResponse;
                    return;
                }
            } 
        }
         
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ERPAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var response = new MiddlewareResponse { Status = new APIResponseStatus { IsSuccessful = false, Message = new APIResponseMessage() } };
            string userId = context.HttpContext.User?.FindFirst("userId")?.Value ?? string.Empty;
            StringValues authHeader = context.HttpContext.Request.Headers["Authorization"];

            bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            if (context == null || hasAllowAnonymous)
            {
                await next();
                return;
            }
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(authHeader))
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedObjectResult(response);
                return;
            }
            string token = authHeader.ToString().Replace("Bearer ", "").Trim();
            var handler = new JwtSecurityTokenHandler();
            var tokena = handler.ReadJwtToken(token);
            var FromDate = tokena.IssuedAt.AddHours(1);
            var EndDate = tokena.ValidTo.AddHours(1);

            var expieryMatch = DateTime.UtcNow.AddHours(1);
            if (expieryMatch > EndDate)
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedObjectResult(response);
                return;
            }

            using (var scope = context.HttpContext.RequestServices.CreateScope())
            {
                try
                {
                    IServiceProvider scopedServices = scope.ServiceProvider;

                    IDetectionService _detectionService = scopedServices.GetRequiredService<IDetectionService>();
                    IIdentityServerRequest _serverRequest = scopedServices.GetRequiredService<IIdentityServerRequest>();
                    if (_detectionService.Device.Type.ToString().ToLower() == Device.Desktop.ToString().ToLower())
                    {
                        var res = await _serverRequest.CheckTrackedAsync(token, userId);
                        if (res.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            context.HttpContext.Response.StatusCode = 401;
                            response.Status.Message.FriendlyMessage = GenericMiddlwareMessages.DUPLICATE_LOGIN;
                            context.Result = new UnauthorizedObjectResult(response);
                            return;
                        }
                    }
                    await next();
                    return;
                }
                catch (Exception ex)
                {
                    context.HttpContext.Response.StatusCode = 500;
                    response.Status.IsSuccessful = false;
                    response.Status.Message.FriendlyMessage = ex.Message;
                    response.Status.Message.TechnicalMessage = ex.ToString();
                    context.Result = response;
                    return;
                }
            }
        }
    }
}
