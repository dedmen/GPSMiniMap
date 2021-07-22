using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace GPSMiniMap.Hubs
{
    public class ChatHub : Hub
    {
        public async Task UpdatePosition(string message)
        {
            //await Clients.All.SendAsync("UpdatePosition", message); //#TODO others only
            await Clients.Others.SendAsync("UpdatePosition", message);
        }

        public async Task ChatMessage(string message)
        {
            await Clients.All.SendAsync("ChatMessage", message);
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }


    }
}
