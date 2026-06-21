using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.ApplicationModel.Communication;

namespace KE03_INTDEV_SE_3.Pages;

public partial class SickReportPage : ContentPage
{
    private readonly AppDbContext _db;
    private readonly AppState _appState;

    private Ride? _todayRide;

    public SickReportPage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _appState = ServiceHelper.Services.GetRequiredService<AppState>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadTodayRideAsync();
        ShowCorrectPanel();
    }

    private async Task LoadTodayRideAsync()
    {
        if (_appState.LoggedInDriverId == null)
        {
            ShiftTimeLabel.Text = "Geen bezorger ingelogd";
            RouteInfoLabel.Text = "Log opnieuw in om je dienst te bekijken.";
            return;
        }

        DateTime today = DateTime.Today;

        _todayRide = await _db.Rides
            .Where(ride => ride.DriverId == _appState.LoggedInDriverId)
            .Where(ride => ride.RideDate.Date == today)
            .OrderBy(ride => ride.StartTime)
            .FirstOrDefaultAsync();

        if (_todayRide == null)
        {
            ShiftTimeLabel.Text = "Geen dienst gevonden";
            RouteInfoLabel.Text = "Er staat vandaag geen rit voor je ingepland.";
            return;
        }

        ShiftTimeLabel.Text = $"{_todayRide.StartTime:HH:mm} - {_todayRide.EndTime:HH:mm}";
        RouteInfoLabel.Text = $"{_todayRide.BusName} • {_todayRide.Region}";
    }

    private void ShowCorrectPanel()
    {
        if (_todayRide == null)
        {
            SickReportFormPanel.IsVisible = false;
            CallPlannerPanel.IsVisible = false;
            return;
        }

        DateTime currentTime = DateTime.Now;
        DateTime latestAppReportTime = _todayRide.StartTime.AddHours(-3);

        bool canReportSickInApp = currentTime <= latestAppReportTime;

        SickReportFormPanel.IsVisible = canReportSickInApp;
        CallPlannerPanel.IsVisible = !canReportSickInApp;
    }

    private async void OnSubmitSickReportClicked(object sender, EventArgs e)
    {
        if (ReasonPicker.SelectedItem == null)
        {
            await DisplayAlert(
                "Reden ontbreekt",
                "Kies eerst een reden voor je ziekmelding.",
                "Oké");

            return;
        }

        string selectedReason = ReasonPicker.SelectedItem.ToString() ?? "Niet opgegeven";
        string expectedReturn = ReturnPicker.SelectedItem?.ToString() ?? "Onbekend";
        string description = DescriptionEditor.Text ?? string.Empty;

        await DisplayAlert(
            "Ziekmelding verstuurd",
            $"Je ziekmelding is geregistreerd.\n\nReden: {selectedReason}\nVerwachte terugkeer: {expectedReturn}",
            "Oké");

        await Navigation.PopAsync();
    }

    private async void OnCallPlannerClicked(object sender, EventArgs e)
    {
        string plannerPhoneNumber = "0698765432";

        try
        {
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open(plannerPhoneNumber);
                return;
            }

            await DisplayAlert(
                "Bellen niet mogelijk",
                "Dit apparaat ondersteunt automatisch bellen niet.",
                "Oké");
        }
        catch
        {
            await DisplayAlert(
                "Fout",
                "De planner kan niet automatisch worden gebeld.",
                "Oké");
        }
    }

    private async void OnBackToProfileClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}