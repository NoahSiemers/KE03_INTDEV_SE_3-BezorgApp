using System;
using System.Collections.Generic;
using System.Text;

namespace KE03_INTDEV_SE_3.Models
{
    public class Ride
    {
        public int Id { get; set; }

        public string Region { get; set; } = string.Empty;

        public string BusName { get; set; } = string.Empty;

        public int PackageCount { get; set; }

        public DateTime RideDate { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string BranchLocation { get; set; } = string.Empty;

        public int DriverId { get; set; }

        public Driver Driver { get; set; } = null!;

        public List<PackageItem> Packages { get; set; } = new();
    }
}
