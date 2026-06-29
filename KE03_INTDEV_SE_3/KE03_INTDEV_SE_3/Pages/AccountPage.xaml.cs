using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;

namespace KE03_INTDEV_SE_3.Pages;

public partial class AccountPage : ContentPage
{
    private const int SickReportDeadlineHours = 3;
    private const double MinimumRecommendedBatteryLevel = 0.20;

    private readonly AppDbContext _db;
    private readonly AppState _appState;

    private Ride? _todayRide;

    public AccountPage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _appState = ServiceHelper.Services.GetRequiredService<AppState>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadAccountAsync();
        RenderReadinessCheck();
    }

    private async Task LoadAccountAsync()
    {
        TodayDateLabel.Text = DateTime.Today.ToString("dddd dd MMMM yyyy");

        if (_appState.LoggedInDriverId == null)
        {
            ShowLoggedOutState();
            return;
        }

        Driver? driver = await _db.Drivers
            .FirstOrDefaultAsync(driverFromDatabase =>
                driverFromDatabase.Id == _appState.LoggedInDriverId);

        if (driver == null)
        {
            ShowDriverNotFoundState();
            return;
        }

        RenderDriver(driver);

        _todayRide = await _db.Rides
            .Include(ride => ride.Packages)
            .Where(ride => ride.DriverId == driver.Id)
            .Where(ride => ride.RideDate.Date == DateTime.Today)
            .OrderBy(ride => ride.StartTime)
            .FirstOrDefaultAsync();

        RenderTodayRide(_todayRide);
    }

    private void RenderDriver(Driver driver)
    {
        NameLabel.Text = driver.Name;
        EmailLabel.Text = driver.Email;
        InitialsLabel.Text = GetInitials(driver.Name);

        LoginStatusLabel.Text = "Ingelogd";
        LoginStatusBadge.BackgroundColor = Color.FromArgb("#ECFDF3");
        LoginStatusBadge.BorderColor = Color.FromArgb("#ABEFC6");
        LoginStatusLabel.TextColor = Color.FromArgb("#027A48");
    }

    private void RenderTodayRide(Ride? ride)
    {
        if (ride == null)
        {
            RenderNoShiftState();
            return;
        }

        int totalPackages = ride.Packages.Count;
        int completedPackages = ride.Packages.Count(package => package.IsCompleted);
        int openPackages = Math.Max(totalPackages - completedPackages, 0);
        int progressPercentage = CalculateProgressPercentage(completedPackages, totalPackages);

        ShiftStatusLabel.Text = GetShiftStatusText(ride);
        ShiftStatusLabel.TextColor = GetShiftStatusColor(ride);
        ShiftStatusBadge.BackgroundColor = GetShiftStatusBackgroundColor(ride);
        ShiftStatusBadge.BorderColor = GetShiftStatusBorderColor(ride);

        TotalPackagesLabel.Text = totalPackages.ToString();
        CompletedPackagesLabel.Text = completedPackages.ToString();
        OpenPackagesLabel.Text = openPackages.ToString();
        ProgressPercentageLabel.Text = $"{progressPercentage}%";

        ShiftTimeLabel.Text = $"{ride.StartTime:HH:mm} - {ride.EndTime:HH:mm}";
        DepotLabel.Text = ride.BranchLocation;
        BusRouteLabel.Text = $"{ride.BusName} • {ride.Region}";
        PackageProgressLabel.Text = $"{completedPackages} van {totalPackages} pakketten afgerond";

        RenderRouteImpact(ride, openPackages, totalPackages);
        RenderSickReportHint(ride, openPackages);
    }

    private void RenderNoShiftState()
    {
        ShiftStatusLabel.Text = "Geen dienst";
        ShiftStatusLabel.TextColor = Color.FromArgb("#6B7280");
        ShiftStatusBadge.BackgroundColor = Color.FromArgb("#F9FAFB");
        ShiftStatusBadge.BorderColor = Color.FromArgb("#E5E7EB");

        TotalPackagesLabel.Text = "0";
        CompletedPackagesLabel.Text = "0";
        OpenPackagesLabel.Text = "0";
        ProgressPercentageLabel.Text = "0%";

        ShiftTimeLabel.Text = "Vandaag geen dienst";
        DepotLabel.Text = "Geen depot gekoppeld";
        BusRouteLabel.Text = "Geen bus of regio gekoppeld";
        PackageProgressLabel.Text = "Geen pakketten ingepland";

        RouteImpactTitleLabel.Text = "Geen route gekoppeld";
        RouteImpactDescriptionLabel.Text =
            "Er staat vandaag geen rit voor je ingepland. Daardoor is er geen route-impact voor de planning.";

        SickReportHintFrame.BackgroundColor = Color.FromArgb("#EFF6FF");
        SickReportHintFrame.BorderColor = Color.FromArgb("#BFDBFE");
        SickReportHintLabel.TextColor = Color.FromArgb("#1D4ED8");
        SickReportHintLabel.Text =
            "Je hebt vandaag geen dienst. Ziekmelden via de app is daarom niet nodig.";
    }

    private void RenderRouteImpact(Ride ride, int openPackages, int totalPackages)
    {
        RouteImpactTitleLabel.Text = $"{ride.Region} • {ride.BusName}";

        if (totalPackages == 0)
        {
            RouteImpactDescriptionLabel.Text =
                $"Deze dienst is gekoppeld aan depot {ride.BranchLocation}, maar er zijn nog geen pakketten aan de route gekoppeld.";

            return;
        }

        if (openPackages == 0)
        {
            RouteImpactDescriptionLabel.Text =
                "Alle pakketten op deze route zijn afgerond. De operationele impact bij uitval is nu laag.";

            return;
        }

        RouteImpactDescriptionLabel.Text =
            $"Als je uitvalt, moeten {openPackages} openstaande pakketten opnieuw worden verdeeld. " +
            $"De planner moet rekening houden met depot {ride.BranchLocation}, regio {ride.Region} en bus {ride.BusName}.";
    }

    private void RenderSickReportHint(Ride ride, int openPackages)
    {
        DateTime latestAppReportTime = GetLatestAppReportTime(ride);
        bool canReportInApp = CanReportSickInApp(ride);

        if (canReportInApp)
        {
            SickReportHintFrame.BackgroundColor = Color.FromArgb("#ECFDF3");
            SickReportHintFrame.BorderColor = Color.FromArgb("#ABEFC6");
            SickReportHintLabel.TextColor = Color.FromArgb("#027A48");
            SickReportHintLabel.Text =
                $"Ziekmelden via de app kan tot {latestAppReportTime:HH:mm}. " +
                $"Bij ziekmelding ziet de planner dat deze route nog {openPackages} openstaande pakketten heeft.";

            return;
        }

        SickReportHintFrame.BackgroundColor = Color.FromArgb("#FEF2F2");
        SickReportHintFrame.BorderColor = Color.FromArgb("#FCA5A5");
        SickReportHintLabel.TextColor = Color.FromArgb("#B42318");
        SickReportHintLabel.Text =
            "Je dienst begint binnen 3 uur. Bij ziekte moet je nu telefonisch contact opnemen met de planner, " +
            "zodat klanten geen verkeerde bezorginformatie krijgen.";
    }

    private void RenderReadinessCheck()
    {
        bool hasInternet = HasInternetConnection();
        bool batteryIsSafe = IsBatteryLevelSafe(out string batteryText);
        string deviceText = GetDeviceDescription();

        ConnectivityStatusLabel.Text = hasInternet
            ? "Internet beschikbaar"
            : "Geen internetverbinding";

        ConnectivityStatusLabel.TextColor = hasInternet
            ? Color.FromArgb("#027A48")
            : Color.FromArgb("#B42318");

        DeviceStatusLabel.Text = deviceText;
        BatteryStatusLabel.Text = batteryText;

        BatteryStatusLabel.TextColor = batteryIsSafe
            ? Color.FromArgb("#027A48")
            : Color.FromArgb("#B42318");

        LastReadinessCheckLabel.Text = DateTime.Now.ToString("HH:mm");

        if (_todayRide == null)
        {
            SetReadinessBadge(
                "Geen dienst",
                Color.FromArgb("#6B7280"),
                Color.FromArgb("#F9FAFB"),
                Color.FromArgb("#E5E7EB"));

            ReadinessAdviceFrame.BackgroundColor = Color.FromArgb("#EFF6FF");
            ReadinessAdviceFrame.BorderColor = Color.FromArgb("#BFDBFE");
            ReadinessAdviceLabel.TextColor = Color.FromArgb("#1D4ED8");
            ReadinessAdviceLabel.Text =
                "Je hebt vandaag geen geplande dienst. De technische controle is wel uitgevoerd, maar er is geen route om te starten.";

            return;
        }

        if (hasInternet && batteryIsSafe)
        {
            SetReadinessBadge(
                "Klaar",
                Color.FromArgb("#027A48"),
                Color.FromArgb("#ECFDF3"),
                Color.FromArgb("#ABEFC6"));

            ReadinessAdviceFrame.BackgroundColor = Color.FromArgb("#ECFDF3");
            ReadinessAdviceFrame.BorderColor = Color.FromArgb("#ABEFC6");
            ReadinessAdviceLabel.TextColor = Color.FromArgb("#027A48");
            ReadinessAdviceLabel.Text =
                "Je apparaat is klaar voor de dienst. Controleer nog wel fysiek je bus, pakketten en eventuele route-instructies.";

            return;
        }

        SetReadinessBadge(
            "Actie nodig",
            Color.FromArgb("#B42318"),
            Color.FromArgb("#FEF2F2"),
            Color.FromArgb("#FCA5A5"));

        ReadinessAdviceFrame.BackgroundColor = Color.FromArgb("#FEF2F2");
        ReadinessAdviceFrame.BorderColor = Color.FromArgb("#FCA5A5");
        ReadinessAdviceLabel.TextColor = Color.FromArgb("#B42318");
        ReadinessAdviceLabel.Text =
            "Los de melding op voordat je vertrekt. Zonder goede verbinding of voldoende batterij kunnen statusupdates te laat worden verwerkt.";
    }

    private void ShowLoggedOutState()
    {
        _todayRide = null;

        NameLabel.Text = "Geen gebruiker ingelogd";
        EmailLabel.Text = "Log opnieuw in om je profiel te bekijken.";
        InitialsLabel.Text = "?";

        LoginStatusLabel.Text = "Uitgelogd";
        LoginStatusBadge.BackgroundColor = Color.FromArgb("#FEF2F2");
        LoginStatusBadge.BorderColor = Color.FromArgb("#FCA5A5");
        LoginStatusLabel.TextColor = Color.FromArgb("#B42318");

        RenderNoShiftState();
    }

    private void ShowDriverNotFoundState()
    {
        _todayRide = null;

        NameLabel.Text = "Gebruiker niet gevonden";
        EmailLabel.Text = "De ingelogde gebruiker bestaat niet meer in de database.";
        InitialsLabel.Text = "?";

        RenderNoShiftState();
    }

    private static int CalculateProgressPercentage(int completedPackages, int totalPackages)
    {
        if (totalPackages <= 0)
        {
            return 0;
        }

        return (int)Math.Round((double)completedPackages / totalPackages * 100);
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        string[] parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            return parts[0][0].ToString().ToUpper();
        }

        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
    }

    private static string GetShiftStatusText(Ride ride)
    {
        DateTime now = DateTime.Now;

        if (now < ride.StartTime)
        {
            return "Nog niet gestart";
        }

        if (now > ride.EndTime)
        {
            return "Afgelopen";
        }

        return "Actief";
    }

    private static Color GetShiftStatusColor(Ride ride)
    {
        DateTime now = DateTime.Now;

        if (now < ride.StartTime)
        {
            return Color.FromArgb("#1D4ED8");
        }

        if (now > ride.EndTime)
        {
            return Color.FromArgb("#6B7280");
        }

        return Color.FromArgb("#027A48");
    }

    private static Color GetShiftStatusBackgroundColor(Ride ride)
    {
        DateTime now = DateTime.Now;

        if (now < ride.StartTime)
        {
            return Color.FromArgb("#EFF6FF");
        }

        if (now > ride.EndTime)
        {
            return Color.FromArgb("#F9FAFB");
        }

        return Color.FromArgb("#ECFDF3");
    }

    private static Color GetShiftStatusBorderColor(Ride ride)
    {
        DateTime now = DateTime.Now;

        if (now < ride.StartTime)
        {
            return Color.FromArgb("#BFDBFE");
        }

        if (now > ride.EndTime)
        {
            return Color.FromArgb("#E5E7EB");
        }

        return Color.FromArgb("#ABEFC6");
    }

    private static DateTime GetLatestAppReportTime(Ride ride)
    {
        return ride.StartTime.AddHours(-SickReportDeadlineHours);
    }

    private static bool CanReportSickInApp(Ride ride)
    {
        return DateTime.Now <= GetLatestAppReportTime(ride);
    }

    private static bool HasInternetConnection()
    {
        try
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsBatteryLevelSafe(out string batteryText)
    {
        try
        {
            double batteryLevel = Battery.Default.ChargeLevel;
            BatteryState batteryState = Battery.Default.State;

            int batteryPercentage = (int)Math.Round(batteryLevel * 100);

            batteryText = batteryState switch
            {
                BatteryState.Charging => $"{batteryPercentage}% • aan het opladen",
                BatteryState.Full => $"{batteryPercentage}% • volledig opgeladen",
                BatteryState.Discharging => $"{batteryPercentage}% • op batterij",
                BatteryState.NotCharging => $"{batteryPercentage}% • niet aan het opladen",
                _ => $"{batteryPercentage}% • status onbekend"
            };

            return batteryLevel >= MinimumRecommendedBatteryLevel
                   || batteryState == BatteryState.Charging
                   || batteryState == BatteryState.Full;
        }
        catch
        {
            batteryText = "Batterijstatus niet beschikbaar";
            return true;
        }
    }

    private static string GetDeviceDescription()
    {
        try
        {
            return $"{DeviceInfo.Current.Platform} • {DeviceInfo.Current.Model}";
        }
        catch
        {
            return "Apparaatgegevens niet beschikbaar";
        }
    }

    private void SetReadinessBadge(
        string text,
        Color textColor,
        Color backgroundColor,
        Color borderColor)
    {
        ReadinessStatusLabel.Text = text;
        ReadinessStatusLabel.TextColor = textColor;
        ReadinessStatusBadge.BackgroundColor = backgroundColor;
        ReadinessStatusBadge.BorderColor = borderColor;
    }

    private async void OnSickReportTapped(object sender, TappedEventArgs e)
    {
        GiveLightFeedback();

        if (_appState.LoggedInDriverId == null)
        {
            await DisplayAlert(
                "Niet ingelogd",
                "Log opnieuw in voordat je een ziekmelding kunt openen.",
                "Oké");

            return;
        }

        await Navigation.PushAsync(new SickReportPage());
    }

    private async void OnHelpTapped(object sender, TappedEventArgs e)
    {
        GiveLightFeedback();
        await Navigation.PushAsync(new HelpPage());
    }

    private async void OnRefreshTapped(object sender, TappedEventArgs e)
    {
        GiveLightFeedback();

        await LoadAccountAsync();
        RenderReadinessCheck();

        await DisplayAlert(
            "Bijgewerkt",
            "Je profiel-, route- en dienstgereed-informatie is opnieuw geladen.",
            "Oké");
    }

    private void OnReadinessCheckTapped(object sender, EventArgs e)
    {
        GiveLightFeedback();
        RenderReadinessCheck();
    }

    private async void OnNotificationsToggled(object sender, ToggledEventArgs e)
    {
        GiveLightFeedback();

        string message = e.Value
            ? "Meldingen voor ritten en pakketwijzigingen staan aan."
            : "Meldingen zijn uitgezet.";

        await DisplayAlert("Meldingen", message, "Oké");
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

        GiveLightFeedback();

        _appState.LoggedInDriverId = null;
        _appState.SelectedRideId = null;

        Application.Current!.MainPage = new NavigationPage(new LoginPage())
        {
            BarBackgroundColor = Color.FromArgb("#1F2937"),
            BarTextColor = Colors.White
        };
    }

    private static void GiveLightFeedback()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Haptische feedback is ondersteunend en niet op elk apparaat beschikbaar.
        }
    }
}