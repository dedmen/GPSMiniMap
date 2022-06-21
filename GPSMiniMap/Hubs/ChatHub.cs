using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace GPSMiniMap.Hubs
{
    public class ChatHub : Hub
    {
        private static string lastPositionMessage = "";
        private static DateTime lastPositionTime = DateTime.MinValue;

        public async Task UpdatePosition(string message)
        {
            //await Clients.All.SendAsync("UpdatePosition", message); //#TODO others only
            await Clients.Others.SendAsync("UpdatePosition", message);
            lastPositionTime = DateTime.Now;
            lastPositionMessage = message;
        }

        public async Task ChatMessage(string message)
        {
            await Clients.All.SendAsync("ChatMessage", message);
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"{DateTime.Now:s} User connected {Context.ConnectionId}");

            if (lastPositionTime.AddMinutes(5) > DateTime.Now)
                Clients.Caller.SendAsync("UpdatePosition", lastPositionMessage);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"{DateTime.Now:s} User disconnected {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }


    }
}
