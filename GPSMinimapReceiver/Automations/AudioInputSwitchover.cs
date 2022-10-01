using DocumentDatabase;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSMinimapReceiver.Automations
{
    public class AudioInputSwitchover
    {

        /// <summary>
        /// Automatic audio failover, if primary input goes silent, turn on secondary. If primary comes back, turn off secondary
        /// </summary>
        /// <param name="volumeMonitor"></param>
        /// <param name="primaryInputName"></param>
        /// <param name="secondaryInputName"></param>
        public AudioInputSwitchover(string primaryInputName, string secondaryInputName)
        {
            var obs = ServiceManager.GetService<OBSWebsocket>();
            var volumeMonitor = ServiceManager.GetService<InputVolumeMonitor>();

            volumeMonitor.OnInputAppeared += name =>
            {
                Console.WriteLine($"Input Appeared {name}");
                if (name == "OBS Out")
                {
                    // its back, lets disable desktop audio again
                    obs.SetInputMute("Desktop-Audio", true);
                }
            };

            volumeMonitor.OnInputDisappeared += name =>
            {
                Console.WriteLine($"Input Disappeared {name}");
                if (name == "OBS Out")
                {
                    // Oh no, obs out disappeared, lets fallover to backup
                    obs.SetInputMute("Desktop-Audio", false);
                }
            };
        }
    }
}
