using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KE03_INTDEV_SE_3.Pages;

public partial class PackageProblemPage : ContentPage
{
    private const int MinimumDescriptionLength = 8;
    private const int MaximumDescriptionLength = 180;

    private readonly AppDbContext _db;
    private readonly int _packageId;
    private readonly string _problemContext;
    private readonly Func<int, string, Task>? _onProblemSaved;

    private PackageItem? _package;
    private string _selectedProblem = "";

    public PackageProblemPage(
        int packageId,
        string problemContext,
        Func<int, string, Task>? onProblemSaved = null)
    {
        InitializeComponent();

        _packageId = packageId;
        _problemContext = problemContext;
        _onProblemSaved = onProblemSaved;

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();

        DescriptionCounterLabel.Text = $"0/{MaximumDescriptionLength}";
        RenderContext();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPackageAsync();
    }

    private async Task LoadPackageAsync()
    {
        _package = await _db.Packages
            .FirstOrDefaultAsync(package => package.Id == _packageId);

        if (_package == null)
        {
            await DisplayAlert("Fout", "Pakket niet gevonden.", "Oké");
            await Navigation.PopAsync();
            return;
        }

        PackageInfoLabel.Text = $"{_package.CustomerName} • {_package.Address}";
    }

    private void RenderContext()
    {
        bool isLoadingProblem = _problemContext == "loading";

        HeaderTitleLabel.Text = isLoadingProblem
            ? "Laadprobleem melden"
            : "Pakketprobleem melden";

        ContextTitleLabel.Text = isLoadingProblem
            ? "Pakket ontbreekt tijdens inladen"
            : "Probleem tijdens de route";

        ContextDescriptionLabel.Text = isLoadingProblem
            ? "Gebruik deze melding als een pakket niet normaal geladen kan worden. De route mag pas starten als het pakket geladen of officieel gemeld is."
            : "Gebruik deze melding als er onderweg iets misgaat met het pakket. Zo voorkom je een verkeerde afleverstatus.";

        LostFrame.IsVisible = !isLoadingProblem;
        StolenFrame.IsVisible = !isLoadingProblem;
    }

    private void OnMissingTapped(object sender, TappedEventArgs e)
    {
        SelectProblem("Pakket ontbreekt");
    }

    private void OnDamagedTapped(object sender, TappedEventArgs e)
    {
        SelectProblem("Pakket beschadigd");
    }

    private void OnWrongRouteTapped(object sender, TappedEventArgs e)
    {
        SelectProblem("Verkeerde bus of route");
    }

    private void OnLostTapped(object sender, TappedEventArgs e)
    {
        SelectProblem("Pakket kwijt onderweg");
    }

    private void OnStolenTapped(object sender, TappedEventArgs e)
    {
        SelectProblem("Mogelijk gestolen");
    }

    private void OnLabelTapped(object sender, TappedEventArgs e)
    {
        SelectProblem("Label onleesbaar");
    }

    private void OnOtherTapped(object sender, TappedEventArgs e)
    {
        SelectProblem("Anders");
    }

    private void SelectProblem(string problem)
    {
        _selectedProblem = problem;

        ResetProblemStyles();

        if (problem == "Pakket ontbreekt")
        {
            SetSelectedStyle(MissingFrame, MissingIcon);
        }
        else if (problem == "Pakket beschadigd")
        {
            SetSelectedStyle(DamagedFrame, DamagedIcon);
        }
        else if (problem == "Verkeerde bus of route")
        {
            SetSelectedStyle(WrongRouteFrame, WrongRouteIcon);
        }
        else if (problem == "Pakket kwijt onderweg")
        {
            SetSelectedStyle(LostFrame, LostIcon);
        }
        else if (problem == "Mogelijk gestolen")
        {
            SetSelectedStyle(StolenFrame, StolenIcon);
        }
        else if (problem == "Label onleesbaar")
        {
            SetSelectedStyle(LabelFrame, LabelIcon);
        }
        else if (problem == "Anders")
        {
            SetSelectedStyle(OtherFrame, OtherIcon);
        }
    }

    private void ResetProblemStyles()
    {
        SetNormalStyle(MissingFrame, MissingIcon);
        SetNormalStyle(DamagedFrame, DamagedIcon);
        SetNormalStyle(WrongRouteFrame, WrongRouteIcon);
        SetNormalStyle(LostFrame, LostIcon);
        SetNormalStyle(StolenFrame, StolenIcon);
        SetNormalStyle(LabelFrame, LabelIcon);
        SetNormalStyle(OtherFrame, OtherIcon);
    }

    private static void SetNormalStyle(Frame frame, Label icon)
    {
        frame.BackgroundColor = Color.FromArgb("#F9FAFB");
        frame.BorderColor = Color.FromArgb("#E5E7EB");
        icon.TextColor = Color.FromArgb("#CBD5E1");
    }

    private static void SetSelectedStyle(Frame frame, Label icon)
    {
        frame.BackgroundColor = Color.FromArgb("#FEF2F2");
        frame.BorderColor = Color.FromArgb("#DC2626");
        icon.TextColor = Color.FromArgb("#DC2626");
    }

    private void OnDescriptionChanged(object sender, TextChangedEventArgs e)
    {
        int length = e.NewTextValue?.Length ?? 0;

        DescriptionCounterLabel.Text = $"{length}/{MaximumDescriptionLength}";
        DescriptionCounterLabel.TextColor = length >= MinimumDescriptionLength
            ? Color.FromArgb("#027A48")
            : Color.FromArgb("#6B7280");
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_package == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedProblem))
        {
            await DisplayAlert("Probleem ontbreekt", "Kies eerst welk probleem je wilt melden.", "Oké");
            return;
        }

        if (!CheckedLoadPointCheckBox.IsChecked ||
            !CheckedLabelCheckBox.IsChecked ||
            !CheckedPlannerCheckBox.IsChecked)
        {
            await DisplayAlert(
                "Controle ontbreekt",
                "Vink eerst alle controles aan voordat je het probleem meldt.",
                "Oké");

            return;
        }

        string description = DescriptionEditor.Text?.Trim() ?? string.Empty;

        if (description.Length < MinimumDescriptionLength)
        {
            await DisplayAlert(
                "Toelichting te kort",
                $"Geef een korte toelichting van minimaal {MinimumDescriptionLength} tekens.",
                "Oké");

            return;
        }

        string incidentText = _problemContext == "loading"
            ? $"Ontbreekt bij laden: {_selectedProblem} — {description}"
            : $"Incident onderweg: {_selectedProblem} — {description}";

        if (_onProblemSaved != null)
        {
            await _onProblemSaved(_packageId, incidentText);
        }

        VibratePhone();

        await DisplayAlert(
            "Probleem gemeld",
            "De melding is geregistreerd. De planner of back-office kan dit opvolgen.",
            "Oké");

        await Navigation.PopAsync();
    }

    private void VibratePhone()
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
        }
        catch
        {
            // Op sommige apparaten/emulators werkt trillen niet.
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}