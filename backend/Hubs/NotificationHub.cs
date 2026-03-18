using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ApiProject.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // JWT token'dan email'i al
            var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
            
            if (!string.IsNullOrEmpty(email))
            {
                // Email'e göre grup oluştur (özel karakterleri temizle)
                var groupName = $"user_{email.ToLower().Replace("@", "_at_").Replace(".", "_")}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                
                await Clients.Caller.SendAsync("JoinedGroup", groupName);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // JWT token'dan email'i al
            var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
            
            if (!string.IsNullOrEmpty(email))
            {
                var groupName = $"user_{email.ToLower().Replace("@", "_at_").Replace(".", "_")}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
