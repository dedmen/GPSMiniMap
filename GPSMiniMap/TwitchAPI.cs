using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.PubSub;
using TwitchLib.Communication.Interfaces;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Interfaces;
using TwitchLib.Api.Interfaces;
using System.Linq.Expressions;

namespace GPSMiniMap
{
    public class TwitchAPI
    {
        private HttpClient httpClient;
        private readonly TwitchLib.Api.TwitchAPI twitchApi;
        //private readonly TwitchPubSub twitchPubSub;

        public TwitchAPI(IConfiguration config)
        {
            twitchApi = new TwitchLib.Api.TwitchAPI();

            twitchApi.Settings.ClientId = config.GetValue<string>("TwitchClientId");
            twitchApi.Settings.Secret = config.GetValue<string>("TwitchClientSecret");
            //twitchApi.Settings.

            //twitchPubSub = new TwitchLib.PubSub.TwitchPubSub();
            //twitchPubSub.OnPubSubServiceConnected += (sender, args) => twitchPubSub.SendTopics();
            //twitchPubSub.OnListenResponse += (sender, e) =>
            //{
            //    if (!e.Successful)
            //        throw new Exception($"Failed to listen! Response: {e.Response}");
            //} ;
            //twitchPubSub.OnStreamUp += onStreamUp;
            //twitchPubSub.OnStreamDown += onStreamDown;
            //twitchPubSub.ListenToVideoPlayback("dedmen");
            //twitchPubSub.Connect();
        }

        //private void onStreamUp(object sender, OnStreamUpArgs e)
        //{
        //    Console.WriteLine($"Stream just went up! Play delay: {e.PlayDelay}, server time: {e.ServerTime}");
        //}
        //
        //private void onStreamDown(object sender, OnStreamDownArgs e)
        //{
        //    Console.WriteLine($"Stream just went down! Server time: {e.ServerTime}");
        //}


        public async Task<Stream> GetStreamInfo(string channelName)
        {
            var y = await twitchApi.Helix.Streams.GetStreamsAsync(userLogins: new List<string> { channelName });
            return y.Streams.FirstOrDefault();


            // Can get offline title here

            //int broadcasterId = channelName switch
            //{
            //    "jackd23" => 38078578,
            //    "dedmen" => 22404023,
            //    _ => 0
            //};
            //
            //if (broadcasterId == 0) return null;

            //var x = await twitchApi.Helix.Channels.GetChannelInformationAsync($"{broadcasterId}");
            //return (x.Data.FirstOrDefault(), y.Streams.FirstOrDefault());
        }
    }
}
