using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentDatabase;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;

namespace GPSMinimapReceiver
{


    /// <summary>
    /// Monitors a audio input, and alerts if it has gone silent for longer than threshold
    /// </summary>
    public class InputVolumeMonitor : IDisposable
    {
        public TimeSpan SilenceThreshold { get; set; } = TimeSpan.FromSeconds(1);

        private class ChannelState
        {
            public DateTime LastActivity { get; set; }
            public bool IsActive { get; set; }
        }

        private ConcurrentDictionary<string, ChannelState> Channels = new();

        public delegate void InputStateCallback(string inputName);

        public event InputStateCallback OnInputAppeared;
        public event InputStateCallback OnInputDisappeared;

        public InputVolumeMonitor()
        {
            ServiceManager.GetService<OBSWebsocket>().InputVolumeMeters += OnUpdate;
        }

        public void OnUpdate(object sender, InputVolumeMetersEventArgs args)
        {
            foreach (var inputVolumeMeter in args.inputs)
            {
                bool isActive = inputVolumeMeter.InputLevels.Any(x => x.PeakRaw > 0);

                if (Channels.TryGetValue(inputVolumeMeter.InputName, out var state))
                {
                    // Update existing channel
                    if (isActive)
                    {
                        state.LastActivity = DateTime.Now;
                        if (state.IsActive) continue;
                        // Reappeared after being gone for a while
                        state.IsActive = true;
                        OnInputAppeared?.Invoke(inputVolumeMeter.InputName);
                    }
                    else if (state.IsActive)
                    {
                        // Inactive, long enough for threshold?
                        if (DateTime.Now - state.LastActivity > SilenceThreshold)
                        {
                            state.IsActive = false;
                            OnInputDisappeared?.Invoke(inputVolumeMeter.InputName);
                        }
                    }
                } 
                else
                {
                    // New channel appeared
                    Channels.TryAdd(inputVolumeMeter.InputName, new ChannelState {IsActive = isActive, LastActivity = DateTime.Now});

                    if (isActive)
                        OnInputAppeared?.Invoke(inputVolumeMeter.InputName);

                }
            }
        }

        public void Dispose()
        {
            var obs = ServiceManager.GetService<OBSWebsocket>();
            if (obs != null) obs.InputVolumeMeters -= OnUpdate;
        }
    }
}
