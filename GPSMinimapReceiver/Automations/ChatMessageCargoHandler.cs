using DocumentDatabase;
using GPSMinimapReceiver.Messaging;
using Microsoft.AspNetCore.SignalR.Protocol;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GPSMinimapReceiver.Automations
{
    public class ChatMessageCargoHandler
    {
        public ChatMessageCargoHandler()
        {
            MessagingCenter.GetEvent<Messaging.Events.GPSChatMessage>()
                .Where(x => x.Message.StartsWith("cargo")) // Filter
                .SubscribeWithCatch(chatMessage => 
                {
                    // Is a cargo message, lets process it

                    string[] argv = chatMessage.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    OnMessage(argv);
                }
            );
        }

        private void OnMessage(string[] arguments)
        {
            if (arguments.Length < 3)
            {
                Console.WriteLine("ERR 'cargo' argv is less than 3 fields!");
                return;
            }

            bool loading = arguments[1].ToLowerInvariant().Equals("l");
            if (!loading && !arguments[1].ToLowerInvariant().Equals("u"))
            {
                Console.WriteLine("ERR 'cargo' loading/unloading field is neither 'U' nor 'L'");
                return;
            }

            if (!int.TryParse(arguments[2], out var percentage))
            {
                Console.WriteLine("ERR Can't parse loading percentage number '{0}' as integer", arguments[2]);
                return;
            }

            var obs = ServiceManager.GetService<OBSWebsocket>();

            // Update OBS source


            InputSettings truckLoading = obs.GetInputSettings("Truck cargo");
            truckLoading.Settings["file"] = $"d:/rec/stream/truck/truck_{(loading ? "l" : "u")}pbar_{percentage}.png";
            obs.SetInputSettings("Truck cargo", truckLoading.Settings);

            truckLoading = obs.GetInputSettings("Truck cargo percentage");
            truckLoading.Settings["text"] = $"{(loading ? "L" : "Unl")}oading... ({percentage} %)";
            obs.SetInputSettings("Truck cargo percentage", truckLoading.Settings);

            truckLoading = null;
        }
    }
}
