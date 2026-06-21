using System;
using System.Collections.Generic;
using System.Text;

namespace KE03_INTDEV_SE_3.Models
{
    public class Driver
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public List<Ride> Rides { get; set; } = new();
    }
}
