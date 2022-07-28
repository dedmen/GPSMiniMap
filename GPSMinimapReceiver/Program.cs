using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Timers;
using DocumentDatabase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GPSMinimapReceiver.Messaging;

namespace GPSMinimapReceiver
{
    class Program
    {
        /*
            SetupServices() sets up services/classes that are always instanced and alive
            Setup() is called before startup, it can use the services and connect events on them
            Startup() initiates the connection to gps server and obs websocket, after startup the Services will do their work, and the events configured in Setup will fire appropriately
         */

        public static void SetupServices()
        {

            ServiceManager.AddInstance(new SpeechSynthesizer());

            {
                // Set up gps chat hub connection

                var hubConnection = new HubConnectionBuilder()
                    .WithUrl(Secret.HubLink)
                    .WithAutomaticReconnect()
                    .Build();

                ServiceManager.AddInstance(hubConnection);

                // Let it fire messages 

                hubConnection.On<string>("ChatMessage", msg => MessagingCenter.Publish(new Messaging.Events.GPSChatMessage() {Message = msg}));
                //hubConnection.On<string>("UpdatePosition", msg => MessagingCenter.Publish(new Messaging.Events.GPSChatMessage() {Message = msg}));
            }

            {
                // Set up OBS websocket connection
                OBSWebsocket obs = new OBSWebsocket();

                ServiceManager.AddInstance<OBSWebsocket>(obs);
            }

            ServiceManager.AddInstance(new InputVolumeMonitor() { SilenceThreshold = TimeSpan.FromSeconds(1) }); // We will need a monitor for others, this will automatically register eventhandlers on OBS Websocket
        }


        static void Setup()
        {
            // Register a automatic audio input switchover, it internally handles all the events needed
            ServiceManager.AddInstance(new Automations.AudioInputSwitchover("OBS Out", "Desktop Audio")); // If primary goes silent, we will switch over to secondary, and back if it reappears

            // Register chat handler for cargo/switchCamera commands, everything is handled internally inside it
            ServiceManager.AddInstance(new Automations.ChatMessageCargoHandler());
            ServiceManager.AddInstance(new Automations.ChatMessageSwitchCameraHandler());

            var obs = ServiceManager.GetService<OBSWebsocket>();

            // Someone sent a chat message to gps server, and we received it
            MessagingCenter.Subscribe<Messaging.Events.GPSChatMessage>(chatMessage =>
            {
                Console.WriteLine($"Chat Message: {chatMessage.Message}");

                if (chatMessage.Message == "test")
                {
                    SourceSettings settings = obs.GetSourceSettings("Truck cargo percentage");
                    Console.WriteLine($"{settings.SourceName}: {settings.SourceKind}, {settings.Settings}");
                }

            });

            obs.Connected += (sender, type) => { Console.WriteLine($"OBS websocket connected!"); };
            //obs.StreamingStateChanged += (sender, type) =>
            //{
            //    Console.WriteLine($"OBS Streaming status changed: {type}");
            //    //if (type == OutputState.Reconnecting) // Doesn't work, this event never fires
            //    //    ServiceManager.GetService<SpeechSynthesizer>().Speak("OBS is Reconnecting");
            //};
            //obs.StreamStatus += (sender, status) => { Console.WriteLine($"OBS Streaming status: { JsonConvert.SerializeObject(status)}"); };


            // A timer that checks every 500ms if OBS is reconnecting, to send us a TTS message

            var timer = new Timer();
            timer.AutoReset = true;
            timer.Interval = 500;
            bool wasReconnecting = false;
            timer.Elapsed += (x, y) =>
            {
                var status = obs.GetStreamStatus();
                if (status.IsReconnecting != wasReconnecting)
                {
                    wasReconnecting = status.IsReconnecting;
                    if (status.IsReconnecting)
                        ServiceManager.GetService<SpeechSynthesizer>().Speak("OBS is Reconnecting");
                    if (!status.IsReconnecting && status.IsActive)
                        ServiceManager.GetService<SpeechSynthesizer>().Speak("OBS is back online");
                }
            };
            timer.Start();

        }


        static void Startup()
        {
            var hubConnection = ServiceManager.GetService<HubConnection>();

            // connect hub
            hubConnection.StartAsync().ContinueWith(x => { Console.WriteLine($"Connected? State: {hubConnection.State}"); });

            var obs = ServiceManager.GetService<OBSWebsocket>();
            // connect obs 
            try
            {
                obs.Connect("ws://127.0.0.1:4455", "perji69");
            }
            catch (AuthFailureException)
            {
                Console.WriteLine($"OBS Authentication failed.");
            }
            catch (ErrorResponseException ex)
            {
                Console.WriteLine($"OBS Connect failed : {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            Setup();
            Startup();

            bool wantExit = false;
            Console.CancelKeyPress += delegate
            {
                wantExit = true;
            };

            while (!wantExit)
            {
                System.Threading.Thread.Sleep(1);
            };
        }
    }
}