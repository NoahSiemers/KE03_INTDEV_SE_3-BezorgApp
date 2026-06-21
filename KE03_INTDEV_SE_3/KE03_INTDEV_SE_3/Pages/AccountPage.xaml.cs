using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
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

        if (_appState.LoggedInDriverId == null)
        {
            NameLabel.Text = "Geen gebruiker ingelogd";
            EmailLabel.Text = "";
            return;
        }

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.Id == _appState.LoggedInDriverId);

        if (driver == null)
        {
            NameLabel.Text = "Geen gebruiker gevonden";
            EmailLabel.Text = "";
            return;
        }

        NameLabel.Text = driver.Name;
        EmailLabel.Text = driver.Email;
    }
}