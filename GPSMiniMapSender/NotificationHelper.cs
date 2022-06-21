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
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using GPSMiniMapSender.Services;

namespace GPSMiniMapSender
{
    internal class NotificationHelper //: INotification
    {
        private static string foregroundChannelId = "9001";
        private static Context context = global::Android.App.Application.Context;


        public static Notification ReturnNotif(AndroidLocationService androidLocationService)
        {
            //var intent = new Intent(context, typeof(MainActivity));
            //intent.AddFlags(ActivityFlags.SingleTop);
            //intent.PutExtra("Title", "Message");
            //
            //var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent);

            var stopServiceIntent = new Intent(androidLocationService, androidLocationService.GetType());
            stopServiceIntent.SetAction(Constants.ACTION_STOP_SERVICE);
            var stopServicePendingIntent = PendingIntent.GetService(androidLocationService, 0, stopServiceIntent, 0);


            var notifBuilder = new Notification.Builder(context, foregroundChannelId)
                .SetContentTitle("GPSMinimap")
                .SetContentText("GPS Minimap is running!")
                .SetSmallIcon(Android.Resource.Drawable.IcMenuMyLocation)
                .AddAction(BuildStopServiceAction(androidLocationService))
                .SetOngoing(true)
                .SetContentIntent(stopServicePendingIntent);

            if (global::Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel notificationChannel = new NotificationChannel(foregroundChannelId, "Title", NotificationImportance.High);
                notificationChannel.Importance = NotificationImportance.High;
                notificationChannel.EnableLights(true);
                notificationChannel.EnableVibration(true);
                notificationChannel.SetShowBadge(true);
                notificationChannel.SetVibrationPattern(new long[] { 100, 200, 300 });

                var notifManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                if (notifManager != null)
                {
                    notifBuilder.SetChannelId(foregroundChannelId);
                    notifManager.CreateNotificationChannel(notificationChannel);
                }
            }

            return notifBuilder.Build();
        }

        /// <summary>
        /// Builds the Notification.Action that will allow the user to stop the service via the
        /// notification in the status bar
        /// </summary>
        /// <returns>The stop service action.</returns>
        static Notification.Action BuildStopServiceAction(AndroidLocationService androidLocationService)
        {
            var stopServiceIntent = new Intent(androidLocationService, androidLocationService.GetType());
            stopServiceIntent.SetAction(Constants.ACTION_STOP_SERVICE);
            var stopServicePendingIntent = PendingIntent.GetService(androidLocationService, 0, stopServiceIntent, 0);

            var builder = new Notification.Action.Builder(0, "Stop", stopServicePendingIntent);
            return builder.Build();

        }


    }
}