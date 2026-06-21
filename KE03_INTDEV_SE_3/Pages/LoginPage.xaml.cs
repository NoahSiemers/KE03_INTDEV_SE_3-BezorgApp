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
        await LoginAsync();
    }

    private async void OnPasswordCompleted(object sender, EventArgs e)
    {
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert(
                "Inloggen mislukt",
                "Vul je email en wachtwoord in.",
                "Oké");

            return;
        }

        try
        {
            var driver = await _db.Drivers
                .FirstOrDefaultAsync(driverFromDatabase =>
                    driverFromDatabase.Email.ToLower() == email.ToLower()
                    && driverFromDatabase.Password == password);

            if (driver == null)
            {
                await DisplayAlert(
                    "Inloggen mislukt",
                    "De gegevens kloppen niet. Gebruik Test@gmail.com met wachtwoord 1234.",
                    "Oké");

                return;
            }

            _appState.LoggedInDriverId = driver.Id;

            var todayRide = await _db.Rides
                .FirstOrDefaultAsync(ride =>
                    ride.DriverId == driver.Id
                    && ride.RideDate.Date == DateTime.Today);

            _appState.SelectedRideId = todayRide?.Id;

            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Technische fout",
                $"Er ging iets mis tijdens het inloggen:\n\n{ex.Message}",
                "Oké");
        }
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
            "Oké");
    }
}
