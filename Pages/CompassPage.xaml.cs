using System.Globalization;
using System.Text.Json;
using AplikacjaMobilna.PageModels;
using AplikacjaMobilna.Services;

namespace AplikacjaMobilna.Pages;

public partial class CompassPage : ContentPage
{
    private readonly CompassPageViewModel _viewModel;
    private readonly LocalDatabaseService _dbService;
    private static readonly HttpClient _httpClient = new();

    public CompassPage()
    {
        InitializeComponent();
        _viewModel = new CompassPageViewModel();
        _dbService = new LocalDatabaseService();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadUserHomeAsync();
    }

    private async void OnAddHomeClicked(object? sender, EventArgs e)
    {
        AddressSearchEntry.Text = string.Empty;
        MapOverlay.IsVisible = true;
        await LoadMapAsync();
    }

    private void OnCloseMapClicked(object? sender, EventArgs e)
    {
        MapOverlay.IsVisible = false;
    }

    // Obsługa wyszukiwania lokalizacji po wpisaniu tekstu
    private async void OnSearchAddressClicked(object? sender, EventArgs e)
    {
        string query = AddressSearchEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            await DisplayAlertAsync("Uwaga", "Wpisz miasto, ulicę lub adres do wyszukania.", "OK");
            return;
        }

        // Ukryj klawiaturę ekranową po kliknięciu Szukaj
        AddressSearchEntry.Unfocus();

        Location? foundLocation = await GeocodeAddressAsync(query);

        if (foundLocation == null)
        {
            await DisplayAlertAsync("Nie znaleziono", $"Nie udało się znaleźć adresu: \"{query}\". Spróbuj podać dokładniejszą nazwę miasta i ulicy.", "OK");
            return;
        }

        string latStr = foundLocation.Latitude.ToString(CultureInfo.InvariantCulture);
        string lonStr = foundLocation.Longitude.ToString(CultureInfo.InvariantCulture);

        // Przesuwamy mapę i ustawiamy znacznik w JavaScript
        await HomeMapWebView.EvaluateJavaScriptAsync($"setHomeFromSearch({latStr}, {lonStr});");

        bool saveNow = await DisplayAlertAsync(
            "Znaleziono lokalizację",
            $"Czy zapisać znaleziony punkt ({foundLocation.Latitude:F5}, {foundLocation.Longitude:F5}) jako Twój DOM?\n\nJeśli chcesz poprawić punkt, wybierz 'Popraw na mapie'.",
            "Zapisz jako DOM",
            "Popraw na mapie");

