using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;

namespace KE03_INTDEV_SE_3.Pages;

public partial class MyRidesPage : ContentPage
{
    private readonly AppDbContext _db;
    private readonly AppState _appState;

    public MyRidesPage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _appState = ServiceHelper.Services.GetRequiredService<AppState>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadRidesAsync();
    }

    private async Task LoadRidesAsync()
    {
        RidesLayout.Clear();

        TodayDateLabel.Text = DateTime.Today.ToString("dddd dd MMMM yyyy");

        if (_appState.LoggedInDriverId == null)
        {
            DriverNameLabel.Text = "Geen gebruiker ingelogd";
            return;
        }

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.Id == _appState.LoggedInDriverId);

        if (driver == null)
        {
            DriverNameLabel.Text = "Geen gebruiker gevonden";
            return;
        }

        DriverNameLabel.Text = driver.Name;

        var rides = await _db.Rides
            .Include(r => r.Packages)
            .Where(r => r.DriverId == driver.Id && r.RideDate.Date >= DateTime.Today)
            .OrderBy(r => r.RideDate)
            .ToListAsync();

        if (!rides.Any())
        {
            RidesLayout.Add(new Frame
            {
                CornerRadius = 22,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E5E7EB"),
                Padding = 22,
                HasShadow = true,
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Geen ritten gevonden",
                            FontSize = 22,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#111827")
                        },
                        new Label
                        {
                            Text = "Je hebt nog geen toekomstige ritten ingepland.",
                            FontSize = 15,
                            TextColor = Color.FromArgb("#6B7280")
                        }
                    }
                }
            });

            return;
        }

        foreach (var ride in rides)
        {
            RidesLayout.Add(CreateRideCard(ride));
        }
    }

    private View CreateRideCard(Ride ride)
    {
        var titleRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition(),
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };

        var iconFrame = new Frame
        {
            WidthRequest = 48,
            HeightRequest = 48,
            CornerRadius = 16,
            BackgroundColor = Color.FromArgb("#15191C"),
            Padding = 0,
            HasShadow = false,
            Content = new Label
            {
                Text = "\uf3c5",
                FontFamily = "FontAwesome",
                FontSize = 20,
                TextColor = Color.FromArgb("#2D7DF6"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var titleStack = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = ride.Region,
                    FontSize = 21,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#111827")
                },
                new Label
                {
                    Text = ride.BusName,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#6B7280")
                }
            }
        };

        var statusLabel = new Label
        {
            Text = GetRideStatusText(ride),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetRideStatusTextColor(ride),
            BackgroundColor = GetRideStatusBackgroundColor(ride),
            Padding = new Thickness(10, 5),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start
        };

        titleRow.Add(iconFrame, 0, 0);
        titleRow.Add(titleStack, 1, 0);
        titleRow.Add(statusLabel, 2, 0);

        var infoGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(),
                new ColumnDefinition()
            },
            RowDefinitions =
            {
                new RowDefinition(),
                new RowDefinition(),
                new RowDefinition()
            },
            ColumnSpacing = 10,
            RowSpacing = 10,
            Margin = new Thickness(0, 16, 0, 0)
        };

        infoGrid.Add(CreateSmallInfoCard("Pakketten", ride.PackageCount.ToString(), "\uf1b2"), 0, 0);
        //infoGrid.Add(CreateSmallInfoCard("Ingescand", GetScannedText(ride), "\uf058"), 1, 0);
        infoGrid.Add(CreateSmallInfoCard("Datum", ride.RideDate.ToString("dd-MM-yyyy"), "\uf073"), 0, 1);
        infoGrid.Add(CreateSmallInfoCard("Tijd", $"{ride.StartTime:HH:mm} - {ride.EndTime:HH:mm}", "\uf017"), 1, 1);
        infoGrid.Add(CreateSmallInfoCard("Filiaal", ride.BranchLocation, "\uf54e"), 1, 0);
        infoGrid.Add(CreateSmallInfoCard("Regio", ride.Region, "\uf3c5"), 0, 2);

        var openButton = CreateRouteOpenButton(ride);

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 18,
            HasShadow = true,
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    titleRow,
                    infoGrid,
                    openButton
                }
            }
        };
    }

    private View CreateSmallInfoCard(string label, string value, string icon)
    {
        return new Frame
        {
            CornerRadius = 16,
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            BorderColor = Color.FromArgb("#F3F4F6"),
            Padding = 12,
            HasShadow = false,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new HorizontalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            new Label
                            {
                                Text = icon,
                                FontFamily = "FontAwesome",
                                FontSize = 12,
                                TextColor = Color.FromArgb("#9CA3AF")
                            },
                            new Label
                            {
                                Text = label,
                                FontSize = 12,
                                TextColor = Color.FromArgb("#9CA3AF")
                            }
                        }
                    },
                    new Label
                    {
                        Text = value,
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#111827"),
                        LineBreakMode = LineBreakMode.TailTruncation
                    }
                }
            }
        };
    }

    private View CreateRouteOpenButton(Ride ride)
    {
        var buttonContent = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "Route openen",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    VerticalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = "\uf054",
                    FontFamily = "FontAwesome",
					FontSize = 14,
                    TextColor = Colors.White,
					VerticalTextAlignment = TextAlignment.Center
				}
            }
        };

        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#15191C"),
            CornerRadius = 0,
            Padding = 0,
            HeightRequest = 56,
            HasShadow = false,
            Margin = new Thickness(-18, 16, -18, -18),
            Content = buttonContent
        };

        var tapGesture = new TapGestureRecognizer();

        tapGesture.Tapped += async (_, _) =>
        {
            _appState.SelectedRideId = ride.Id;
            await Shell.Current.GoToAsync("//today");
        };

        frame.GestureRecognizers.Add(tapGesture);

        return frame;
	}

    //private string GetScannedText(Ride ride)
    //{
    //    int completed = ride.Packages.Count(p => p.IsCompleted);
    //    int total = ride.Packages.Count;

    //    if (total == 0)
    //    {
    //        total = ride.PackageCount;
    //    }

    //    return $"{completed}/{total}";
    //}



    private string GetRideStatusText(Ride ride)
    {
        if (ride.Packages.Any() && ride.Packages.All(p => p.IsCompleted))
        {
            return "Afgerond";
        }

        if (ride.RideDate.Date == DateTime.Today)
        {
            return "Onderweg";
        }

        return "Open";
    }

    private Color GetRideStatusTextColor(Ride ride)
    {
        string status = GetRideStatusText(ride);

        return status switch
        {
            "Afgerond" => Color.FromArgb("#16A34A"),
            "Onderweg" => Color.FromArgb("#2563EB"),
            _ => Color.FromArgb("#374151")
        };
    }

    private Color GetRideStatusBackgroundColor(Ride ride)
    {
        string status = GetRideStatusText(ride);

        return status switch
        {
            "Afgerond" => Color.FromArgb("#DCFCE7"),
            "Onderweg" => Color.FromArgb("#DBEAFE"),
            _ => Color.FromArgb("#F3F4F6")
        };
    }
}