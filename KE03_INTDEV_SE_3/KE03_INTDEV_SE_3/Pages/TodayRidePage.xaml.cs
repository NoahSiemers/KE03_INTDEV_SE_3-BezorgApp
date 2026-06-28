using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;

namespace KE03_INTDEV_SE_3.Pages;

public partial class TodayRidePage : ContentPage
{
    private readonly AppDbContext _db;
    private readonly AppState _appState;

    private Ride? _currentRide;
    private bool _timerRunning;
    private bool _timeWarningShown;
    private TimeSpan _remainingTime;
    private int _totalPackages;

    public TodayRidePage()
    {
        InitializeComponent();

        _db = ServiceHelper.Services.GetRequiredService<AppDbContext>();
        _appState = ServiceHelper.Services.GetRequiredService<AppState>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadRideAsync();
    }

    private async Task LoadRideAsync()
    {
        int? rideId = _appState.SelectedRideId;

        if (rideId == null && _appState.LoggedInDriverId != null)
        {
            var todayRide = await _db.Rides
                .FirstOrDefaultAsync(r =>
                    r.DriverId == _appState.LoggedInDriverId &&
                    r.RideDate.Date == DateTime.Today);

            rideId = todayRide?.Id;
            _appState.SelectedRideId = rideId;
        }

        if (rideId == null)
        {
            ShowNoRide();
            return;
        }

        _currentRide = await _db.Rides
            .Include(r => r.Packages)
            .FirstOrDefaultAsync(r => r.Id == rideId);

        if (_currentRide == null)
        {
            ShowNoRide();
            return;
        }

        _totalPackages = _currentRide.Packages.Count;

        DateLabel.Text = _currentRide.RideDate.ToString("dddd dd MMMM yyyy");

        BranchLabel.Text = _currentRide.BranchLocation;
        TimeLabel.Text = $"{_currentRide.RideDate:dd-MM-yyyy} • {_currentRide.StartTime:HH:mm} - {_currentRide.EndTime:HH:mm}";
        BusLabel.Text = _currentRide.BusName;
        RegionLabel.Text = _currentRide.Region;

        int remainingPackages = _currentRide.Packages.Count(p => !p.IsCompleted);
        RemainingPackagesLabel.Text = remainingPackages.ToString();

        StartButton.IsVisible = true;
        RideInfoFrame.IsVisible = true;

        RenderPackages(false);
    }