        if (saveNow)
        {
            await SaveHomeAndCloseAsync(foundLocation.Latitude, foundLocation.Longitude);
        }
    }

    // Wyszukiwanie współrzędnych
    private async Task<Location?> GeocodeAddressAsync(string address)
    {
        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(address);
            var first = locations?.FirstOrDefault();
            if (first != null)
                return first;
        }
        catch { /* Fallback do Nominatim poniżej */ }

        try
        {
            string url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(address)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("AplikacjaSenioraMAUI/1.0");

            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.GetArrayLength() > 0)
                {
                    var item = doc.RootElement[0];
                    if (double.TryParse(item.GetProperty("lat").GetString(), CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(item.GetProperty("lon").GetString(), CultureInfo.InvariantCulture, out double lon))
                    {
                        return new Location(lat, lon);
                    }
                }
            }
        }
        catch { }

        return null;
    }

    private async Task SaveHomeAndCloseAsync(double lat, double lon)
    {
        string currentUser = LocalDatabaseService.CurrentUsername ?? "admin";
        await _dbService.SaveUserHomeLocationAsync(currentUser, lat, lon);
        await _viewModel.LoadUserHomeAsync();

        MapOverlay.IsVisible = false;
        await DisplayAlertAsync("Zapisano", $"Punkt DOM został zapisany dla konta: {currentUser}", "OK");
    }

    private async Task LoadMapAsync()
    {
        double startLat = 52.2297;
        double startLon = 21.0122;

        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.Default.GetLastKnownLocationAsync()
                               ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));

                if (location != null)
                {
                    startLat = location.Latitude;
                    startLon = location.Longitude;
                }
            }
        }
        catch { }

        Location? savedHome = null;
        if (!string.IsNullOrEmpty(LocalDatabaseService.CurrentUsername))
        {
            savedHome = await _dbService.GetUserHomeLocationAsync(LocalDatabaseService.CurrentUsername);
        }

        double homeLat = savedHome?.Latitude ?? startLat;
        double homeLon = savedHome?.Longitude ?? startLon;
        bool hasHome = savedHome != null;

        double centerLat = hasHome ? homeLat : startLat;
        double centerLon = hasHome ? homeLon : startLon;

        HomeMapWebView.Source = new HtmlWebViewSource
        {
            Html = GenerateEsriMapHtml(centerLat, centerLon, startLat, startLon, homeLat, homeLon, hasHome)
        };
    }

    private async void OnMapNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("https://sethome.local/", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;

            try
            {
                var uri = new Uri(e.Url);
                var query = uri.Query.TrimStart('?');
                var parts = query.Split('&');

                if (parts.Length == 2 &&
                    double.TryParse(parts[0].Replace("lat=", ""), CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(parts[1].Replace("lon=", ""), CultureInfo.InvariantCulture, out double lon))
                {
                    await SaveHomeAndCloseAsync(lat, lon);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Błąd zapisu", ex.Message, "OK");
            }
        }
    }

    private string GenerateEsriMapHtml(double centerLat, double centerLon, double userLat, double userLon, double homeLat, double homeLon, bool hasHome)
    {
        string cLat = centerLat.ToString(CultureInfo.InvariantCulture);
        string cLon = centerLon.ToString(CultureInfo.InvariantCulture);
        string uLat = userLat.ToString(CultureInfo.InvariantCulture);
        string uLon = userLon.ToString(CultureInfo.InvariantCulture);
        string hLat = homeLat.ToString(CultureInfo.InvariantCulture);
        string hLon = homeLon.ToString(CultureInfo.InvariantCulture);
        string hasHomeJs = hasHome ? "true" : "false";

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
        var map = L.map('map').setView([{cLat}, {cLon}], 16);

        L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{{z}}/{{y}}/{{x}}', {{
            maxZoom: 19,
            attribution: 'Tiles © Esri'
        }}).addTo(map);

        L.circleMarker([{uLat}, {uLon}], {{
            color: '#1976D2', fillColor: '#2196F3', fillOpacity: 0.8, radius: 8
        }}).addTo(map).bindPopup('Twoja pozycja');

        var homeMarker = null;
        if ({hasHomeJs}) {{
            homeMarker = L.marker([{hLat}, {hLon}]).addTo(map).bindPopup('🏠 Twój zapisany DOM').openPopup();
        }}

        // Funkcja wywoływana z C# po wyszukaniu adresu tekstowego
        function setHomeFromSearch(lat, lon) {{
            var latlng = L.latLng(lat, lon);
            map.setView(latlng, 17);
            if (homeMarker) {{
                homeMarker.setLatLng(latlng);
            }} else {{
                homeMarker = L.marker(latlng).addTo(map);
            }}
            homeMarker.bindPopup('🏠 Znaleziony adres').openPopup();
        }}

        map.on('click', function(e) {{
            var lat = e.latlng.lat.toFixed(6);
            var lon = e.latlng.lng.toFixed(6);

            if (homeMarker) {{
                homeMarker.setLatLng(e.latlng);
            }} else {{
                homeMarker = L.marker(e.latlng).addTo(map);
            }}
            homeMarker.bindPopup('🏠 Zapisywanie...').openPopup();

            window.location.href = 'https://sethome.local/?lat=' + lat + '&lon=' + lon;
        }});
    </script>
</body>
</html>";
    }
    private async void OnUseCurrentLocationClicked(object? sender, EventArgs e)
    {
        UseCurrentLocationBtn.IsEnabled = false;
        UseCurrentLocationBtn.Text = "⏳ Pobieranie pozycji GPS...";

        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlertAsync("Brak uprawnień", "Zezwól aplikacji na dostęp do lokalizacji GPS, aby ustawić obecną pozycję jako dom.", "OK");
                return;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request)
                           ?? await Geolocation.Default.GetLastKnownLocationAsync();

            if (location == null)
            {
                await DisplayAlertAsync("Błąd GPS", "Nie udało się ustalić obecnej pozycji. Upewnij się, że masz włączony moduł GPS w telefonie.", "OK");
                return;
            }

            string latStr = location.Latitude.ToString(CultureInfo.InvariantCulture);
            string lonStr = location.Longitude.ToString(CultureInfo.InvariantCulture);

            await HomeMapWebView.EvaluateJavaScriptAsync($"setHomeFromSearch({latStr}, {lonStr});");

            await SaveHomeAndCloseAsync(location.Latitude, location.Longitude);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Błąd lokalizacji", $"Wystąpił problem z pobraniem pozycji: {ex.Message}", "OK");
        }
        finally
        {
            UseCurrentLocationBtn.IsEnabled = true;
            UseCurrentLocationBtn.Text = "📍 Ustaw obecną pozycję jako DOM";
        }
    }
}