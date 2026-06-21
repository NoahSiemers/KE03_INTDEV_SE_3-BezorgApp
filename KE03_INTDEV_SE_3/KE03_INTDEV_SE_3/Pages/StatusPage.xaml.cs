using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using Microsoft.EntityFrameworkCore;

namespace KE03_INTDEV_SE_3.Pages;

public partial class StatusPage : ContentPage
{
    private readonly AppDbContext _db;

    private readonly int _packageId;
    private PackageItem? _package;

    private string _selectedStatus = "";
    private string _selectedFailedReason = "";

    public StatusPage(int packageId)
    {
        InitializeComponent();

        _packageId = packageId;
        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPackageAsync();
    }

    private async Task LoadPackageAsync()
    {
        _package = await _db.Packages
            .FirstOrDefaultAsync(p => p.Id == _packageId);

        if (_package == null)
        {
            await DisplayAlert("Fout", "Pakket niet gevonden.", "Ok");
            await Navigation.PopAsync();
            return;
        }

        PackageInfoLabel.Text = $"{_package.CustomerName} • {_package.Address}";
    }

    private void OnDeliveredTapped(object sender, TappedEventArgs e)
    {
        SelectStatus("Afgeleverd");
    }

    private void OnNeighborTapped(object sender, TappedEventArgs e)
    {
        SelectStatus("Bezorgd bij buren");
    }

    private void OnPickupTapped(object sender, TappedEventArgs e)
    {
        SelectStatus("Afhaalpunt");
    }

    private void OnFailedTapped(object sender, TappedEventArgs e)
    {
        SelectStatus("Niet geleverd");
    }

    private void SelectStatus(string status)
    {
        _selectedStatus = status;

        ResetStatusStyles();

        NeighborInfoFrame.IsVisible = false;
        PickupInfoFrame.IsVisible = false;
        FailedReasonFrame.IsVisible = false;
        OtherReasonEditor.IsVisible = false;

        if (status == "Afgeleverd")
        {
            SetSelectedStyle(DeliveredFrame, DeliveredIcon, "#00C853", "#ECFDF3");
        }
        else if (status == "Bezorgd bij buren")
        {
            SetSelectedStyle(NeighborFrame, NeighborIcon, "#FF7A00", "#FFF7ED");
            NeighborInfoFrame.IsVisible = true;
        }
        else if (status == "Afhaalpunt")
        {
            SetSelectedStyle(PickupFrame, PickupIcon, "#FF7A00", "#FFF7ED");
            PickupInfoFrame.IsVisible = true;
        }
        else if (status == "Niet geleverd")
        {
            SetSelectedStyle(FailedFrame, FailedIcon, "#FF3B4A", "#FEF2F2");
            FailedReasonFrame.IsVisible = true;
        }
    }

    private void ResetStatusStyles()
    {
        SetNormalStyle(DeliveredFrame, DeliveredIcon);
        SetNormalStyle(NeighborFrame, NeighborIcon);
        SetNormalStyle(PickupFrame, PickupIcon);
        SetNormalStyle(FailedFrame, FailedIcon);
    }

    private void SetNormalStyle(Frame frame, Label icon)
    {
        frame.BackgroundColor = Color.FromArgb("#F9FAFB");
        frame.BorderColor = Color.FromArgb("#E5E7EB");
        icon.TextColor = Color.FromArgb("#CBD5E1");
    }

    private void SetSelectedStyle(Frame frame, Label icon, string color, string backgroundColor)
    {
        frame.BackgroundColor = Color.FromArgb(backgroundColor);
        frame.BorderColor = Color.FromArgb(color);
        icon.TextColor = Color.FromArgb(color);
    }

    private void OnReasonCustomerNotHomeTapped(object sender, TappedEventArgs e)
    {
        SelectFailedReason("Klant niet thuis");
    }

    private void OnReasonWrongAddressTapped(object sender, TappedEventArgs e)
    {
        SelectFailedReason("Adres onjuist");
    }

    private void OnReasonDamagedTapped(object sender, TappedEventArgs e)
    {
        SelectFailedReason("Pakket beschadigd");
    }