    private void ShowNoRide()
    {
        DateLabel.Text = DateTime.Today.ToString("dddd dd MMMM yyyy");

        StartButton.IsVisible = false;
        RideInfoFrame.IsVisible = false;

        PackagesLayout.Clear();

        PackagesLayout.Add(new Frame
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
                        Text = "Vandaag geen rit",
                        FontSize = 23,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#111827")
                    },
                    new Label
                    {
                        Text = "Je hebt vandaag geen bezorgroute ingepland.",
                        FontSize = 15,
                        TextColor = Color.FromArgb("#6B7280")
                    }
                }
            }
        });
    }

    private async void RenderPackages(bool showMultipleAddressMessage)
    {
        PackagesLayout.Clear();

        if (_currentRide == null)
        {
            return;
        }

        var packagesToLoad = _currentRide.Packages
            .Where(p =>
                !p.IsLoadedInBus &&
                p.ActionType.ToLower() != "ophalen")
            .OrderBy(p => p.SequenceNumber)
            .ToList();

        if (packagesToLoad.Any())
        {
            SectionTitleLabel.Text = "Bus inladen";

            var nextPackageToLoad = packagesToLoad.First();

            PackagesLayout.Add(CreateLoadPackageCard(nextPackageToLoad));

            return;
        }

        SectionTitleLabel.Text = "Volgende stop";

        var nextPackage = _currentRide.Packages
            .Where(p => !p.IsCompleted)
            .OrderBy(p => p.SequenceNumber)
            .FirstOrDefault();

        if (nextPackage == null)
        {
            PackagesLayout.Add(new Frame
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
                        Text = "Route afgerond",
                        FontSize = 23,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#16A34A")
                    },
                    new Label
                    {
                        Text = "Alle pakketten van deze route zijn afgerond.",
                        FontSize = 15,
                        TextColor = Color.FromArgb("#6B7280")
                    }
                }
                }
            });

            return;
        }

        var packagesForSameAddress = _currentRide.Packages
            .Where(p => !p.IsCompleted && p.Address == nextPackage.Address)
            .OrderBy(p => p.SequenceNumber)
            .ToList();

        if (showMultipleAddressMessage && packagesForSameAddress.Count > 1)
        {
            await DisplayAlert(
                "Meerdere pakketten",
                "Er zijn meerdere pakketten op dit adres.",
                "Ok");
        }

        foreach (var package in packagesForSameAddress)
        {
            PackagesLayout.Add(CreatePackageCard(package));
        }
    }

    private View CreateLoadPackageCard(PackageItem package)
    {
        var nameLabel = new Label
        {
            Text = package.CustomerName,
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827")
        };

        var addressLabel = new Label
        {
            Text = package.Address,
            FontSize = 15,
            TextColor = Color.FromArgb("#6B7280")
        };

        var detailsLayout = new HorizontalStackLayout
        {
            Spacing = 18,
            Margin = new Thickness(0, 22, 0, 0),
            Children =
        {
            new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = "\uf1b2",
                        FontFamily = "FontAwesome",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#9CA3AF")
                    },
                    new Label
                    {
                        Text = package.Size,
                        FontSize = 14,
                        TextColor = Color.FromArgb("#6B7280")
                    }
                }
            },
            new HorizontalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = "\uf5cd",
                        FontFamily = "FontAwesome",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#9CA3AF")
                    },
                    new Label
                    {
                        Text = $"{package.WeightKg} kg",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#6B7280")
                    }
                }
            }
        }
        };

        var infoButton = CreateActionButton(
            "\uf05a",
            "Info",
            "#6B7280",
            async () =>
            {
                await DisplayAlert(
                    "Pakket info",
                    $"Naam: {package.CustomerName}\nAdres: {package.Address}\nType: {package.ActionType}\nGrootte: {package.Size}\nGewicht: {package.WeightKg} kg",
                    "Ok");
            });

        var scanButton = CreateActionButton(
            "\uf030",
            "Inscannen",
            "#2D7DF6",
            async () =>
            {
                await LoadPackageIntoBusAsync(package.Id);
            });

        var buttonGrid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition(),
            new ColumnDefinition()
        },
            ColumnSpacing = 0,
            Margin = new Thickness(-18, 18, -18, -18),
            BackgroundColor = Color.FromArgb("#F3F4F6")
        };

        buttonGrid.Add(infoButton, 0, 0);
        buttonGrid.Add(scanButton, 1, 0);

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 18,
            HasShadow = true,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
            {
                nameLabel,
                addressLabel,
                detailsLayout,
                buttonGrid
            }
            }
        };
    }

    private async Task LoadPackageIntoBusAsync(int packageId)
    {
        var package = await _db.Packages.FindAsync(packageId);

        if (package == null)
        {
            return;
        }

        package.IsLoadedInBus = true;

        await _db.SaveChangesAsync();

        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(150));
        }
        catch
        {
            // Op Windows/emulator kan trillen soms niet.
        }

        await LoadRideAgainAfterChangeAsync();
    }

    private View CreatePackageCard(PackageItem package)
    {
        var statusLabel = new Label
        {
            Text = package.ActionType,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = package.ActionType.ToLower() == "bezorgen"
                ? Color.FromArgb("#2563EB")
                : Color.FromArgb("#EA580C"),
            BackgroundColor = package.ActionType.ToLower() == "bezorgen"
                ? Color.FromArgb("#DBEAFE")
                : Color.FromArgb("#FFEDD5"),
            Padding = new Thickness(9, 4),
            HorizontalOptions = LayoutOptions.Start
        };

        var nameLabel = new Label
        {
            Text = package.CustomerName,
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827")
        };

        var addressLabel = new Label
        {
            Text = package.Address,
            FontSize = 15,
            TextColor = Color.FromArgb("#6B7280")
        };

        var detailsLayout = new HorizontalStackLayout
        {
            Spacing = 18,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        new Label
                        {
                            Text = "\uf1b2",
                            FontFamily = "FontAwesome",
                            FontSize = 14,
                            TextColor = Color.FromArgb("#9CA3AF")
                        },
                        new Label
                        {
                            Text = package.Size,
                            FontSize = 14,
                            TextColor = Color.FromArgb("#6B7280")
                        }
                    }
                },
                new HorizontalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        new Label
                        {
                            Text = "\uf5cd",
                            FontFamily = "FontAwesome",
                            FontSize = 14,
                            TextColor = Color.FromArgb("#9CA3AF")
                        },
                        new Label
                        {
                            Text = $"{package.WeightKg} kg",
                            FontSize = 14,
                            TextColor = Color.FromArgb("#6B7280")
                        }
                    }
                }
            }
        };

        var navigateButton = CreateActionButton(
            "\uf124",
            "Navigeren",
            "#2D7DF6",
            async () =>
            {
                await DisplayAlert("Navigeren", "Navigatie wordt later toegevoegd.", "Ok");
            });

        var infoButton = CreateActionButton(
            "\uf05a",
            "Info",
            "#6B7280",
            async () =>
            {
                await DisplayAlert(
                    "Pakket info",
                    $"Naam: {package.CustomerName}\nAdres: {package.Address}\nType: {package.ActionType}\nGrootte: {package.Size}\nGewicht: {package.WeightKg} kg",
                    "Ok");
            });

        var completeButton = CreateActionButton(
            "\uf058",
            "Afronden",
            "#16A34A",
            async () =>
            {
                await Navigation.PushAsync(new StatusPage(package.Id));
            });

        var buttonGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(),
                new ColumnDefinition(),
                new ColumnDefinition()
            },
            ColumnSpacing = 0,
            Margin = new Thickness(-18, 12, -18, -18),
            BackgroundColor = Color.FromArgb("#F3F4F6")
        };

        buttonGrid.Add(navigateButton, 0, 0);
        buttonGrid.Add(infoButton, 1, 0);
        buttonGrid.Add(completeButton, 2, 0);

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 18,
            HasShadow = true,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    statusLabel,
                    nameLabel,
                    addressLabel,
                    detailsLayout,
                    buttonGrid
                }
            }
        };
    }

    private View CreateActionButton(string icon, string text, string color, Func<Task> action)
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 3,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = icon,
                    FontFamily = "FontAwesome",
                    FontSize = 20,
                    TextColor = Color.FromArgb(color),
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = text,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb(color),
                    HorizontalOptions = LayoutOptions.Center
                }
            }
        };

        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            CornerRadius = 0,
            Padding = 8,
            HasShadow = false,
            HeightRequest = 74,
            Content = layout
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (_, _) => await action();

        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    private async Task CompletePackageAsync(int packageId)
    {
        var package = await _db.Packages.FindAsync(packageId);

        if (package == null)
        {
            return;
        }

        package.IsCompleted = true;
        await _db.SaveChangesAsync();

        await LoadRideAgainAfterChangeAsync();
    }

    private async Task LoadRideAgainAfterChangeAsync()
    {
        if (_currentRide == null)
        {
            return;
        }

        _currentRide = await _db.Rides
            .Include(r => r.Packages)
            .FirstOrDefaultAsync(r => r.Id == _currentRide.Id);

        if (_currentRide == null)
        {
            return;
        }

        int remainingPackages = _currentRide.Packages.Count(p => !p.IsCompleted);
        RemainingPackagesLabel.Text = remainingPackages.ToString();

        RenderPackages(true);
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (_currentRide == null)
        {
            return;
        }

        if (_timerRunning)
        {
            await DisplayAlert("Route gestart", "De timer loopt al.", "Ok");
            return;
        }

        _timerRunning = true;
        _timeWarningShown = false;

        _remainingTime = _currentRide.EndTime - _currentRide.StartTime;

        if (_remainingTime.TotalSeconds <= 0)
        {
            _remainingTime = TimeSpan.FromHours(2);
        }

        StartButton.BackgroundColor = Color.FromArgb("#DC2626");
        StartButton.Text = FormatTime(_remainingTime);

        Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

            StartButton.Text = FormatTime(_remainingTime);

            if (_remainingTime.TotalSeconds <= 0 && !_timeWarningShown)
            {
                _timeWarningShown = true;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert(
                        "Tijd voorbij",
                        "Je bent over de tijd heen. De timer loopt nu in de min.",
                        "Ok");
                });
            }

            return _timerRunning;
        });
    }

    private string FormatTime(TimeSpan time)
    {
        string sign = time.TotalSeconds < 0 ? "-" : "";
        time = time.Duration();

        int totalHours = (int)time.TotalHours;

        return $"{sign}{totalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }
}