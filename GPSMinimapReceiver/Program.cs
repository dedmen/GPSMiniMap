using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;

namespace GPSMinimapReceiver
{
    class Program
    {
        static SpeechSynthesizer _TTS = new SpeechSynthesizer();

        static void Main(string[] args)
        {
            HubConnection hubConnection;
            hubConnection = new HubConnectionBuilder()
               .WithUrl(Secret.HubLink)
               .WithAutomaticReconnect()
               .Build();



            hubConnection.On<string>("UpdatePosition", (message) =>
            {
                //Console.WriteLine($"POS {DateTime.Now:T} {message}");
            });


            hubConnection.StartAsync().ContinueWith(
                (x) =>
                {
                    Console.WriteLine($"Connected? State: {hubConnection.State}");
                });


            OBSWebsocket obs = new OBSWebsocket();

            obs.Connected += (sender, type) => { Console.WriteLine($"OBS websocket connected!"); };
            obs.StreamingStateChanged += (sender, type) => { Console.WriteLine($"OBS Streaming status changed: {type}"); };
            //obs.StreamStatus += (sender, status) => { Console.WriteLine($"OBS Streaming status: { JsonConvert.SerializeObject(status)}"); };


            // connect to OBS

            try
            {
                obs.Connect("ws://127.0.0.1:4444", "perji69");
            }
            catch (AuthFailureException)
            {
                Console.WriteLine($"OBS Authentication failed.");
            }
            catch (ErrorResponseException ex)
            {
                Console.WriteLine($"OBS Connect failed : {ex.Message}");
            }



            hubConnection.On<string>("ChatMessage", (message) =>
            {
            Console.WriteLine($"CHAT {DateTime.Now:T} {message}");

            string[] argv = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (argv.Length <= 0) return;

                switch (argv[0])
                {
                    case "test":
                        SourceSettings settings = obs.GetSourceSettings("Truck cargo percentage");
                        Console.WriteLine("{0}: {1}, {2}, {3}",
                            settings.SourceName,
                            settings.SourceKind,
                            settings.SourceType,
                            settings.Settings);
                        break;
                    case "switchCamera":
                        if (argv.Length > 1)
                        {
                            List<string> cameras = new List<string>();
                            int visibleIndex = -1;

                            foreach (SceneItemDetails detail in obs.GetSceneItemList(obs.GetCurrentScene().Name))
                            {
                                if (!detail.SourceKind.Equals("dshow_input"))
                                    continue;

                                if (!detail.SourceName.Equals("Dashcam"))
                                {
                                    cameras.Add(detail.SourceName);

                                    if (visibleIndex < 0 && obs.GetSceneItemProperties(detail.SourceName).Visible)
                                        visibleIndex = cameras.Count - 1;
                                }
                            }

                            cameras.ForEach(x =>
                            {
                                SceneItemProperties p = obs.GetSceneItemProperties(x);
                                p.Visible = false;
                                obs.SetSceneItemProperties(p);
                            });

                            SceneItemProperties props = null;

                            props = obs.GetSceneItemProperties(argv[1]);
                            props.Visible = true;
                            obs.SetSceneItemProperties(props);

                            props = null;
                            cameras.Clear();
                            cameras = null;

                            _TTS.SpeakAsync("Camera switched to " + argv[1]);
                        }
                        else
                        {
                            SceneItemProperties laptop = obs.GetSceneItemProperties("Laptop camera");
                            SceneItemProperties webcam = obs.GetSceneItemProperties("Webcam");
                            laptop.Visible = !(webcam.Visible = laptop.Visible);

                            obs.SetSceneItemProperties(laptop);
                            obs.SetSceneItemProperties(webcam);

                            _TTS.SpeakAsync("Camera switched to " + (laptop.Visible ? "laptop" : "webcam"));
                        }

                        break;
                    case "cargo":
                        if (argv.Length < 3)
                        {
                            Console.WriteLine("ERR 'cargo' argv is less than 3 fields!");
                            break;
                        }

                        bool loading = argv[1].ToLowerInvariant().Equals("l");
                        if (!loading && !argv[1].ToLowerInvariant().Equals("u"))
                        {
                            Console.WriteLine("ERR 'cargo' loading/unloading field is neither 'U' nor 'L'");
                            break;
                        }

                        int percentage;

                        if (!int.TryParse(argv[2], out percentage))
                        {
                            Console.WriteLine("ERR Can't parse loading percentage number '{0}' as integer", argv[2]);
                            break;
                        }

                        SourceSettings truckLoading = obs.GetSourceSettings("Truck cargo");
                        truckLoading.Settings["file"] = String.Format("d:/rec/stream/truck/truck_{0}pbar_{1}.png", (loading ? "l" : "u"), percentage);
                        obs.SetSourceSettings("Truck cargo", truckLoading.Settings);

                        truckLoading = obs.GetSourceSettings("Truck cargo percentage");
                        truckLoading.Settings["text"] = String.Format("{0}oading... ({1} %)", (loading ? "L" : "Unl"), percentage);
                        obs.SetSourceSettings("Truck cargo percentage", truckLoading.Settings);

                        truckLoading = null;

                        break;
                }

                argv = null;
            });



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