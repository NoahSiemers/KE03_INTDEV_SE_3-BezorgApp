using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;

namespace KE03_INTDEV_SE_3.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AppDbContext _db;
    private readonly AppState _appState;

    public LoginPage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _appState = ServiceHelper.Services.GetRequiredService<AppState>();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Inloggen mislukt", "Vul je email en wachtwoord in.", "Ok");
            return;
        }

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.Email == email && d.Password == password);

        if (driver == null)
        {
            await DisplayAlert("Inloggen mislukt", "De gegevens kloppen niet. Gebruik bijvoorbeeld noah@matrix.nl en wachtwoord 1234.", "Ok");
            return;
        }

        _appState.LoggedInDriverId = driver.Id;

        var todayRide = await _db.Rides
            .FirstOrDefaultAsync(r => r.DriverId == driver.Id && r.RideDate.Date == DateTime.Today);

        _appState.SelectedRideId = todayRide?.Id;

        Application.Current!.MainPage = new AppShell();
    }

    private async void OnHelpLabelTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new HelpPage());
    }

    private async void OnForgotPasswordTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlert(
            "Wachtwoord vergeten",
            "Neem contact op met je planner of teamleider om je wachtwoord opnieuw in te stellen.",
            "Ok");
    }
}