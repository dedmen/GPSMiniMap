using Microsoft.AspNetCore.SignalR.Client;
using System;

namespace GPSMinimapReceiver
{
    class Program
    {


        

        static void Main(string[] args)
        {
            HubConnection hubConnection;
            hubConnection = new HubConnectionBuilder()
               .WithUrl("URL HERE")
               .WithAutomaticReconnect()
               .Build();

            hubConnection.On<string>("ChatMessage", (message) => {
                Console.WriteLine($"CHAT {DateTime.Now:T} {message}");
            });

            hubConnection.On<string>("UpdatePosition", (message) => {
                Console.WriteLine($"POS {DateTime.Now:T} {message}");
            });


            hubConnection.StartAsync().ContinueWith(
                (x) => {
                    Console.WriteLine($"Connected? State: {hubConnection.State}");
                });


            bool wantExit = false;
            Console.CancelKeyPress += delegate {
                wantExit = true;
            };

            while (!wantExit) { };


        }
    }
}
