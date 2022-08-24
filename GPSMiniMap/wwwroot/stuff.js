// Note: This example requires that you consent to location sharing when
// prompted by your browser. If you see the error "The Geolocation service
// failed.", it means you probably did not give permission for the browser to
// locate you.
let map, curPosMarker, automoveBlockedUntil, curAccuracyMarker;

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
        accessTokenFactory: () => "webClient"
    })
.configureLogging(signalR.LogLevel.Information)
.withAutomaticReconnect({
  nextRetryDelayInMilliseconds: retryContext => {
      return Math.random() * 10000;
  }
})
.build();

automoveBlockedUntil = 0;

// URL contains ?sendOnly, don't display a map
let isSendOnly = window.location.href.indexOf("?sendOnly") > -1;
// URL contains ?obs
let isOBS = window.location.href.indexOf("?obs") > -1;
let isOBS2 = window.location.href.indexOf("?obs2") > -1;
// URL contains ?desktop
let isDesktop = true; // isOBS || window.location.href.indexOf("?desktop") > -1;


function CenterControl(controlDiv, map) {
  // Set CSS for the control border.
  const controlUI = document.createElement("div");
  controlUI.style.backgroundColor = "#fff";
  controlUI.style.border = "2px solid #fff";
  controlUI.style.borderRadius = "3px";
  controlUI.style.boxShadow = "0 2px 6px rgba(0,0,0,.3)";
  controlUI.style.cursor = "pointer";
  controlUI.style.marginTop = "8px";
  controlUI.style.marginBottom = "22px";
  controlUI.style.textAlign = "center";
  controlUI.title = "Toggle autocenter";
  controlDiv.appendChild(controlUI);
  // Set CSS for the control interior.
  const controlText = document.createElement("div");
  controlText.style.color = "rgb(25,25,25)";
  controlText.style.fontFamily = "Roboto,Arial,sans-serif";
  controlText.style.fontSize = "16px";
  controlText.style.lineHeight = "38px";
  controlText.style.paddingLeft = "5px";
  controlText.style.paddingRight = "5px";
  controlText.innerHTML = "Toggle autocenter";
  controlUI.appendChild(controlText);
  // Setup the click event listeners: simply set the map to Chicago.
  controlUI.addEventListener("click", () => {
    if (automoveBlockedUntil == 0){
      automoveBlockedUntil = Number.MAX_SAFE_INTEGER;
      controlUI.style.backgroundColor = "#f00";
    }
    else
    {
      automoveBlockedUntil = 0;
      controlUI.style.backgroundColor = "#fff";

	  const pos = {
        lat: lastPosUpdate.latitude,
        lng: lastPosUpdate.longitude,
      };
	  
      map.setCenter(pos);
      map.setHeading(lastPosUpdate.heading);
    }
      
  });
}

