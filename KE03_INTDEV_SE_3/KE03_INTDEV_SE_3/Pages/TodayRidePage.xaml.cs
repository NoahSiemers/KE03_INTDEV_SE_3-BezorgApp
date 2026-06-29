using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Models;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace KE03_INTDEV_SE_3.Pages;

public partial class TodayRidePage : ContentPage
{
    private readonly AppDbContext _db;
    private readonly AppState _appState;

    private Ride? _currentRide;
    private PackageItem? _activeScannedPackage;

    private readonly HashSet<int> _loadedPackageIds = new();
    private readonly Dictionary<int, string> _packageIncidents = new();

    private bool _shiftConfirmed;
    private bool _busConfirmed;
    private bool _routeStarted;
    private bool _timerRunning;
    private bool _timeWarningShown;
    private TimeSpan _remainingTime;
    private int? _activeRideId;

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
        RenderFlow();
    }

    private async Task LoadRideAsync()
    {
        int? rideId = _appState.SelectedRideId;

        if (rideId == null && _appState.LoggedInDriverId != null)
        {
            Ride? todayRide = await _db.Rides
                .FirstOrDefaultAsync(ride =>
                    ride.DriverId == _appState.LoggedInDriverId &&
                    ride.RideDate.Date == DateTime.Today);

            rideId = todayRide?.Id;
            _appState.SelectedRideId = rideId;
        }

        if (rideId == null)
        {
            _currentRide = null;
            ShowNoRide();
            return;
        }

        _currentRide = await _db.Rides
            .Include(ride => ride.Packages)
            .FirstOrDefaultAsync(ride => ride.Id == rideId);

        if (_currentRide == null)
        {
            ShowNoRide();
            return;
        }

        if (_activeRideId != _currentRide.Id)
        {
            ResetLocalWorkflowState();
            _activeRideId = _currentRide.Id;
        }

        DateLabel.Text = _currentRide.RideDate.ToString("dddd dd MMMM yyyy");

        BranchLabel.Text = _currentRide.BranchLocation;
        TimeLabel.Text = $"{_currentRide.RideDate:dd-MM-yyyy} • {_currentRide.StartTime:HH:mm} - {_currentRide.EndTime:HH:mm}";
        BusLabel.Text = _currentRide.BusName;
        RegionLabel.Text = _currentRide.Region;

        UpdateRemainingPackagesLabel();
        RideInfoFrame.IsVisible = true;
    }

    private void ResetLocalWorkflowState()
    {
        _activeScannedPackage = null;
        _loadedPackageIds.Clear();
        _packageIncidents.Clear();

        _shiftConfirmed = false;
        _busConfirmed = false;
        _routeStarted = false;
        _timerRunning = false;
        _timeWarningShown = false;

        RouteTimerLabel.Text = "Nog niet gestart";
    }

    private void ShowNoRide()
    {
        DateLabel.Text = DateTime.Today.ToString("dddd dd MMMM yyyy");

        RideInfoFrame.IsVisible = false;
        RemainingPackagesLabel.Text = "0";

        FlowStepBadgeLabel.Text = "Geen rit";
        CurrentStepTitleLabel.Text = "Vandaag geen rit";
        CurrentStepDescriptionLabel.Text = "Er staat vandaag geen bezorgroute voor je ingepland.";

        StepIndicatorLayout.Clear();
        WorkPanelLayout.Clear();

        WorkPanelLayout.Add(CreateCard(
            "Vandaag geen rit",
            "Je hebt vandaag geen bezorgroute ingepland. Controleer eventueel Mijn ritten voor andere geplande diensten.",
            "#111827",
            "#6B7280"));
    }

    private void RenderFlow()
    {
        WorkPanelLayout.Clear();
        StepIndicatorLayout.Clear();

        if (_currentRide == null)
        {
            ShowNoRide();
            return;
        }

        UpdateRemainingPackagesLabel();

        WorkFlowStep step = DetermineCurrentStep();

        RenderStepHeader(step);
        RenderStepIndicators(step);

        switch (step)
        {
            case WorkFlowStep.ConfirmShift:
                RenderConfirmShiftStep();
                break;

            case WorkFlowStep.ConfirmBus:
                RenderConfirmBusStep();
                break;

            case WorkFlowStep.LoadPackages:
                RenderLoadPackagesStep();
                break;

            case WorkFlowStep.StartRoute:
                RenderStartRouteStep();
                break;

            case WorkFlowStep.DeliverStops:
                RenderDeliverStopsStep();
                break;

            case WorkFlowStep.CompleteRoute:
                RenderCompleteRouteStep();
                break;
        }
    }

    private WorkFlowStep DetermineCurrentStep()
    {
        if (!_shiftConfirmed)
        {
            return WorkFlowStep.ConfirmShift;
        }

        if (!_busConfirmed)
        {
            return WorkFlowStep.ConfirmBus;
        }

        if (!AllPackagesLoadedOrReported())
        {
            return WorkFlowStep.LoadPackages;
        }

        if (!_routeStarted)
        {
            return WorkFlowStep.StartRoute;
        }

        if (!AllStopsHandled())
        {
            return WorkFlowStep.DeliverStops;
        }

        return WorkFlowStep.CompleteRoute;
    }

    private void RenderStepHeader(WorkFlowStep step)
    {
        FlowStepBadgeLabel.Text = $"Stap {(int)step}/6";

        switch (step)
        {
            case WorkFlowStep.ConfirmShift:
                CurrentStepTitleLabel.Text = "Dienst controleren";
                CurrentStepDescriptionLabel.Text = "Controleer of je de juiste dienst, depot, bus en regio voor je hebt.";
                break;

            case WorkFlowStep.ConfirmBus:
                CurrentStepTitleLabel.Text = "Bus controleren";
                CurrentStepDescriptionLabel.Text = "Controleer de busindeling voordat je pakketten gaat laden.";
                break;

            case WorkFlowStep.LoadPackages:
                CurrentStepTitleLabel.Text = "Pakketten inladen";
                CurrentStepDescriptionLabel.Text = "Scan elk pakket en leg het in de juiste buszone. Meld ontbrekende pakketten direct.";
                break;

            case WorkFlowStep.StartRoute:
                CurrentStepTitleLabel.Text = "Route starten";
                CurrentStepDescriptionLabel.Text = "Alle pakketten zijn geladen of gemeld. Je kunt nu veilig vertrekken.";
                break;

            case WorkFlowStep.DeliverStops:
                CurrentStepTitleLabel.Text = "Stops bezorgen";
                CurrentStepDescriptionLabel.Text = "Werk de route stop voor stop af en meld problemen direct.";
                break;

            case WorkFlowStep.CompleteRoute:
                CurrentStepTitleLabel.Text = "Route afronden";
                CurrentStepDescriptionLabel.Text = "Alle stops zijn afgehandeld of als incident gemeld.";
                break;
        }
    }

    private void RenderStepIndicators(WorkFlowStep currentStep)
    {
        AddStepIndicator("1", "Dienst", _shiftConfirmed, currentStep == WorkFlowStep.ConfirmShift);
        AddStepIndicator("2", "Bus", _busConfirmed, currentStep == WorkFlowStep.ConfirmBus);
        AddStepIndicator("3", "Laden", AllPackagesLoadedOrReported(), currentStep == WorkFlowStep.LoadPackages);
        AddStepIndicator("4", "Route", _routeStarted, currentStep == WorkFlowStep.StartRoute);
        AddStepIndicator("5", "Bezorgen", AllStopsHandled(), currentStep == WorkFlowStep.DeliverStops);
        AddStepIndicator("6", "Afronden", currentStep == WorkFlowStep.CompleteRoute, currentStep == WorkFlowStep.CompleteRoute);
    }

    private void AddStepIndicator(string number, string title, bool completed, bool active)
    {
        string icon = completed ? "\uf00c" : number;
        string backgroundColor = completed ? "#ECFDF3" : active ? "#EFF6FF" : "#F9FAFB";
        string borderColor = completed ? "#ABEFC6" : active ? "#BFDBFE" : "#E5E7EB";
        string textColor = completed ? "#027A48" : active ? "#1D4ED8" : "#6B7280";

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };

        var iconLabel = new Label
        {
            Text = icon,
            FontFamily = completed ? "FontAwesome" : null,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb(textColor),
            WidthRequest = 24,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var titleLabel = new Label
        {
            Text = title,
            FontSize = 14,
            FontAttributes = active ? FontAttributes.Bold : FontAttributes.None,
            TextColor = Color.FromArgb(textColor),
            VerticalTextAlignment = TextAlignment.Center
        };

        grid.Add(iconLabel, 0, 0);
        grid.Add(titleLabel, 1, 0);

        StepIndicatorLayout.Add(new Frame
        {
            CornerRadius = 16,
            BackgroundColor = Color.FromArgb(backgroundColor),
            BorderColor = Color.FromArgb(borderColor),
            Padding = new Thickness(12, 9),
            HasShadow = false,
            Content = grid
        });
    }

    private void RenderConfirmShiftStep()
    {
        if (_currentRide == null) return;

        WorkPanelLayout.Add(CreateCard(
            "Dienst vandaag",
            $"Depot: {_currentRide.BranchLocation}\n" +
            $"Diensttijd: {_currentRide.StartTime:HH:mm} - {_currentRide.EndTime:HH:mm}\n" +
            $"Bus: {_currentRide.BusName}\n" +
            $"Regio: {_currentRide.Region}\n" +
            $"Aantal pakketten: {_currentRide.Packages.Count}",
            "#111827",
            "#374151"));

        WorkPanelLayout.Add(CreatePrimaryButton(
            "Dienstgegevens kloppen",
            async () =>
            {
                _shiftConfirmed = true;
                await DisplayAlert("Dienst gecontroleerd", "Je hebt bevestigd dat deze dienst klopt.", "Oké");
                RenderFlow();
            }));
    }

    private void RenderConfirmBusStep()
    {
        WorkPanelLayout.Add(CreateCard(
            "Busindeling",
            "De bus is verdeeld in 4 laad zones. Controleer dit vóórdat je pakketten gaat laden.",
            "#111827",
            "#6B7280"));

        WorkPanelLayout.Add(CreateBusZoneMap());

        WorkPanelLayout.Add(CreatePrimaryButton(
            "Bus gecontroleerd",
            async () =>
            {
                _busConfirmed = true;
                await DisplayAlert("Bus gecontroleerd", "Je kunt nu pakketten scannen en inladen.", "Oké");
                RenderFlow();
            }));
    }

    private void RenderLoadPackagesStep()
    {
        if (_currentRide == null) return;

        WorkPanelLayout.Add(CreateLoadingSummaryCard());
        WorkPanelLayout.Add(CreateBusZoneProgressCard());

        if (_activeScannedPackage == null)
        {
            PackageItem? nextPackage = GetNextPackageToLoad();

            if (nextPackage == null)
            {
                WorkPanelLayout.Add(CreateCard(
                    "Laden afgerond",
                    "Alle pakketten zijn geladen of als incident gemeld. De route kan worden gestart.",
                    "#027A48",
                    "#065F46"));

                return;
            }

            PackageItem packageToLoad = nextPackage;

            WorkPanelLayout.Add(CreateCard(
                "Volgend pakket",
                $"Pak het volgende pakket en scan het label.\n\n" +
                $"Verwacht pakket: {GetPackageCode(packageToLoad)}\n" +
                $"Klant: {packageToLoad.CustomerName}",
                "#111827",
                "#374151"));

            WorkPanelLayout.Add(CreatePrimaryButton(
                "Scan volgend pakket",
                async () =>
                {
                    _activeScannedPackage = packageToLoad;
                    string zone = GetLoadZone(packageToLoad);

                    await DisplayAlert(
                        "Pakket gescand",
                        $"{GetPackageCode(packageToLoad)}\n\n" +
                        $"Plaats in: Zone {zone} — {GetZoneDescription(zone)}",
                        "Oké");

                    RenderFlow();
                }));

            WorkPanelLayout.Add(CreateWarningButton(
                "Pakket ontbreekt / hulp nodig",
                async () =>
                {
                    await ReportLoadingProblemAsync(packageToLoad);
                }));
        }
        else
        {
            PackageItem scannedPackage = _activeScannedPackage;
            string zone = GetLoadZone(scannedPackage);

            WorkPanelLayout.Add(CreateScannedPackageCard(scannedPackage, zone));

            WorkPanelLayout.Add(CreatePrimaryButton(
                $"Bevestig geplaatst in Zone {zone}",
                async () =>
                {
                    _loadedPackageIds.Add(scannedPackage.Id);

                    await DisplayAlert(
                        "Pakket geladen",
                        $"{GetPackageCode(scannedPackage)} is geplaatst in Zone {zone}.",
                        "Oké");

                    _activeScannedPackage = null;
                    RenderFlow();
                }));

            WorkPanelLayout.Add(CreateWarningButton(
                "Pakket toch niet gevonden / beschadigd",
                async () =>
                {
                    await ReportLoadingProblemAsync(scannedPackage);
                }));
        }

        WorkPanelLayout.Add(CreatePackageChecklistCard());
    }

    private void RenderStartRouteStep()
    {
        if (_currentRide == null) return;

        int loadedCount = _loadedPackageIds.Count;
        int incidentCount = _packageIncidents.Count;

        WorkPanelLayout.Add(CreateCard(
            "Route klaar om te starten",
            $"Geladen pakketten: {loadedCount}\n" +
            $"Gemelde incidenten: {incidentCount}\n\n" +
            "Je mag pas vertrekken omdat elk pakket geladen is of officieel als probleem is gemeld.",
            "#027A48",
            "#065F46"));

        WorkPanelLayout.Add(CreatePrimaryButton(
            "Route starten",
            async () =>
            {
                _routeStarted = true;
                StartRouteTimer();

                await DisplayAlert(
                    "Route gestart",
                    "Je route is gestart. Werk de stops één voor één af.",
                    "Oké");

                RenderFlow();
            }));
    }

    private void RenderDeliverStopsStep()
    {
        if (_currentRide == null) return;

        PackageItem? nextPackage = GetNextPackageToDeliver();

        if (nextPackage == null)
        {
            RenderCompleteRouteStep();
            return;
        }

        List<PackageItem> packagesForSameAddress = _currentRide.Packages
            .Where(package =>
                !package.IsCompleted &&
                !_packageIncidents.ContainsKey(package.Id) &&
                package.Address == nextPackage.Address)
            .OrderBy(package => package.SequenceNumber)
            .ToList();

        WorkPanelLayout.Add(CreateCard(
            "Volgende stop",
            $"Adres: {nextPackage.Address}\n" +
            $"Aantal pakketten op dit adres: {packagesForSameAddress.Count}",
            "#111827",
            "#374151"));

        foreach (PackageItem package in packagesForSameAddress)
        {
            WorkPanelLayout.Add(CreateDeliveryPackageCard(package));
        }
    }

    private void RenderCompleteRouteStep()
    {
        if (_currentRide == null) return;

        int totalPackages = _currentRide.Packages.Count;
        int completedPackages = _currentRide.Packages.Count(package => package.IsCompleted);
        int incidentPackages = _packageIncidents.Count;
        int openPackages = GetOpenPackageCount();

        WorkPanelLayout.Add(CreateCard(
            "Route afgerond",
            $"Totaal pakketten: {totalPackages}\n" +
            $"Afgerond via status: {completedPackages}\n" +
            $"Incidenten gemeld: {incidentPackages}\n" +
            $"Nog open: {openPackages}",
            "#027A48",
            "#065F46"));

        if (incidentPackages > 0)
        {
            WorkPanelLayout.Add(CreateIncidentSummaryCard());
        }

        WorkPanelLayout.Add(CreatePrimaryButton(
            "Dienst afsluiten",
            async () =>
            {
                bool confirm = await DisplayAlert(
                    "Dienst afsluiten",
                    "Wil je deze dienst afsluiten en Planning vandaag opnieuw klaarzetten vanaf stap 1?",
                    "Ja, afsluiten",
                    "Terug");

                if (!confirm)
                {
                    return;
                }

                await ResetPlanningTodayAsync();
            }));
    }

    private async Task ResetPlanningTodayAsync()
    {
        if (_currentRide == null)
        {
            return;
        }

        int rideId = _currentRide.Id;

        _timerRunning = false;
        RouteTimerLabel.Text = "Nog niet gestart";

        foreach (PackageItem package in _currentRide.Packages)
        {
            package.IsCompleted = false;
        }

        await _db.SaveChangesAsync();

        ResetLocalWorkflowState();

        _currentRide = await _db.Rides
            .Include(ride => ride.Packages)
            .FirstOrDefaultAsync(ride => ride.Id == rideId);

        _activeRideId = rideId;

        UpdateRemainingPackagesLabel();

        await DisplayAlert(
            "Dienst afgesloten",
            "De dienst is afgesloten. Planning vandaag staat weer klaar vanaf stap 1.",
            "Oké");

        RenderFlow();
    }

    private View CreateLoadingSummaryCard()
    {
        if (_currentRide == null) return new VerticalStackLayout();

        int totalPackages = _currentRide.Packages.Count;
        int loadedPackages = _loadedPackageIds.Count;
        int incidentPackages = _packageIncidents.Count;
        int openPackages = totalPackages - loadedPackages - incidentPackages;

        return CreateCard(
            "Laadstatus",
            $"{loadedPackages} van {totalPackages} pakketten geladen\n" +
            $"{incidentPackages} incident(en) gemeld\n" +
            $"{Math.Max(openPackages, 0)} pakket(ten) nog te laden",
            "#111827",
            "#374151");
    }

    private View CreateBusZoneMap()
    {
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(),
                new RowDefinition()
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(),
                new ColumnDefinition()
            },
            RowSpacing = 10,
            ColumnSpacing = 10
        };

        grid.Add(CreateZoneBox("Zone C", "Links voor"), 0, 0);
        grid.Add(CreateZoneBox("Zone D", "Rechts voor"), 1, 0);
        grid.Add(CreateZoneBox("Zone A", "Links achter"), 0, 1);
        grid.Add(CreateZoneBox("Zone B", "Rechts achter"), 1, 1);

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 16,
            HasShadow = true,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "Laadplan bus",
                        FontSize = 21,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#111827")
                    },
                    new Label
                    {
                        Text = "Leg pakketten in de zone die de app na het scannen aangeeft.",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#6B7280")
                    },
                    grid
                }
            }
        };
    }

    private View CreateBusZoneProgressCard()
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 12
        };

        layout.Add(new Label
        {
            Text = "Voortgang per zone",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827")
        });

        layout.Add(CreateZoneProgressRow("A", "Links achter"));
        layout.Add(CreateZoneProgressRow("B", "Rechts achter"));
        layout.Add(CreateZoneProgressRow("C", "Links voor"));
        layout.Add(CreateZoneProgressRow("D", "Rechts voor"));

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 16,
            HasShadow = true,
            Content = layout
        };
    }

    private View CreateZoneProgressRow(string zone, string description)
    {
        if (_currentRide == null) return new VerticalStackLayout();

        List<PackageItem> zonePackages = _currentRide.Packages
            .Where(package => GetLoadZone(package) == zone)
            .ToList();

        int total = zonePackages.Count;
        int loaded = zonePackages.Count(package => _loadedPackageIds.Contains(package.Id));
        int incident = zonePackages.Count(package => _packageIncidents.ContainsKey(package.Id));
        int handled = loaded + incident;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        var textLayout = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = $"Zone {zone} — {description}",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#111827")
                },
                new Label
                {
                    Text = $"{loaded}/{total} geladen • {incident} incident",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#6B7280")
                }
            }
        };

        var progressLabel = new Label
        {
            Text = total > 0 && handled == total ? "\uf00c" : $"{handled}/{total}",
            FontFamily = total > 0 && handled == total ? "FontAwesome" : null,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = total > 0 && handled == total
                ? Color.FromArgb("#027A48")
                : Color.FromArgb("#C2410C"),
            VerticalTextAlignment = TextAlignment.Center
        };

        grid.Add(textLayout, 0, 0);
        grid.Add(progressLabel, 1, 0);

        return new Frame
        {
            CornerRadius = 16,
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 12,
            HasShadow = false,
            Content = grid
        };
    }

    private View CreateZoneBox(string title, string description)
    {
        return new Frame
        {
            CornerRadius = 18,
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 14,
            HasShadow = false,
            Content = new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#111827"),
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = description,
                        FontSize = 13,
                        TextColor = Color.FromArgb("#6B7280"),
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
    }

    private View CreateScannedPackageCard(PackageItem package, string zone)
    {
        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#BFDBFE"),
            Padding = 18,
            HasShadow = true,
            Content = new VerticalStackLayout
            {
                Spacing = 9,
                Children =
                {
                    new Label
                    {
                        Text = "Pakket gescand",
                        FontSize = 21,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#1D4ED8")
                    },
                    new Label
                    {
                        Text = GetPackageCode(package),
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#111827")
                    },
                    new Label
                    {
                        Text = $"Klant: {package.CustomerName}",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#374151")
                    },
                    new Label
                    {
                        Text = $"Adres: {package.Address}",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#374151")
                    },
                    new Label
                    {
                        Text = $"Plaats in: Zone {zone} — {GetZoneDescription(zone)}",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#C2410C")
                    }
                }
            }
        };
    }

    private View CreatePackageChecklistCard()
    {
        if (_currentRide == null) return new VerticalStackLayout();

        var layout = new VerticalStackLayout
        {
            Spacing = 12
        };

        layout.Add(new Label
        {
            Text = "Laadchecklist",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#111827")
        });

        foreach (PackageItem package in GetOrderedPackages())
        {
            layout.Add(CreatePackageChecklistRow(package));
        }

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 16,
            HasShadow = true,
            Content = layout
        };
    }

    private View CreatePackageChecklistRow(PackageItem package)
    {
        bool isLoaded = _loadedPackageIds.Contains(package.Id);
        bool hasIncident = _packageIncidents.ContainsKey(package.Id);
        string zone = GetLoadZone(package);

        string icon = hasIncident ? "\uf071" : isLoaded ? "\uf00c" : "\uf111";
        string iconColor = hasIncident ? "#DC2626" : isLoaded ? "#027A48" : "#9CA3AF";
        string statusText = hasIncident ? _packageIncidents[package.Id] : isLoaded ? "Geladen" : "Nog laden";
        string statusColor = hasIncident ? "#DC2626" : isLoaded ? "#027A48" : "#6B7280";

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        grid.Add(new Label
        {
            Text = icon,
            FontFamily = "FontAwesome",
            FontSize = 15,
            TextColor = Color.FromArgb(iconColor),
            VerticalTextAlignment = TextAlignment.Center
        }, 0, 0);

        grid.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = $"{GetPackageCode(package)} — {package.CustomerName}",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#111827")
                },
                new Label
                {
                    Text = statusText,
                    FontSize = 12,
                    TextColor = Color.FromArgb(statusColor),
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        }, 1, 0);

        grid.Add(new Label
        {
            Text = $"Zone {zone}",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1D4ED8"),
            VerticalTextAlignment = TextAlignment.Center
        }, 2, 0);

        return new Frame
        {
            CornerRadius = 14,
            BackgroundColor = Color.FromArgb("#F9FAFB"),
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 12,
            HasShadow = false,
            Content = grid
        };
    }

    private View CreateDeliveryPackageCard(PackageItem package)
    {
        string zone = GetLoadZone(package);

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

        var content = new VerticalStackLayout
        {
            Spacing = 9,
            Children =
            {
                statusLabel,
                new Label
                {
                    Text = package.CustomerName,
                    FontSize = 21,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#111827")
                },
                new Label
                {
                    Text = package.Address,
                    FontSize = 15,
                    TextColor = Color.FromArgb("#6B7280")
                },
                new Label
                {
                    Text = $"Zone {zone} — {GetZoneDescription(zone)}",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1D4ED8")
                },
                new Label
                {
                    Text = $"{package.Size} • {package.WeightKg} kg",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#6B7280")
                },
                CreateDeliveryButtonGrid(package)
            }
        };

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E5E7EB"),
            Padding = 18,
            HasShadow = true,
            Content = content
        };
    }

    private View CreateDeliveryButtonGrid(PackageItem package)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(),
                new ColumnDefinition(),
                new ColumnDefinition()
            },
            ColumnSpacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };

        grid.Add(CreateSmallActionButton(
            "\uf124",
            "Navigeren",
            "#2D7DF6",
            async () =>
            {
                await DisplayAlert("Navigeren", "Navigatie wordt later toegevoegd.", "Oké");
            }), 0, 0);

        grid.Add(CreateSmallActionButton(
            "\uf058",
            "Afronden",
            "#16A34A",
            async () =>
            {
                await Navigation.PushAsync(new StatusPage(package.Id));
            }), 1, 0);

        grid.Add(CreateSmallActionButton(
            "\uf071",
            "Probleem",
            "#DC2626",
            async () =>
            {
                await ReportRouteProblemAsync(package);
            }), 2, 0);

        return grid;
    }

    private View CreateIncidentSummaryCard()
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 10
        };

        layout.Add(new Label
        {
            Text = "Gemelde incidenten",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#B42318")
        });

        foreach (KeyValuePair<int, string> incident in _packageIncidents)
        {
            PackageItem? package = _currentRide?.Packages.FirstOrDefault(item => item.Id == incident.Key);

            layout.Add(new Label
            {
                Text = package == null
                    ? $"Pakket {incident.Key}: {incident.Value}"
                    : $"{GetPackageCode(package)} — {package.CustomerName}: {incident.Value}",
                FontSize = 14,
                TextColor = Color.FromArgb("#374151"),
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        return new Frame
        {
            CornerRadius = 24,
            BackgroundColor = Color.FromArgb("#FEF2F2"),
            BorderColor = Color.FromArgb("#FCA5A5"),
            Padding = 16,
            HasShadow = false,
            Content = layout
        };
    }

    private async Task ReportLoadingProblemAsync(PackageItem package)
    {
        await Navigation.PushAsync(new PackageProblemPage(
            package.Id,
            "loading",
            RegisterPackageIncidentAsync));
    }

    private async Task ReportRouteProblemAsync(PackageItem package)
    {
        await Navigation.PushAsync(new PackageProblemPage(
            package.Id,
            "route",
            RegisterPackageIncidentAsync));
    }

    private Task RegisterPackageIncidentAsync(int packageId, string incidentText)
    {
        _packageIncidents[packageId] = incidentText;
        _loadedPackageIds.Remove(packageId);

        if (_activeScannedPackage != null && _activeScannedPackage.Id == packageId)
        {
            _activeScannedPackage = null;
        }

        RenderFlow();

        return Task.CompletedTask;
    }

    private View CreateCard(string title, string description, string titleColor, string descriptionColor)
    {
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
                    new Label
                    {
                        Text = title,
                        FontSize = 21,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb(titleColor)
                    },
                    new Label
                    {
                        Text = description,
                        FontSize = 14,
                        TextColor = Color.FromArgb(descriptionColor),
                        LineBreakMode = LineBreakMode.WordWrap
                    }
                }
            }
        };
    }

    private View CreatePrimaryButton(string text, Func<Task> action)
    {
        var button = new Button
        {
            Text = text,
            HeightRequest = 54,
            BackgroundColor = Color.FromArgb("#2D7DF6"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 16
        };

        button.Clicked += async (_, _) => await action();

        return button;
    }

    private View CreateWarningButton(string text, Func<Task> action)
    {
        var button = new Button
        {
            Text = text,
            HeightRequest = 52,
            BackgroundColor = Colors.White,
            TextColor = Color.FromArgb("#B42318"),
            BorderColor = Color.FromArgb("#FCA5A5"),
            BorderWidth = 1,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 16
        };

        button.Clicked += async (_, _) => await action();

        return button;
    }

    private View CreateSmallActionButton(string icon, string text, string color, Func<Task> action)
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = icon,
                    FontFamily = "FontAwesome",
                    FontSize = 18,
                    TextColor = Color.FromArgb(color),
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = text,
                    FontSize = 12,
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
            CornerRadius = 14,
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

    private PackageItem? GetNextPackageToLoad()
    {
        return GetOrderedPackages()
            .FirstOrDefault(package =>
                !_loadedPackageIds.Contains(package.Id) &&
                !_packageIncidents.ContainsKey(package.Id));
    }

    private PackageItem? GetNextPackageToDeliver()
    {
        return GetOrderedPackages()
            .FirstOrDefault(package =>
                !package.IsCompleted &&
                !_packageIncidents.ContainsKey(package.Id));
    }

    private List<PackageItem> GetOrderedPackages()
    {
        if (_currentRide == null)
        {
            return new List<PackageItem>();
        }

        return _currentRide.Packages
            .OrderBy(package => package.SequenceNumber)
            .ThenBy(package => package.Id)
            .ToList();
    }

    private bool AllPackagesLoadedOrReported()
    {
        if (_currentRide == null)
        {
            return false;
        }

        if (!_currentRide.Packages.Any())
        {
            return true;
        }

        return _currentRide.Packages.All(package =>
            _loadedPackageIds.Contains(package.Id) ||
            _packageIncidents.ContainsKey(package.Id));
    }

    private bool AllStopsHandled()
    {
        if (_currentRide == null)
        {
            return false;
        }

        if (!_currentRide.Packages.Any())
        {
            return true;
        }

        return _currentRide.Packages.All(package =>
            package.IsCompleted ||
            _packageIncidents.ContainsKey(package.Id));
    }

    private int GetOpenPackageCount()
    {
        if (_currentRide == null)
        {
            return 0;
        }

        return _currentRide.Packages.Count(package =>
            !package.IsCompleted &&
            !_packageIncidents.ContainsKey(package.Id));
    }

    private void UpdateRemainingPackagesLabel()
    {
        RemainingPackagesLabel.Text = GetOpenPackageCount().ToString();
    }

    private string GetLoadZone(PackageItem package)
    {
        List<PackageItem> orderedPackages = GetOrderedPackages();

        int index = orderedPackages.FindIndex(item => item.Id == package.Id);

        if (index < 0)
        {
            return "A";
        }

        int total = orderedPackages.Count;
        int quarterSize = (int)Math.Ceiling(total / 4.0);

        if (index < quarterSize) return "A";
        if (index < quarterSize * 2) return "B";
        if (index < quarterSize * 3) return "C";

        return "D";
    }

    private static string GetZoneDescription(string zone)
    {
        return zone switch
        {
            "A" => "links achter",
            "B" => "rechts achter",
            "C" => "links voor",
            "D" => "rechts voor",
            _ => "onbekende zone"
        };
    }

    private static string GetPackageCode(PackageItem package)
    {
        return $"PKG-{package.SequenceNumber:000}";
    }

    private void StartRouteTimer()
    {
        if (_currentRide == null || _timerRunning)
        {
            return;
        }

        _timerRunning = true;
        _timeWarningShown = false;

        _remainingTime = _currentRide.EndTime - _currentRide.StartTime;

        if (_remainingTime.TotalSeconds <= 0)
        {
            _remainingTime = TimeSpan.FromHours(2);
        }

        RouteTimerLabel.Text = FormatTime(_remainingTime);

        Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            RouteTimerLabel.Text = FormatTime(_remainingTime);

            if (_remainingTime.TotalSeconds <= 0 && !_timeWarningShown)
            {
                _timeWarningShown = true;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert(
                        "Tijd voorbij",
                        "Je bent over de geplande tijd heen. Rond je route af of meld vertraging bij de planner.",
                        "Oké");
                });
            }

            return _timerRunning;
        });
    }

    private static string FormatTime(TimeSpan time)
    {
        string sign = time.TotalSeconds < 0 ? "-" : "";
        time = time.Duration();

        int totalHours = (int)time.TotalHours;

        return $"{sign}{totalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }

    private enum WorkFlowStep
    {
        ConfirmShift = 1,
        ConfirmBus = 2,
        LoadPackages = 3,
        StartRoute = 4,
        DeliverStops = 5,
        CompleteRoute = 6
    }
}