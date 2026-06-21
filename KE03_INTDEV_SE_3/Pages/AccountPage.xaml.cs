using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;

namespace KE03_INTDEV_SE_3.Pages;

public partial class AccountPage : ContentPage
{
    private readonly AppDbContext _db;
    private readonly AppState _appState;

    public AccountPage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _appState = ServiceHelper.Services.GetRequiredService<AppState>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        if (_appState.LoggedInDriverId == null)
        {
            NameLabel.Text = "Geen gebruiker ingelogd";
            EmailLabel.Text = "";
            RideTimeLabel.Text = "Log opnieuw in om je dienst te bekijken.";
            BusRouteLabel.Text = "Geen route beschikbaar";
            PackageCountLabel.Text = "Geen pakketten beschikbaar";
            return;
        }

        Driver? driver = await _db.Drivers
            .FirstOrDefaultAsync(driverFromDatabase => driverFromDatabase.Id == _appState.LoggedInDriverId);

        if (driver == null)
        {
            NameLabel.Text = "Geen gebruiker gevonden";
            EmailLabel.Text = "";
            RideTimeLabel.Text = "Geen dienst gevonden";
            BusRouteLabel.Text = "Geen route beschikbaar";
            PackageCountLabel.Text = "Geen pakketten beschikbaar";
            return;
        }

        NameLabel.Text = driver.Name;
        EmailLabel.Text = driver.Email;

        DateTime today = DateTime.Today;

        Ride? todayRide = await _db.Rides
            .Where(ride => ride.DriverId == driver.Id)
            .Where(ride => ride.RideDate.Date == today)
            .OrderBy(ride => ride.StartTime)
            .FirstOrDefaultAsync();

        if (todayRide == null)
        {
            RideTimeLabel.Text = "Vandaag geen dienst";
            BusRouteLabel.Text = "Geen bus of route gekoppeld";
            PackageCountLabel.Text = "Geen pakketten ingepland";
            return;
        }

        RideTimeLabel.Text = $"{todayRide.StartTime:HH:mm} - {todayRide.EndTime:HH:mm}";
        BusRouteLabel.Text = $"{todayRide.BusName} • {todayRide.Region}";
        PackageCountLabel.Text = $"{todayRide.PackageCount} pakketten";
    }

    private async void OnSickReportTapped(object sender, TappedEventArgs e)
    {
        SickReportPage sickReportPage = new SickReportPage();
        await Navigation.PushAsync(sickReportPage);
    }

    private async void OnHelpTapped(object sender, TappedEventArgs e)
    {
        HelpPage helpPage = new HelpPage();
        await Navigation.PushAsync(helpPage);
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool shouldLogout = await DisplayAlert(
            "Uitloggen",
            "Weet je zeker dat je wilt uitloggen?",
            "Uitloggen",
            "Annuleren");

        if (!shouldLogout)
        {
            return;
        }

        _appState.LoggedInDriverId = null;
        _appState.SelectedRideId = null;

        Application.Current!.MainPage = new NavigationPage(new LoginPage());
    }
}
