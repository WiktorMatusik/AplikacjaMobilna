using System.Globalization;
using System.Text.Json;
using AplikacjaMobilna.PageModels;

namespace AplikacjaMobilna.Pages;

public partial class WalkPage : ContentPage
{
    private readonly WalkPageViewModel _viewModel;
    private bool _isMapLoaded = false;

    public WalkPage()
    {
        InitializeComponent();
        _viewModel = new WalkPageViewModel();
        BindingContext = _viewModel;

        _viewModel.LocationUpdated += OnLocationUpdated;
        _viewModel.WalkReset += OnWalkReset;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isMapLoaded)
        {
            _isMapLoaded = true;
            await InitializeMapAsync();
        }
    }

    private async Task InitializeMapAsync()
    {
        double startLat = 52.2297;
        double startLon = 21.0122;

        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                var loc = await Geolocation.Default.GetLastKnownLocationAsync()
                          ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));

                if (loc != null)
                {
                    startLat = loc.Latitude;
                    startLon = loc.Longitude;
                }
            }
        }
        catch { }

        var existingCoords = _viewModel.RoutePoints
            .Select(p => new[] { p.Latitude, p.Longitude })
            .ToList();
        string coordsJson = JsonSerializer.Serialize(existingCoords);

        WalkMapWebView.Source = new HtmlWebViewSource
        {
            Html = GenerateWalkMapHtml(startLat, startLon, coordsJson)
        };
    }

    private void OnLocationUpdated(double lat, double lon, bool addToRoute)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                string latStr = lat.ToString(CultureInfo.InvariantCulture);
                string lonStr = lon.ToString(CultureInfo.InvariantCulture);
                string addJs = addToRoute ? "true" : "false";

                await WalkMapWebView.EvaluateJavaScriptAsync($"updatePosition({latStr}, {lonStr}, {addJs});");
            }
            catch { }
        });
    }

    private void OnWalkReset()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await WalkMapWebView.EvaluateJavaScriptAsync("resetRoute();");
            }
            catch { }
        });
    }

    private static string GenerateWalkMapHtml(double lat, double lon, string initialCoordsJson)
    {
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no' />
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body, html, #map {{ margin: 0; padding: 0; height: 100%; width: 100%; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var initialPoints = {initialCoordsJson};
        var startCenter = initialPoints.length > 0 ? initialPoints[initialPoints.length - 1] : [{latStr}, {lonStr}];

        var map = L.map('map').setView(startCenter, 16);

        L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{{z}}/{{y}}/{{x}}', {{
            maxZoom: 19,
            attribution: 'Tiles © Esri'
        }}).addTo(map);

        // Linia przebytej trasy
        var routeLine = L.polyline(initialPoints, {{
            color: '#0F766E',
            weight: 6,
            opacity: 0.85
        }}).addTo(map);

        // Marker aktualnej pozycji seniora
        var currentMarker = L.circleMarker(startCenter, {{
            color: '#FFFFFF',
            weight: 3,
            fillColor: '#2563EB',
            fillOpacity: 1,
            radius: 9
        }}).addTo(map).bindPopup('Twoja obecna pozycja');

        // Marker punktu startowego (jeśli trasa już istnieje)
        var startMarker = null;
        if (initialPoints.length > 0) {{
            startMarker = L.circleMarker(initialPoints[0], {{
                color: '#166534',
                fillColor: '#22C55E',
                fillOpacity: 0.9,
                radius: 7
            }}).addTo(map).bindPopup('Start spaceru');
        }}

        function updatePosition(lat, lon, addToRoute) {{
            var pos = [lat, lon];
            currentMarker.setLatLng(pos);
            map.panTo(pos);

            if (addToRoute) {{
                if (!startMarker && routeLine.getLatLngs().length === 0) {{
                    startMarker = L.circleMarker(pos, {{
                        color: '#166534',
                        fillColor: '#22C55E',
                        fillOpacity: 0.9,
                        radius: 7
                    }}).addTo(map).bindPopup('Start spaceru');
                }}
                routeLine.addLatLng(pos);
            }}
        }}

        function resetRoute() {{
            routeLine.setLatLngs([]);
            if (startMarker) {{
                map.removeLayer(startMarker);
                startMarker = null;
            }}
        }}
    </script>
</body>
</html>";
    }
}