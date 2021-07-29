﻿using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Xamarin.Essentials;
using Microsoft.AspNetCore.SignalR.Client;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;

namespace GPSMiniMapSender
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        HubConnection hubConnection;
        static System.Timers.Timer myTimer = new System.Timers.Timer();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            //FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            //fab.Click += FabOnClick;

            



            hubConnection = new HubConnectionBuilder()
            .WithUrl("URL HERE")
            .WithAutomaticReconnect()
            .Build();

            myTimer.Elapsed += (x,y) => TimerEventProcessor();
            myTimer.Interval = 1000;
            myTimer.AutoReset = false;

            var allChatBox = FindViewById<AppCompatTextView>(Resource.Id.textView1);
            
            hubConnection.On<string>("ChatMessage", (message) => {
                RunOnUiThread(() => {
                    allChatBox.Text = ($"{DateTime.Now:T} {message}\n{allChatBox.Text}"); // this is somewhat stupid, but the best prepend I could find
                });
            });

            var chatInputBox = FindViewById<AppCompatEditText>(Resource.Id.editText1);
            var buttonSend = FindViewById<AppCompatButton>(Resource.Id.button1);
            chatInputBox.EditorAction += (x, y) => {
                if (y.ActionId == Android.Views.InputMethods.ImeAction.Send)
                {
                    buttonSend.CallOnClick();
                    y.Handled = true;
                } 
            };

            chatInputBox.KeyPress += (object sender, View.KeyEventArgs e) => {
                e.Handled = false;
                if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
                {
                    buttonSend.CallOnClick();
                    e.Handled = true;
                }
            };

            buttonSend.Click += async (x, y) =>
            {
                await SendChatMessage(chatInputBox.Text);
                chatInputBox.Text = "";
            };


            hubConnection.StartAsync().ContinueWith(
                (x) => {
                    Looper.Prepare();
                    myTimer.Start();
                    Snackbar.Make(chatInputBox, $"Connected? State: {hubConnection.State}", Snackbar.LengthLong).Show();
                },System.Threading.Tasks.TaskScheduler.Current);

            // do once on mainthread to get permission
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(2));
            var location = Geolocation.GetLocationAsync(request, System.Threading.CancellationToken.None);


            // Set up the buttons

            FindViewById<AppCompatButton>(Resource.Id.buttonSwitchCamera).Click += async (x, y) =>
            {
                await SendChatMessage("switchCamera");
            };

            Func<string> GetCargoState = () =>
            {
                if (FindViewById<AppCompatRadioButton>(Resource.Id.radioButtonLoading).Checked)
                {
                    return "cargo L";
                }

                if (FindViewById<AppCompatRadioButton>(Resource.Id.radioButtonUnloading).Checked)
                {
                    return "cargo U";
                }
                return "";
            };

            // cargo progress

            FindViewById<AppCompatButton>(Resource.Id.buttonCargo0).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 0"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo10).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 10"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo20).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 20"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo30).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 30"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo40).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 40"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo50).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 50"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo60).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 60"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo70).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 70"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo80).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 80"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo90).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 90"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargo100).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} 100"); };
            FindViewById<AppCompatButton>(Resource.Id.buttonCargoToggle).Click += async (x, y) => { await SendChatMessage($"{GetCargoState()} toggle"); };
        }


        private async Task SendChatMessage(string message)
        {
            try
            {
                await hubConnection.SendAsync("ChatMessage", message);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => {
                    Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                    alert.SetTitle("Exception");
                    alert.SetMessage(ex.Message);
                    Dialog dialog = alert.Create();
                    dialog.Show();
                });
            }
        }


        // Event to run every second
        private async void TimerEventProcessor()
        {
            if (hubConnection.State != HubConnectionState.Connected)
                return;

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            /*
            accuracy: 14.404999732971191
            altitude: 46.799819758115405
            altitudeAccuracy: null
            */
            try
            {          
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(1));
                var location = await Geolocation.GetLocationAsync(request, System.Threading.CancellationToken.None);

                await hubConnection.SendAsync("UpdatePosition", $"{{" +
                    $"\"latitude\": {location.Latitude}," +
                    $"\"longitude\": {location.Longitude}," +
                    $"\"heading\": {(location.Course.HasValue ? location.Course.Value.ToString() : "null")}," +
                    $"\"speed\": {(location.Speed.HasValue ? location.Speed.Value.ToString() : "null")}," +
                    $"\"accuracy\": {(location.Accuracy.HasValue ? location.Accuracy.Value.ToString() : "null")}," +
                    $"\"altitude\": {(location.Altitude.HasValue ? location.Altitude.Value.ToString() : "null")}," +
                    $"\"altitudeAccuracy\": {(location.VerticalAccuracy.HasValue ? location.VerticalAccuracy.Value.ToString() : "null")}" +
                    $"}}");

            }
            catch (FeatureNotSupportedException fnsEx)
            {
                RunOnUiThread(() => {
                    Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                    alert.SetTitle("FeatureNotSupportedException");
                    alert.SetMessage(fnsEx.Message);
                    alert.SetPositiveButton("OK", (x,y) => { });
                    Dialog dialog = alert.Create();
                    dialog.Show();
                });
            }
            catch (FeatureNotEnabledException fneEx)
            {
                RunOnUiThread(() => {
                    Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                    alert.SetTitle("FeatureNotSupportedException");
                    alert.SetMessage(fneEx.Message);
                    alert.SetPositiveButton("OK", (x, y) => { });
                    Dialog dialog = alert.Create();
                    dialog.Show();
                });
            }
            catch (PermissionException pEx)
            {
                RunOnUiThread(() => {
                    Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                    alert.SetTitle("FeatureNotSupportedException");
                    alert.SetMessage(pEx.Message);
                    alert.SetPositiveButton("OK", (x, y) => { });
                    Dialog dialog = alert.Create();
                    dialog.Show();
                });
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => {
                    Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                    alert.SetTitle("Exception");
                    alert.SetMessage(ex.Message);
                    Dialog dialog = alert.Create();
                    dialog.Show();
                });
            }
            myTimer.Start();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}
