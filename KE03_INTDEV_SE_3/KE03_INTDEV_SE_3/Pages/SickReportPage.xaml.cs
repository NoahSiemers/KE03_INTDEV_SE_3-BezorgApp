using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;

namespace KE03_INTDEV_SE_3.Pages;

public partial class SickReportPage : ContentPage
{
    private const string PlannerPhoneNumber = "0698765432";
    private const int MinimumDescriptionLength = 10;
    private const int MaximumDescriptionLength = 160;
    private const int SickReportDeadlineHours = 3;

    private readonly AppDbContext _db;
    private readonly AppState _appState;

    private Ride? _todayRide;

    public SickReportPage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _appState = ServiceHelper.Services.GetRequiredService<AppState>();

        PlannerPhoneLabel.Text = FormatDutchPhoneNumber(PlannerPhoneNumber);
        DescriptionCounterLabel.Text = $"0/{MaximumDescriptionLength}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadTodayRideAsync();
        RenderPageState();
    }

    private async Task LoadTodayRideAsync()
    {
        if (_appState.LoggedInDriverId == null)
        {
            _todayRide = null;
            return;
        }

        _todayRide = await _db.Rides
            .Include(ride => ride.Packages)
            .Where(ride => ride.DriverId == _appState.LoggedInDriverId)
            .Where(ride => ride.RideDate.Date == DateTime.Today)
            .OrderBy(ride => ride.StartTime)
            .FirstOrDefaultAsync();
    }

    private void RenderPageState()
    {
        if (_todayRide == null)
        {
            RenderNoShiftState();
            return;
        }

        int totalPackages = _todayRide.Packages.Count;
        int completedPackages = _todayRide.Packages.Count(package => package.IsCompleted);
        int openPackages = Math.Max(totalPackages - completedPackages, 0);

        ShiftTimeLabel.Text = $"{_todayRide.StartTime:HH:mm} - {_todayRide.EndTime:HH:mm}";
        RouteLabel.Text = $"{_todayRide.BusName} • {_todayRide.Region}";
        DepotLabel.Text = _todayRide.BranchLocation;

        TotalPackagesLabel.Text = totalPackages.ToString();
        OpenPackagesLabel.Text = openPackages.ToString();

        RenderDeadlineState(_todayRide);
        RenderRouteImpact(_todayRide, totalPackages, openPackages);

        NoShiftPanel.IsVisible = false;
        SickReportFormPanel.IsVisible = CanReportSickInApp(_todayRide);
        CallPlannerPanel.IsVisible = !CanReportSickInApp(_todayRide);
    }

    private void RenderNoShiftState()
    {
        ShiftTimeLabel.Text = "Geen dienst vandaag";
        RouteLabel.Text = "Geen route ingepland";
        DepotLabel.Text = "Geen depot gekoppeld";

        TotalPackagesLabel.Text = "0";
        OpenPackagesLabel.Text = "0";

        DeadlineStatusFrame.BackgroundColor = Color.FromArgb("#EFF6FF");
        DeadlineStatusFrame.BorderColor = Color.FromArgb("#BFDBFE");
        DeadlineStatusTitleLabel.Text = "Geen ziekmelding nodig";
        DeadlineStatusTitleLabel.TextColor = Color.FromArgb("#1D4ED8");

        DeadlineBadgeFrame.BackgroundColor = Color.FromArgb("#EFF6FF");
        DeadlineBadgeFrame.BorderColor = Color.FromArgb("#BFDBFE");
        DeadlineBadgeLabel.Text = "Geen dienst";
        DeadlineBadgeLabel.TextColor = Color.FromArgb("#1D4ED8");

        RuleDescriptionLabel.Text =
            "Er staat vandaag geen dienst voor je ingepland. Daardoor is ziekmelden via de app niet nodig.";
        RuleDescriptionLabel.TextColor = Color.FromArgb("#1E3A8A");

        ImpactBadgeFrame.BackgroundColor = Color.FromArgb("#F9FAFB");
        ImpactBadgeFrame.BorderColor = Color.FromArgb("#E5E7EB");
        ImpactBadgeLabel.Text = "Geen route";
        ImpactBadgeLabel.TextColor = Color.FromArgb("#6B7280");

        RouteImpactDescriptionLabel.Text =
            "Er is geen gekoppelde route. De planning hoeft op basis van deze appmelding niets opnieuw te verdelen.";

        NoShiftPanel.IsVisible = true;
        SickReportFormPanel.IsVisible = false;
        CallPlannerPanel.IsVisible = false;
    }

    private void RenderDeadlineState(Ride ride)
    {
        DateTime latestAppReportTime = GetLatestAppReportTime(ride);
        bool canReportInApp = CanReportSickInApp(ride);

        if (canReportInApp)
        {
            DeadlineStatusFrame.BackgroundColor = Color.FromArgb("#ECFDF3");
            DeadlineStatusFrame.BorderColor = Color.FromArgb("#ABEFC6");
            DeadlineStatusTitleLabel.Text = "Appmelding mogelijk";
            DeadlineStatusTitleLabel.TextColor = Color.FromArgb("#027A48");

            DeadlineBadgeFrame.BackgroundColor = Color.FromArgb("#ECFDF3");
            DeadlineBadgeFrame.BorderColor = Color.FromArgb("#ABEFC6");
            DeadlineBadgeLabel.Text = $"Tot {latestAppReportTime:HH:mm}";
            DeadlineBadgeLabel.TextColor = Color.FromArgb("#027A48");

            RuleDescriptionLabel.Text =
                $"Je kunt deze ziekmelding via de app versturen tot {latestAppReportTime:HH:mm}. " +
                "Daarna moet je de planner bellen, omdat de route dan te kort voor vertrek opnieuw verdeeld moet worden.";
            RuleDescriptionLabel.TextColor = Color.FromArgb("#065F46");

            FormInstructionLabel.Text =
                "Je melding valt binnen de 3-uursregel en kan via de app worden verwerkt.";

            return;
        }

        DeadlineStatusFrame.BackgroundColor = Color.FromArgb("#FEF2F2");
        DeadlineStatusFrame.BorderColor = Color.FromArgb("#FCA5A5");
        DeadlineStatusTitleLabel.Text = "Bel de planner";
        DeadlineStatusTitleLabel.TextColor = Color.FromArgb("#B42318");

        DeadlineBadgeFrame.BackgroundColor = Color.FromArgb("#FEF2F2");
        DeadlineBadgeFrame.BorderColor = Color.FromArgb("#FCA5A5");
        DeadlineBadgeLabel.Text = "Te laat";
        DeadlineBadgeLabel.TextColor = Color.FromArgb("#B42318");

        RuleDescriptionLabel.Text =
            $"De appmelding kon tot {latestAppReportTime:HH:mm}. Je dienst begint binnen {SickReportDeadlineHours} uur, " +
            "dus je moet de planner direct bellen.";
        RuleDescriptionLabel.TextColor = Color.FromArgb("#7F1D1D");
    }

    private void RenderRouteImpact(Ride ride, int totalPackages, int openPackages)
    {
        if (totalPackages == 0)
        {
            ImpactBadgeFrame.BackgroundColor = Color.FromArgb("#F9FAFB");
            ImpactBadgeFrame.BorderColor = Color.FromArgb("#E5E7EB");
            ImpactBadgeLabel.Text = "Laag";
            ImpactBadgeLabel.TextColor = Color.FromArgb("#6B7280");

            RouteImpactDescriptionLabel.Text =
                $"Deze dienst is gekoppeld aan {ride.Region} en {ride.BusName}, maar er zijn geen pakketten aan de route gekoppeld.";

            CallPlannerReasonLabel.Text =
                "Je dienst begint binnen 3 uur. Bel de planner direct, ook als er nog geen pakketten zichtbaar zijn.";

            return;
        }

        if (openPackages == 0)
        {
            ImpactBadgeFrame.BackgroundColor = Color.FromArgb("#ECFDF3");
            ImpactBadgeFrame.BorderColor = Color.FromArgb("#ABEFC6");
            ImpactBadgeLabel.Text = "Laag";
            ImpactBadgeLabel.TextColor = Color.FromArgb("#027A48");

            RouteImpactDescriptionLabel.Text =
                "Alle pakketten op deze route zijn afgerond. De operationele impact is laag, maar de planner moet nog steeds weten dat je niet beschikbaar bent.";

            CallPlannerReasonLabel.Text =
                "Je dienst begint binnen 3 uur. Bel de planner direct, ook als je route al grotendeels of volledig is afgerond.";

            return;
        }

        ImpactBadgeFrame.BackgroundColor = Color.FromArgb("#FFF7ED");
        ImpactBadgeFrame.BorderColor = Color.FromArgb("#FED7AA");
        ImpactBadgeLabel.Text = "Planning";
        ImpactBadgeLabel.TextColor = Color.FromArgb("#C2410C");

        RouteImpactDescriptionLabel.Text =
            $"Deze ziekmelding raakt {openPackages} openstaande pakketten op route {ride.Region}. " +
            $"De planner moet depot {ride.BranchLocation}, bus {ride.BusName} en de resterende stops opnieuw verdelen.";

        CallPlannerReasonLabel.Text =
            $"Je dienst begint binnen 3 uur en er staan nog {openPackages} pakketten open. " +
            "Bel de planner direct zodat de route opnieuw verdeeld kan worden en klanten geen verkeerde bezorginformatie krijgen.";
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        if (_todayRide == null)
        {
            await DisplayAlert(
                "Geen dienst",
                "Er is geen dienst gevonden waarvoor je je kunt ziekmelden.",
                "Oké");

            return;
        }

        if (!CanReportSickInApp(_todayRide))
        {
            RenderPageState();

            await DisplayAlert(
                "Bel de planner",
                "Je dienst begint binnen 3 uur. De appmelding is geblokkeerd; bel de planner direct.",
                "Oké");

            return;
        }

        string? reason = ReasonPicker.SelectedItem?.ToString();
        string description = DescriptionEditor.Text?.Trim() ?? string.Empty;
        string? expectedReturn = ExpectedReturnPicker.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(reason))
        {
            await DisplayAlert(
                "Reden ontbreekt",
                "Kies eerst een reden voor je ziekmelding.",
                "Oké");

            return;
        }

        if (description.Length < MinimumDescriptionLength)
        {
            await DisplayAlert(
                "Toelichting te kort",
                $"Geef een korte maar duidelijke toelichting van minimaal {MinimumDescriptionLength} tekens.",
                "Oké");

            return;
        }

        if (string.IsNullOrWhiteSpace(expectedReturn))
        {
            await DisplayAlert(
                "Terugkeer ontbreekt",
                "Kies een verwachte terugkeer, ook als die nog onbekend is.",
                "Oké");

            return;
        }

        if (!ConfirmationCheckBox.IsChecked)
        {
            await DisplayAlert(
                "Bevestiging nodig",
                "Bevestig dat je melding naar waarheid is ingevuld en dat je bereikbaar blijft.",
                "Oké");

            return;
        }

        int openPackages = GetOpenPackageCount(_todayRide);
        string referenceNumber = CreateReferenceNumber(_todayRide);

        GiveSuccessFeedback();

        await DisplayAlert(
            "Ziekmelding geregistreerd",
            $"Referentie: {referenceNumber}\n\n" +
            $"Dienst: {_todayRide.StartTime:HH:mm} - {_todayRide.EndTime:HH:mm}\n" +
            $"Route: {_todayRide.Region} • {_todayRide.BusName}\n" +
            $"Openstaande pakketten: {openPackages}\n" +
            $"Reden: {reason}\n" +
            $"Verwachte terugkeer: {expectedReturn}",
            "Oké");

        await Navigation.PopAsync();
    }

    private void OnDescriptionChanged(object sender, TextChangedEventArgs e)
    {
        int length = e.NewTextValue?.Length ?? 0;

        DescriptionCounterLabel.Text = $"{length}/{MaximumDescriptionLength}";
        DescriptionCounterLabel.TextColor = length >= MinimumDescriptionLength
            ? Color.FromArgb("#027A48")
            : Color.FromArgb("#6B7280");
    }

    private async void OnCallPlannerClicked(object sender, EventArgs e)
    {
        GiveLightFeedback();

        try
        {
            if (!PhoneDialer.Default.IsSupported)
            {
                await DisplayAlert(
                    "Bellen niet ondersteund",
                    "Dit apparaat ondersteunt automatisch bellen niet. Kopieer het nummer en bel handmatig.",
                    "Oké");

                return;
            }

            PhoneDialer.Default.Open(PlannerPhoneNumber);
        }
        catch
        {
            await DisplayAlert(
                "Bellen mislukt",
                "De planner kan niet automatisch worden gebeld. Kopieer het nummer en bel handmatig.",
                "Oké");
        }
    }

    private async void OnCopyPlannerNumberClicked(object sender, EventArgs e)
    {
        GiveLightFeedback();

        await Clipboard.Default.SetTextAsync(PlannerPhoneNumber);

        await DisplayAlert(
            "Gekopieerd",
            "Het telefoonnummer van de planner is gekopieerd.",
            "Oké");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        GiveLightFeedback();
        await Navigation.PopAsync();
    }

    private static int GetOpenPackageCount(Ride ride)
    {
        int totalPackages = ride.Packages.Count;
        int completedPackages = ride.Packages.Count(package => package.IsCompleted);

        return Math.Max(totalPackages - completedPackages, 0);
    }

    private static bool CanReportSickInApp(Ride ride)
    {
        return DateTime.Now <= GetLatestAppReportTime(ride);
    }

    private static DateTime GetLatestAppReportTime(Ride ride)
    {
        return ride.StartTime.AddHours(-SickReportDeadlineHours);
    }

    private static string CreateReferenceNumber(Ride ride)
    {
        return $"ZR-{ride.RideDate:yyyyMMdd}-{DateTime.Now:HHmm}";
    }

    private static string FormatDutchPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length == 10 && phoneNumber.StartsWith("06"))
        {
            return $"{phoneNumber[..2]} {phoneNumber[2..6]} {phoneNumber[6..]}";
        }

        return phoneNumber;
    }

    private static void GiveLightFeedback()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Niet elk device ondersteunt haptische feedback.
        }
    }

    private static void GiveSuccessFeedback()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(120));
        }
        catch
        {
            // Feedback is ondersteunend. De functionaliteit mag hier niet op falen.
        }
    }
}