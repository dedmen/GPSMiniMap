using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Xamarin.Essentials;
using Android.Nfc;
using Android.Util;

namespace GPSMiniMapSender.Services
{
    [Service]
    public class AndroidLocationService : Service
    {
        CancellationTokenSource _cts;
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        private static Location _loc = null;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {

            if (intent.Action != null && intent.Action.Equals(Constants.ACTION_STOP_SERVICE))
            {
                _loc?.Shutdown();
                _cts?.Cancel();
                StopForeground(true);
                StopSelf();
                _cts = null;
                _loc = null;
                return StartCommandResult.NotSticky;
            }

            if (_loc != null)
                return StartCommandResult.NotSticky;

            _cts = new CancellationTokenSource();

            Notification notif = NotificationHelper.ReturnNotif(this);
            StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notif);

            Task.Run(() => {
                try
                {
                    _loc = new Location();
                    _loc.Run(_cts.Token).Wait();
                }
                //catch (OperationCanceledException)
                //{
                //}
                finally
                {
                    if (_cts.IsCancellationRequested)
                    {
                        //var message = new StopServiceMessage();
                        //BluetoothClass.Device.BeginInvokeOnMainThread(
                        //    () => MessagingCenter.Send(message, "ServiceStopped")
                        //);
                    }
                }
            }, _cts.Token);

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            if (_cts != null)
            {
                _cts.Token.ThrowIfCancellationRequested();
                _cts.Cancel();
            }
            base.OnDestroy();
        }
    }
}