    private void OnReasonNoAccessTapped(object sender, TappedEventArgs e)
    {
        SelectFailedReason("Geen toegang tot gebouw");
    }

    private void OnReasonOtherTapped(object sender, TappedEventArgs e)
    {
        SelectFailedReason("Anders");
    }

    private void SelectFailedReason(string reason)
    {
        _selectedFailedReason = reason;

        ResetReasonStyles();

        OtherReasonEditor.IsVisible = reason == "Anders";

        SetReasonSelectedStyle(reason);
    }

    private void ResetReasonStyles()
    {
        SetNormalStyle(ReasonCustomerNotHomeFrame, ReasonCustomerNotHomeIcon);
        SetNormalStyle(ReasonWrongAddressFrame, ReasonWrongAddressIcon);
        SetNormalStyle(ReasonDamagedFrame, ReasonDamagedIcon);
        SetNormalStyle(ReasonNoAccessFrame, ReasonNoAccessIcon);
        SetNormalStyle(ReasonOtherFrame, ReasonOtherIcon);
    }

    private void SetReasonSelectedStyle(string reason)
    {
        if (reason == "Klant niet thuis")
        {
            SetSelectedStyle(ReasonCustomerNotHomeFrame, ReasonCustomerNotHomeIcon, "#FF3B4A", "#FEF2F2");
        }
        else if (reason == "Adres onjuist")
        {
            SetSelectedStyle(ReasonWrongAddressFrame, ReasonWrongAddressIcon, "#FF3B4A", "#FEF2F2");
        }
        else if (reason == "Pakket beschadigd")
        {
            SetSelectedStyle(ReasonDamagedFrame, ReasonDamagedIcon, "#FF3B4A", "#FEF2F2");
        }
        else if (reason == "Geen toegang tot gebouw")
        {
            SetSelectedStyle(ReasonNoAccessFrame, ReasonNoAccessIcon, "#FF3B4A", "#FEF2F2");
        }
        else if (reason == "Anders")
        {
            SetSelectedStyle(ReasonOtherFrame, ReasonOtherIcon, "#FF3B4A", "#FEF2F2");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_package == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedStatus))
        {
            await DisplayAlert("Status ontbreekt", "Kies eerst een status.", "Ok");
            return;
        }

        if (_selectedStatus == "Bezorgd bij buren")
        {
            if (string.IsNullOrWhiteSpace(NeighborNameEntry.Text) ||
                string.IsNullOrWhiteSpace(NeighborHouseNumberEntry.Text))
            {
                await DisplayAlert("Gegevens ontbreken", "Vul de naam en het huisnummer van de buur in.", "Ok");
                return;
            }

            _package.NeighborName = NeighborNameEntry.Text.Trim();
            _package.NeighborHouseNumber = NeighborHouseNumberEntry.Text.Trim();
        }

        if (_selectedStatus == "Afhaalpunt")
        {
            if (string.IsNullOrWhiteSpace(PickupPointEntry.Text))
            {
                await DisplayAlert("Afhaalpunt ontbreekt", "Vul het afhaalpunt in.", "Ok");
                return;
            }

            _package.PickupPoint = PickupPointEntry.Text.Trim();
        }

        if (_selectedStatus == "Niet geleverd")
        {
            if (string.IsNullOrWhiteSpace(_selectedFailedReason))
            {
                await DisplayAlert("Reden ontbreekt", "Kies waarom het pakket niet geleverd is.", "Ok");
                return;
            }

            if (_selectedFailedReason == "Anders" &&
                string.IsNullOrWhiteSpace(OtherReasonEditor.Text))
            {
                await DisplayAlert("Reden ontbreekt", "Vul de reden in bij Anders.", "Ok");
                return;
            }

            _package.FailedReason = _selectedFailedReason;
            _package.FailedReasonOtherText = OtherReasonEditor.Text?.Trim();
        }

        _package.DeliveryStatus = _selectedStatus;
        _package.IsCompleted = true;
        _package.CompletedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        VibratePhone();

        await DisplayAlert("Opgeslagen", "De status is opgeslagen.", "Ok");

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