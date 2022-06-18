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

namespace GPSMiniMapSender.Services
{
    public class Location
    {
        HubConnection hubConnection;
        readonly bool stopping = false;
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

        public async Task Run(CancellationToken token)
        {
            await Task.Run(async () => {


                await hubConnection.StartAsync();
                
                while (!stopping)
                {

                    try
                    {
                        token.ThrowIfCancellationRequested();

                        await Task.Delay(2000);

                        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(1));
                        var location = await Geolocation.GetLocationAsync(request);
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
                return;
            }, token);
        }
    }
}