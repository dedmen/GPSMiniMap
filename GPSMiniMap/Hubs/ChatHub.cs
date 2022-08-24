using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GPSMiniMap.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace GPSMiniMap.Hubs
{

    public class ChatHub : Hub
    {
        private static string lastPositionMessage = "";
        private static DateTime lastPositionTime = DateTime.MinValue;

      
        private StreamTitleMonitor _titleMonitor;
        private readonly Context _dbContext;

        public ChatHub(StreamTitleMonitor titleMonitor, Context dbContext)
        {
            _titleMonitor = titleMonitor;
            _dbContext = dbContext;
        }

        struct UpdatePositionMessage
        {
            public float Latitude { get; set; }
            public float Longitude { get; set; }
            public float Heading { get; set; }
            public float Speed { get; set; }
            public float Accuracy { get; set; }
            public float Altitude { get; set; }
            public float AltitudeAccuracy { get; set; }
            public DateTime Timestamp { get; set; }
        }


        public async Task UpdatePosition(string message)
        {
            //await Clients.All.SendAsync("UpdatePosition", message); //#TODO others only
            await Clients.Others.SendAsync("UpdatePosition", message);
            lastPositionTime = DateTime.Now;
            lastPositionMessage = message;

            await _titleMonitor.TryUpdate(this);

            var parsedUpdate = JsonConvert.DeserializeObject<UpdatePositionMessage>(message);


            // https://github.com/dotnet/efcore/issues/16949
            if (!_dbContext.LocationHistory.Any(x => x.Time == parsedUpdate.Timestamp))
                await _dbContext.LocationHistory.AddAsync(new LocationRecord
                {
                    Time = parsedUpdate.Timestamp, // DateTimeOffset.FromUnixTimeSeconds(parsedUpdate.Timestamp).DateTime,
                    Accuracy = parsedUpdate.Accuracy,
                    Altitude = parsedUpdate.Altitude,
                    AltitudeAccuracy = parsedUpdate.AltitudeAccuracy,
                    Heading = parsedUpdate.Heading,
                    Latitude = parsedUpdate.Latitude,
                    Longitude = parsedUpdate.Longitude,
                    Speed = parsedUpdate.Speed
                });
          
            await _dbContext.SaveChangesAsync();

        }

        public async Task ChatMessage(string message)
        {
            Console.WriteLine($"{DateTime.Now:s} chat message from {Context.ConnectionId}: {message}");
            await Clients.All.SendAsync("ChatMessage", message);
        }

        struct HistoryPoint
        {
            [JsonProperty("lat")]
            public float Lat { get; set; }

            [JsonProperty("lng")]
            public float Lng { get; set; }
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();

            string accessToken = "";
            if (httpContext.Request.Query.TryGetValue("access_token", out var tokenPar))
            {
                accessToken = tokenPar.FirstOrDefault();
            }
            else if (httpContext.Request.Headers.Authorization.Count != 0)
            {
                accessToken = httpContext.Request.Headers.Authorization.First().Replace("Bearer ", "");
            }

            switch (accessToken)
            {
                case "webClient":
                    await Groups.AddToGroupAsync(Context.ConnectionId, "webClient");
                    break;
                case "locationService":
                    await Groups.AddToGroupAsync(Context.ConnectionId, "locationService");
                    break;
            }

            Console.WriteLine($"{DateTime.Now:s} User connected {Context.ConnectionId} {accessToken}");

            if (lastPositionTime.AddMinutes(5) > DateTime.Now)
                await Clients.Caller.SendAsync("UpdatePosition", lastPositionMessage);


            // Send them current location history
            var historyToday = _dbContext.LocationHistory.Where(x => x.Time.Date == DateTime.Now.Date)
                .OrderBy(x => x.Time).Select(x => new HistoryPoint{Lat = x.Latitude, Lng = x.Longitude}).ToArray();

            await Clients.Caller.SendAsync("UpdateHistory", JsonConvert.SerializeObject(historyToday));


            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"{DateTime.Now:s} User disconnected {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }


    }
}
