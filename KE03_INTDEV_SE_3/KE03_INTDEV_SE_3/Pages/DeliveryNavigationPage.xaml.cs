using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;

namespace KE03_INTDEV_SE_3.Pages;

[QueryProperty(nameof(PackageId), "PackageId")]
public partial class DeliveryNavigationPage : ContentPage
{
    private readonly AppDbContext _db;
    private readonly NavigationService _navigationService;

    private PackageItem? _package;
    private Location? _currentLocation;
    private Location? _destinationLocation;
    private NavigationRoute? _route;

    public ObservableCollection<NavigationStep> Steps { get; } = new();

    public int PackageId { get; set; }

    public DeliveryNavigationPage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _navigationService = ServiceHelper.Services.GetRequiredService<NavigationService>();

        StepsCollection.ItemsSource = Steps;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (PackageId <= 0)
            return;

        await LoadRouteAsync();
    }

    private async Task LoadRouteAsync()
    {
        Steps.Clear();

        _package = await _db.Packages
            .Include(p => p.Ride)
            .FirstOrDefaultAsync(p => p.Id == PackageId);

        if (_package == null)
        {
            await DisplayAlertAsync("Navigatie", "Pakket niet gevonden.", "Ok");
            await Navigation.PopAsync();
            return;
        }

        AddressLabel.Text = _package.Address;

        _currentLocation = await _navigationService.GetCurrentLocationAsync();
        if (_currentLocation == null)
        {
            await DisplayAlertAsync("Locatie", "Kan je huidige locatie niet bepalen.", "Ok");
            return;
        }

        _destinationLocation = _package.Latitude.HasValue && _package.Longitude.HasValue
            ? new Location(_package.Latitude.Value, _package.Longitude.Value)
            : await _navigationService.GeocodeAsync(_package.Address);

        if (_destinationLocation == null)
        {
            await DisplayAlertAsync("Locatie", "Adres kon niet worden omgezet naar een locatie.", "Ok");
            return;
        }

        _route = await _navigationService.BuildDrivingRouteAsync(_currentLocation, _destinationLocation);
        if (_route == null)
        {
            await DisplayAlertAsync("Route", "Route kon niet worden geladen.", "Ok");
            return;
        }

        foreach (var step in _route.Steps)
            Steps.Add(step);

        NextInstructionLabel.Text = _route.Steps.FirstOrDefault()?.Instruction ?? "Route geladen";
        DistanceLabel.Text = $"{_route.DistanceMeters / 1000:0.0} km";
        EtaLabel.Text = $"±{TimeSpan.FromSeconds(_route.DurationSeconds):hh\\:mm}";

        DrawRoute();
    }

    private void DrawRoute()
    {
        if (_route == null || _currentLocation == null || _destinationLocation == null)
            return;

        RouteMap.Pins.Clear();
        RouteMap.MapElements.Clear();

        var polyline = new Polyline
        {
            StrokeColor = Colors.DodgerBlue,
            StrokeWidth = 8
        };

        foreach (var point in _route.Path)
            polyline.Geopath.Add(point);

        RouteMap.MapElements.Add(polyline);

        RouteMap.Pins.Add(new Pin
        {
            Label = "Jij",
            Location = _currentLocation,
            Type = PinType.Place
        });

        RouteMap.Pins.Add(new Pin
        {
            Label = _package?.CustomerName ?? "Bestemming",
            Location = _destinationLocation,
            Type = PinType.Place
        });

        var minLat = Math.Min(_currentLocation.Latitude, _destinationLocation.Latitude);
        var maxLat = Math.Max(_currentLocation.Latitude, _destinationLocation.Latitude);
        var minLon = Math.Min(_currentLocation.Longitude, _destinationLocation.Longitude);
        var maxLon = Math.Max(_currentLocation.Longitude, _destinationLocation.Longitude);

        var center = new Location((minLat + maxLat) / 2, (minLon + maxLon) / 2);
        var roughSpanKm = Math.Max(1.0, Math.Max(maxLat - minLat, maxLon - minLon) * 111 * 0.75);

        RouteMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(roughSpanKm)));
    }

    private async void OnDeliveredClicked(object? sender, EventArgs e)
    {
        if (_package == null)
            return;

        _package.IsCompleted = true;
        await _db.SaveChangesAsync();

        await DisplayAlertAsync("Geleverd", "Pakket is gemarkeerd als geleverd.", "Ok");
        await Navigation.PopAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}