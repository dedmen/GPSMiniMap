using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
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
using Android.Content;
using GPSMiniMapSender.Services;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Distribute;
using Sentry;

namespace GPSMiniMapSender
{
    // BatteryOptimizationsIntent  https://social.msdn.microsoft.com/Forums/en-US/895f0759-e05d-4747-b72b-e16a2e8dbcf9/developing-a-location-background-service?forum=xamarinforms
    // https://github.com/shernandezp/XamarinForms.LocationService +++

    // https://github.com/jamesmontemagno/GeolocatorPlugin 
    // https://www.youtube.com/watch?v=Q_renpfnbk4

    public static class Constants
    {
        public static readonly string SERVICE_STATUS_KEY = "service_status";
        public static readonly string ACTION_STOP_SERVICE = "stop_service";
    }


    public class EndlessRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(10);
        }
    }


    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]   
    public class MainActivity : AppCompatActivity
    {
        HubConnection hubConnection;
        static System.Timers.Timer myTimer = new System.Timers.Timer();
        Intent serviceIntent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SentryXamarin.Init(options =>
            {
                options.Dsn = "http://1bc959c13bbb4690b35136292047172e@lima.dedmen.de:9001/3";
                //options.Debug = true;
                options.TracesSampleRate = 1.0;
                options.AttachStacktrace = true;
                options.AutoSessionTracking = true;
                options.Release = "1.1@37e58f0a9b897c43536b6c576db3dafe40689c0c";
            });

            AppCenter.Start("e75b51e1-c2b0-40b7-9647-3a8ed009f8e6", typeof(Distribute));
            Distribute.SetEnabledForDebuggableBuild(true);

            // SentrySdk.CaptureMessage("Hello Sentry");

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicy) =>
            {
                if (sslPolicy == SslPolicyErrors.None)
                    return true;

                if (sslPolicy == SslPolicyErrors.RemoteCertificateChainErrors &&
                    ((HttpWebRequest)sender).RequestUri.Authority.Equals("MY_API_DOMAIN"))
                    return true;

                return false;
            };



            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            //FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            //fab.Click += FabOnClick;

            



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
                    //Looper.Prepare();
                    //myTimer.Start();
                    Snackbar.Make(chatInputBox, $"Connected? State: {hubConnection.State}", Snackbar.LengthLong).Show();
                },System.Threading.Tasks.TaskScheduler.Current);


            serviceIntent = new Intent(this, typeof(AndroidLocationService));

            //if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !Android.Provider.Settings.CanDrawOverlays(this))
            //{
            //    var intent = new Intent(Android.Provider.Settings.ActionManageOverlayPermission);
            //    intent.SetFlags(ActivityFlags.NewTask);
            //    this.StartActivity(intent);
            //}

            // do once on mainthread to get permission
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(2));
            var location = Geolocation.GetLocationAsync(request, System.Threading.CancellationToken.None);
            
            // Set up the buttons

            FindViewById<AppCompatButton>(Resource.Id.buttonSwitchCamera).Click += async (x, y) =>
            {
                await SendChatMessage("switchCamera");
            };

            FindViewById<AppCompatButton>(Resource.Id.buttonEnableService).Click += (x, y) =>
            {
                Start();
            };
            FindViewById<AppCompatButton>(Resource.Id.buttonDisableService).Click += (x, y) =>
            {
                OnStopClick();
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
                SentrySdk.CaptureException(ex);
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
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(1));
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
                SentrySdk.CaptureException(ex);
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

        private bool IsServiceRunning(System.Type cls)
        {
            ActivityManager manager = (ActivityManager)GetSystemService(Context.ActivityService);
            foreach (var service in manager.GetRunningServices(int.MaxValue))
            {
                if (service.Service.ClassName.Equals(Java.Lang.Class.FromType(cls).CanonicalName))
                {
                    return true;
                }
            }
            return false;
        }


        public void OnStopClick()
        {
            if (IsServiceRunning(typeof(AndroidLocationService)))
                StopService(serviceIntent);

            //UserMessage = "Location Service has been stopped!";
            //SecureStorage.SetAsync(Constants.SERVICE_STATUS_KEY, "0");
            //StartEnabled = true;
            //StopEnabled = false;
        }

        //void ValidateStatus()
        //{
        //    var status = SecureStorage.GetAsync(Constants.SERVICE_STATUS_KEY).Result;
        //    if (status != null && status.Equals("1"))
        //    {
        //        Start();
        //    }
        //}

        void Start()
        {
            if (!IsServiceRunning(typeof(AndroidLocationService)))
            {
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    StartForegroundService(serviceIntent);
                }
                else
                {
                    StartService(serviceIntent);
                }
            }

            //UserMessage = "Location Service has been started!";
            //SecureStorage.SetAsync(Constants.SERVICE_STATUS_KEY, "1");
            //StartEnabled = false;
            //StopEnabled = true;
        }









    }
}
