using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GPSMiniMap.Database;
using GPSMiniMap.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GPSMiniMap;

public class StreamTitleMonitor
{
    private readonly TwitchAPI _twitchApi;
    private readonly IServiceProvider _serviceProvider;
    private string _lastStreamTitle = "";
    private DateTime lastUpdateTime = DateTime.MinValue;
    private static List<GeocodeRecord> markedLocations = new();

    public string CurrentTitle => _lastStreamTitle;

    public StreamTitleMonitor(TwitchAPI twitchApi, IServiceProvider serviceProvider)
    {
        _twitchApi = twitchApi;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> TryUpdate(ChatHub chatHub)
    {
        if (DateTime.Now - lastUpdateTime < TimeSpan.FromMinutes(30))
            return false; // Last update less than 30 minutes ago, skip

        // Get new stream title
        var currentStreamInfo = await _twitchApi.GetStreamInfo("jackd23");

        if (currentStreamInfo == null)
            return false; // not live

        if (currentStreamInfo.Title == _lastStreamTitle)
            return false; // same title as before

        // Title changed, update
        _lastStreamTitle = currentStreamInfo.Title;


        //_lastStreamTitle = "German truck driver -> Dorsten -> Dortmund -> Straussfurt -> ??? !explain !switchcam !location";

        markedLocations = await GetLocationsFromStreamTitle(_lastStreamTitle);

        await chatHub.Clients.Group("webClient").SendAsync("UpdateMarkedLocations", JsonConvert.SerializeObject(markedLocations));

        return true;
    }



    async Task<List<GeocodeRecord>> GetLocationsFromStreamTitle(string streamTitle)
    {
        if (!streamTitle.Contains("->"))
            return new List<GeocodeRecord>();

        // German truck driver -> Straussfurt -> Dortmund -> ??? !explain !switchcam !location
        // German truck driver -> Dorsten -> Dortmund -> Straussfurt -> ??? !explain !switchcam !location
        // German truck driver | Limburg -> Dorsten -> ??? !explain !switchcam !location

        string[] exclusionList = { "truck", "german" };

        var match = Regex.Matches(streamTitle, " ([A-zß]*) ", RegexOptions.Singleline);

        var results = match.Select(x => x.Groups[1]).Select(x => x.Value).Except(exclusionList, StringComparer.OrdinalIgnoreCase).ToArray();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService(typeof(Context)) as Context;

        var parsedLocations = (await Task.WhenAll(results.Select(async x => await dbContext.GetNameLocation(x)))).Where(x => x.Latitude != 0).ToList();

        return parsedLocations;
    }



}