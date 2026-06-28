namespace KE03_INTDEV_SE_3.Pages;

public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}