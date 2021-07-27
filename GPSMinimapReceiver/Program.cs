using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
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

    

            hubConnection.On<string>("UpdatePosition", (message) => {
                Console.WriteLine($"POS {DateTime.Now:T} {message}");
            });


            hubConnection.StartAsync().ContinueWith(
                (x) => {
                    Console.WriteLine($"Connected? State: {hubConnection.State}");
                });


            OBSWebsocket obs = new OBSWebsocket();

            obs.Connected += (sender, type) => { Console.WriteLine($"OBS websocket connected!"); };
            obs.StreamingStateChanged += (sender, type) => { Console.WriteLine($"OBS Streaming status changed: {type}"); };
            obs.StreamStatus += (sender, status) => { Console.WriteLine($"OBS Streaming status: { JsonConvert.SerializeObject(status)}"); };


            // connect to OBS

            try
            {
                obs.Connect("ws://127.0.0.1:4444", "");
            }
            catch (AuthFailureException)
            {
                Console.WriteLine($"OBS Authentication failed.");
            }
            catch (ErrorResponseException ex)
            {
                Console.WriteLine($"OBS Connect failed : {ex.Message}");
            }



            hubConnection.On<string>("ChatMessage", (message) => {
                Console.WriteLine($"CHAT {DateTime.Now:T} {message}");

                if (message == "switchCamera")
                {
                    // Hide Browser 2
                    var oldProps = obs.GetSceneItemProperties("Browser 2");
                    oldProps.Visible = false;
                    obs.SetSceneItemProperties(oldProps);

                    // Show "Fensteraufnahme"
                    oldProps = obs.GetSceneItemProperties("Fensteraufnahme");
                    oldProps.Visible = true;
                    obs.SetSceneItemProperties(oldProps);
                }

            });



            bool wantExit = false;
            Console.CancelKeyPress += delegate {
                wantExit = true;
            };

            while (!wantExit) { };


        }
    }
}