function initMap() {
  if (isSendOnly) // sendOnly has no map
    return;

  let defaultZoom = 16;
  if (isOBS)
    defaultZoom = 18;

  let defaultTypeID = google.maps.MapTypeId.ROADMAP;
  if (isOBS && isOBS2)
    defaultTypeID = google.maps.MapTypeId.SATELLITE;

  if (window.location.href.indexOf("zoom=") > -1){ 
    defaultZoom = parseFloat(window.location.href.substr(window.location.href.indexOf("zoom=")+5));
  }

  map = new google.maps.Map(document.getElementById("map"), {
    center: {
      lat: 56.511018,
      lng: 3.515624,
    },

    zoom: defaultZoom,
    heading: 0,
    tilt: 47.5,
    mapId: "9f859f206c766f13",
    disableDefaultUI: isOBS,
    streetViewControl: false,
    mapTypeId: defaultTypeID,
    gestureHandling: "greedy", // grab all gestures
  });

  var marker = new google.maps.Marker({
    position: new google.maps.LatLng(56.511018, 3.515624),
    icon: {url:'https://static-cdn.jtvnw.net/jtv_user_pictures/jackd23-profile_image-86b4981fe59d9ae4-70x70.png', labelOrigin: new google.maps.Point(0,100)},
    label: 'Jack is currently offline wrrm wrrm',
    map: map
  });

  const trafficLayer = new google.maps.TrafficLayer({map: map});

  curPosMarker = new google.maps.Marker({
    position: {
      lat: 56.511018,
      lng: 3.515624,
    },
    map: map,
    icon: { // https://developers.google.com/maps/documentation/javascript/reference/marker#Symbol
      path: google.maps.SymbolPath.FORWARD_CLOSED_ARROW,
      scale: 4
    }
  });

//#TODO https://developers.google.com/maps/documentation/javascript/events#MarkerEvents on manual zoom, don't set center on pos updates for a few seconds

  map.addListener("drag", () => {
    // block autocenter for next 5 seconds
    let nextTime = new Date().valueOf() + 5000;
    if (automoveBlockedUntil < nextTime)
      automoveBlockedUntil = new Date().valueOf() + 5000;
  });

  if (isDesktop && !isOBS) {
    const centerControlDiv = document.createElement("div");
    CenterControl(centerControlDiv, map);
    map.controls[google.maps.ControlPosition.TOP_CENTER].push(centerControlDiv);
   var measureTool = new MeasureTool(map, {
      showSegmentLength: true,
      unit: MeasureTool.UnitTypeId.METRIC // or just use 'imperial'
   });
  }
	
 curAccuracyMarker = new google.maps.Circle({
  strokeColor: "#FF0000",
  strokeOpacity: 0.8,
  strokeWeight: 2,
  fillColor: "#FF0000",
  fillOpacity: 0.2,
  map,
  center: { lat: 0, lng: 0 },
  radius: 0,
 });
	
}

connection.start().then(function () {
	
  // Add location listener
  
  // Try HTML5 geolocation.
  if (isSendOnly && navigator.geolocation) {
    navigator.geolocation.watchPosition(
      (position) => {
        // https://developer.mozilla.org/en-US/docs/Web/API/GeolocationCoordinates

        connection.invoke("UpdatePosition", JSON.stringify({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          altitude: position.coords.altitude,
          accuracy: position.coords.accuracy,
          altitudeAccuracy: position.coords.altitudeAccuracy,
          heading: position.coords.heading,
          speed: position.coords.speed
        })
        ).catch(function (err) {
            return console.error(err.toString());
        });
      },
      () => {
        //handleLocationError(true, infoWindow, map.getCenter());
      },
      {
          maximumAge: 1000, // maximum age to use a cached position
          enableHighAccuracy: false // don't need high accuracy, that would make it slower
      }
    );
  }


}).catch(function (err) {
    return console.error(err.toString());
});



// Select the node that will be observed for mutations
var targetNode = document.getElementById('map');

// Options for the observer (which mutations to observe)
var config = { attributes: false, childList: true };

// Callback function to execute when mutations are observed
var callback = function(mutationsList) {
    for(var mutation of mutationsList) {
        if (mutation.type == 'childList') {
            const dismissButton = document.getElementsByClassName('dismissButton');
            //console.log(dismissButton);
            if (dismissButton.length) {
              dismissButton[0].click();
            }

            //console.log('A child node has been added or removed.');
            //console.log(mutation);
        }
    }
};

// Create an observer instance linked to the callback function
var observer = new MutationObserver(callback);

// Start observing the target node for configured mutations
observer.observe(targetNode, config);

// Later, you can stop observing
//observer.disconnect();


var lastPosUpdate = {};


