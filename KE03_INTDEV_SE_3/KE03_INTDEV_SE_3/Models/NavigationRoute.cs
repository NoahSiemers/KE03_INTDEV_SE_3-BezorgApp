using Microsoft.Maui.Devices.Sensors;

namespace KE03_INTDEV_SE_3.Models;

public class NavigationRoute
{
    public List<Location> Path { get; set; } = new();
    public List<NavigationStep> Steps { get; set; } = new();
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
}