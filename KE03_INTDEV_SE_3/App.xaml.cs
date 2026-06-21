using Microsoft.Extensions.DependencyInjection;
using KE03_INTDEV_SE_3.Pages;

namespace KE03_INTDEV_SE_3
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#1F2937"),
                BarTextColor = Colors.White
            };
        }
    }
}