connection.on("UpdatePosition", function (message) {
    const obj = JSON.parse(message);
    console.log(obj);
    
    // obj format is coords
    // https://developer.mozilla.org/en-US/docs/Web/API/GeolocationCoordinates
    
    
    const pos = {
        lat: obj.latitude,
        lng: obj.longitude,
      };
	  
	lastPosUpdate = obj;
    

    if (!automoveBlockedUntil)
    {
      map.setCenter(pos);
      map.setHeading(obj.heading);
    } else {
      let curTime = new Date().valueOf();
      if (curTime > automoveBlockedUntil)
        automoveBlockedUntil = 0;
    }
    
    curPosMarker.setPosition(pos);




    // add new lat/long to last path if we have a running history
    if (currentHistoryMarkers.length > 0)
        currentHistoryMarkers[currentHistoryMarkers.length - 1].getPath().push(new google.maps.LatLng(pos));

});

connection.on("ChatMessage", function (message) {
    if (message == "reloadweb") {
        location.reload(); 
    }
});


var currentTargetMarkers = [];

connection.on("UpdateMarkedLocations", function (message) {
    const obj = JSON.parse(message);

    //"[
    //{\"Name\":\"dorsten\",\"Longitude\":6.964260599999999,\"Latitude\":51.6559681},
    //{\"Name\":\"dortmund\",\"Longitude\":7.465298100000001,\"Latitude\":51.5135872},
    //{\"Name\":\"straussfurt\",\"Longitude\":10.983056,\"Latitude\":51.166667}
    //]"


    // Remove markers from map
    currentTargetMarkers.forEach(element => element.setMap(null));
    currentTargetMarkers = [];


    obj.forEach(element => {
        var newMarker = new google.maps.Marker({
            position: {
                lat: element.Latitude,
                lng: element.Longitude,
            },
            map: map,
            icon: { // https://developers.google.com/maps/documentation/javascript/reference/marker#Symbol
                path: google.maps.SymbolPath.BACKWARD_OPEN_ARROW,
                scale: 4
            },
            title: element.Name,
            label: {
                text: element.RealName,
                fontSize: '30px'
            }
        });

        currentTargetMarkers.push(newMarker);
    });


    if (currentHistoryMarkers.length == 0) {
        const flightPlanCoordinates = [
            { lat: 51.6559681, lng: 6.964260599999999 },
            { lat: 51.5135872, lng: 7.465298100000001 },
            { lat: 51.166667, lng: 10.983056 }
        ];
        var newMarker = new google.maps.Polyline({ // https://developers.google.com/maps/documentation/javascript/reference/polygon#Polyline
            path: flightPlanCoordinates,
            geodesic: true,
            strokeColor: "#FF0000",
            strokeOpacity: 1.0,
            strokeWeight: 2,
            map: map
        });
        newMarker.setMap(map);
        currentHistoryMarkers.push(newMarker);

    }


});

var currentHistoryMarkers = [];

connection.on("UpdateHistory", function (message) {
    const obj = JSON.parse(message);

    //"[
    //{\"lat\":6.964260599999999,\"lng\":51.6559681},
    //{\"lat\":7.465298100000001,\"lng\":51.5135872},
    //{\"lat\":10.983056,\"lng\":51.166667}
    //]"


    // Remove markers from map
    currentHistoryMarkers.forEach(element => element.setMap(null));
    currentHistoryMarkers = [];

    const flightPlanCoordinates = [

    ];

    obj.forEach(element => {
        //element.lat,
        //element.lng,
        flightPlanCoordinates.push(element);
    });

    const newMarker = new google.maps.Polyline({ // https://developers.google.com/maps/documentation/javascript/reference/polygon#Polyline
        path: flightPlanCoordinates,
        geodesic: true,
        strokeColor: "#FF0000",
        strokeOpacity: 1.0,
        strokeWeight: 2,
        map: map
    });

    currentHistoryMarkers.push(newMarker);

    // https://developers.google.com/maps/documentation/javascript/reference/visualization#HeatmapLayer
});

connection.on("AddHistory", function (message) {
    const obj = JSON.parse(message);

    //obj.Latitude,
    //obj.Longitude,

    // add new lat/long to last path

    currentHistoryMarkers[currentHistoryMarkers.length - 1].getPath().push({ lat: 6.964260599999999, lng: 51.6559681 });
});

