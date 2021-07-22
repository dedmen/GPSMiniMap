using System;
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
            myTimer.AutoReset = true;

            var allChatBox = FindViewById<AppCompatTextView>(Resource.Id.textView1);
            
            hubConnection.On<string>("ChatMessage", (message) => {
                RunOnUiThread(() => {
                    allChatBox.Append($"{DateTime.Now:t} {message}\n");
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
                try
                {
                    await hubConnection.SendAsync("ChatMessage", chatInputBox.Text);
                    chatInputBox.Text = "";
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
                //RunOnUiThread(() => {
                //    Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                //    alert.SetTitle("Exception");
                //    alert.SetMessage(ex.Message);
                //    Dialog dialog = alert.Create();
                //    dialog.Show();
                //});
            }

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
