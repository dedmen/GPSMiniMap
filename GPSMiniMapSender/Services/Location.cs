using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Google.Android.Material.Snackbar;
using Microsoft.AspNetCore.SignalR.Client;
using Xamarin.Essentials;
using Android.Locations;
using Java.Security;
using Java.Interop;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System.Runtime.Remoting.Contexts;
using Context = Android.Content.Context;

namespace GPSMiniMapSender.Services
{
    public class Location
    {
        HubConnection hubConnection;
        readonly bool stopping = false;
        private Position _lastPosition;
        public Location()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl("URL HERE", (opts) =>
                {
                    // Ignore https connection errors
                    opts.HttpMessageHandlerFactory = (message) =>
                    {
                        if (message is HttpClientHandler clientHandler)
                            // always verify the SSL certificate
                            clientHandler.ServerCertificateCustomValidationCallback +=
                                (sender, certificate, chain, sslPolicyErrors) => { return true; };
                        return message;
                    };
                })
                .WithAutomaticReconnect(new EndlessRetryPolicy())
                .Build();

        }

        private static Context context = global::Android.App.Application.Context;
        async Task SendUpdateAsync(Position location)
        {
            if (hubConnection.State !=HubConnectionState.Connected && hubConnection.State != HubConnectionState.Connected)
                await hubConnection.StartAsync();

            await hubConnection.SendAsync("UpdatePosition", $"{{" +
                                                      $"\"latitude\": {location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                      $"\"longitude\": {location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                      $"\"heading\": {location.Heading.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                      $"\"speed\": {location.Speed.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                      $"\"accuracy\": {location.Accuracy.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                      $"\"altitude\": {location.Altitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                      $"\"altitudeAccuracy\": {location.AltitudeAccuracy.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                                                      $"}}");

            var builder = NotificationHelper.GetBuilderCached();
            if (builder != null)
            {
                builder.SetContentText($"Last update {DateTime.Now:T}");

                var notifManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                if (notifManager != null)
                {
                    notifManager.Notify(AndroidLocationService.SERVICE_RUNNING_NOTIFICATION_ID, builder.Build());
                }
            }

        }


        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var locator = CrossGeolocator.Current;
                    await hubConnection.StartAsync();
                    await hubConnection.SendAsync("ChatMessage", "LocationServiceHello");

                    if (!CrossGeolocator.Current.IsListening)
                        await locator.StartListeningAsync(TimeSpan.FromSeconds(5), 10, true, new ListenerSettings {AllowBackgroundUpdates = true});
               
                locator.PositionChanged += (sender, args) =>
                {
                    if (_lastPosition != null &&
                        _lastPosition.CalculateDistance(args.Position, GeolocatorUtils.DistanceUnits.Kilometers) < 0.01)
                        return; // too close to last
                    var location = args.Position;
                    _lastPosition = location;
                    SendUpdateAsync(location);
                    };
                    locator.PositionError += (sender, args) =>
                    {
                        var builder = NotificationHelper.GetBuilderCached();
                        if (builder != null)
                        {
                            builder.SetContentText($"ERROR1 {args.Error}");

                            if (context.GetSystemService(Context.NotificationService) is NotificationManager notifManager)
                            {
                                notifManager.Notify(AndroidLocationService.SERVICE_RUNNING_NOTIFICATION_ID, builder.Build());
                            }
                        }

                        hubConnection.SendAsync("ChatMessage", args.Error.ToString());
                    };

                /*
                while (!stopping)
                {

                    try
                    {
                        token.ThrowIfCancellationRequested();

                        await Task.Delay(2000, token);

                        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                        var location = await Geolocation.GetLocationAsync(request, token);
                        if (location != null)
                        {
                            await hubConnection.SendAsync("UpdatePosition", $"{{" +
                                                                            $"\"latitude\": {location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                                            $"\"longitude\": {location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                                                                            $"\"heading\": {(location.Course.HasValue ? location.Course.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null")}," +
                                                                            $"\"speed\": {(location.Speed.HasValue ? location.Speed.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null")}," +
                                                                            $"\"accuracy\": {(location.Accuracy.HasValue ? location.Accuracy.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null")}," +
                                                                            $"\"altitude\": {(location.Altitude.HasValue ? location.Altitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null")}," +
                                                                            $"\"altitudeAccuracy\": {(location.VerticalAccuracy.HasValue ? location.VerticalAccuracy.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null")}" +
                                                                            $"}}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await hubConnection.SendAsync("ChatMessage", ex.Message);
                    }
                }
                    */

                    do {
                        await SendUpdateAsync(await locator.GetLastKnownLocationAsync());
                        await Task.Delay(30000, token); // Send update every 30 seconds manually, in case the automatic updating doesn't send anything
                    }
                    while (!token.IsCancellationRequested);

                if (CrossGeolocator.Current.IsListening)
                    await locator.StopListeningAsync();
                    if (hubConnection.State == HubConnectionState.Connected)
                        await hubConnection.StopAsync();

                }
                catch (Exception ex)
                {

                    var builder = NotificationHelper.GetBuilderCached();
                    if (builder != null)
                    {
                        builder.SetContentText($"ERROR2 {ex.Message}");

                        var notifManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                        if (notifManager != null)
                        {
                            notifManager.Notify(AndroidLocationService.SERVICE_RUNNING_NOTIFICATION_ID, builder.Build());
                        }
                    }

                    await hubConnection.SendAsync("ChatMessage", ex.Message);
                }


                return;
            }, token);
        }

        public void Shutdown()
        {
            if (CrossGeolocator.Current.IsListening)
                CrossGeolocator.Current.StopListeningAsync();
            if (hubConnection.State == HubConnectionState.Connected)
                hubConnection.StopAsync();
        }
    }
}