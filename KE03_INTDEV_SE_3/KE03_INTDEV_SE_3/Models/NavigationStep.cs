namespace KE03_INTDEV_SE_3.Models;

public class NavigationStep
{
    public string Instruction { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
}