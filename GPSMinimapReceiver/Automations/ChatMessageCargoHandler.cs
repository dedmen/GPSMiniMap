using DocumentDatabase;
using GPSMinimapReceiver.Messaging;
using Microsoft.AspNetCore.SignalR.Protocol;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSMinimapReceiver.Automations
{
    public class ChatMessageCargoHandler
    {
        public ChatMessageCargoHandler()
        {
            MessagingCenter.Subscribe<Messaging.Events.GPSChatMessage>(chatMessage =>
            {
                if (!chatMessage.Message.StartsWith("cargo")) return; // Not a cargo message

                // Is a cargo message, lets process it

                string[] argv = chatMessage.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                OnMessage(argv);
            });
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

            SourceSettings truckLoading = obs.GetSourceSettings("Truck cargo");
            truckLoading.Settings["file"] = $"d:/rec/stream/truck/truck_{(loading ? "l" : "u")}pbar_{percentage}.png";
            obs.SetSourceSettings("Truck cargo", truckLoading.Settings);

            truckLoading = obs.GetSourceSettings("Truck cargo percentage");
            truckLoading.Settings["text"] = $"{(loading ? "L" : "Unl")}oading... ({percentage} %)";
            obs.SetSourceSettings("Truck cargo percentage", truckLoading.Settings);

            truckLoading = null;
        }
    }
}
