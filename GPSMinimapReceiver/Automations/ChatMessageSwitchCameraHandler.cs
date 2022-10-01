using DocumentDatabase;
using GPSMinimapReceiver.Messaging;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;

namespace GPSMinimapReceiver.Automations
{
    public class ChatMessageSwitchCameraHandler
    {
        public ChatMessageSwitchCameraHandler()
        {
            MessagingCenter.GetEvent<Messaging.Events.GPSChatMessage>()
                .Where(x => x.Message.StartsWith("switchCamera")) // Filter
                .SubscribeWithCatch(chatMessage =>
                    {
                        string[] argv = chatMessage.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        OnMessage(argv);
                    }
                );
        }

        private void OnMessage(string[] arguments)
        {
            var obs = ServiceManager.GetService<OBSWebsocket>();
            var TTS = ServiceManager.GetService<SpeechSynthesizer>();
            var currentScene = obs.GetCurrentProgramScene();

            if (arguments.Length > 1)
            {

                // Code for enabling/disabling inputs


                //// Get all dshow_input ID's, and disable them all
                //var cameras = obs.GetSceneItemList(currentScene.Name)
                //    .Where(x => x.SourceKind.Equals("dshow_input") && !x.SourceName.Equals("Dashcam"))
                //    .Select(x => x.ItemId);
                //
                //foreach (var cameraId in cameras)
                //    obs.SetSceneItemEnabled(currentScene.Name, cameraId, false);
                //
                //// Find the desired camera (name case sensitive), and enable it.
                //
                //currentScene.GetSceneItemByName(arguments[1])?.SetIsEnabled(true);

                // Code for switching scenes

                obs.SetCurrentProgramScene(arguments[1]); // Scene name == camera name

                TTS.SpeakAsync("Camera switched to " + arguments[1]);
            }
            else
            {
                // Code for enabling/disabling inputs

                //var laptopCam = currentScene.GetSceneItemByName("Laptop camera");
                //var webcam = currentScene.GetSceneItemByName("Webcam");
                //
                //webcam.SetIsEnabled(laptopCam.GetIsEnabled());
                //laptopCam.SetIsEnabled(!webcam.GetIsEnabled());
                //
                //TTS.SpeakAsync("Camera switched to " + (laptopCam.GetIsEnabled() ? "laptop" : "webcam"));


                // Code for switching scenes

                string newSceneName;
                switch (currentScene)
                {
                    case "Szene":
                        newSceneName = "Szene 1";
                        break;
                    case "Scene 2":
                        newSceneName = "Szene";
                        break;
                    default:
                        newSceneName = "Szene";
                        break;
                }

                obs.SetCurrentProgramScene(newSceneName);

                TTS.SpeakAsync($"Camera switched to {newSceneName}");


            }
        }
    }
}
