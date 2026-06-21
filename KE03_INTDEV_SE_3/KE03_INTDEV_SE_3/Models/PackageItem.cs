using System;
using System.Collections.Generic;
using System.Text;

namespace KE03_INTDEV_SE_3.Models
{
    public class PackageItem
    {
        public int Id { get; set; }

        public int SequenceNumber { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public double WeightKg { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsCompleted { get; set; }

        public int RideId { get; set; }

        public Ride Ride { get; set; } = null!;
    }
}
