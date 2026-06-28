using KE03_INTDEV_SE_3.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace KE03_INTDEV_SE_3.Data
{
    public static class DatabaseSeeder
    {
        public static void Seed(AppDbContext db)
        {
            if (db.Drivers.Any())
            {
                return;
            }

            var driver = new Driver
            {
                Name = "ik de Chauffeur",
                Email = "Test@gmail.com",
                Password = "1234"
            };

            var driver2 = new Driver
            {
                Name = "ik de Chauffeur",
                Email = "Test1@gmail.com",
                Password = "12345"
            };

            var todayRide = new Ride
            {
                Region = "Regio Geleen",
                BusName = "Bus 3",
                RideDate = DateTime.Today,
                StartTime = DateTime.Today.AddHours(15).AddMinutes(30),
                EndTime = DateTime.Today.AddHours(17).AddMinutes(30),
                BranchLocation = "Matrix Depot Geleen",
                Driver = driver,
                Packages = new List<PackageItem>
            {
                new PackageItem
                {
                    SequenceNumber = 1,
                    CustomerName = "Jan de Vries",
                    Address = "Hoofdstraat 12, 6161 AB Geleen",
                    ActionType = "Bezorgen",
                    Size = "Klein",
                    WeightKg = 1.0
                },
                new PackageItem
                {
                    SequenceNumber = 2,
                    CustomerName = "Maria Jansen",
                    Address = "Parkweg 45, 6161 EN Geleen",
                    ActionType = "Bezorgen",
                    Size = "Middel",
                    WeightKg = 2.5
                },
                new PackageItem
                {
                    SequenceNumber = 3,
                    CustomerName = "Bakkerij Smeets",
                    Address = "Markt 8, 6161 GE Geleen",
                    ActionType = "Ophalen",
                    Size = "Groot",
                    WeightKg = 4.8
                },
                new PackageItem
                {
                    SequenceNumber = 4,
                    CustomerName = "Tom Peeters",
                    Address = "Rijksweg Noord 102, 6162 AL Geleen",
                    ActionType = "Bezorgen",
                    Size = "Klein",
                    WeightKg = 0.7
                },
                new PackageItem
                {
                    SequenceNumber = 5,
                    CustomerName = "Sophie Lemmens",
                    Address = "Annastraat 21, 6161 GW Geleen",
                    ActionType = "Bezorgen",
                    Size = "Middel",
                    WeightKg = 3.1
                }
            }
            };

            todayRide.PackageCount = todayRide.Packages.Count;

            var tomorrowRide = new Ride
            {
                Region = "Regio Heerlen",
                BusName = "Bus 7",
                RideDate = DateTime.Today.AddDays(1),
                StartTime = DateTime.Today.AddDays(1).AddHours(10),
                EndTime = DateTime.Today.AddDays(1).AddHours(13),
                BranchLocation = "Matrix Depot Heerlen",
                Driver = driver,
                Packages = new List<PackageItem>
            {
                new PackageItem
                {
                    SequenceNumber = 1,
                    CustomerName = "Linda Meijer",
                    Address = "Akerstraat 31, 6411 GV Heerlen",
                    ActionType = "Bezorgen",
                    Size = "Klein",
                    WeightKg = 1.2
                },
                new PackageItem
                {
                    SequenceNumber = 2,
                    CustomerName = "Kantoor Nova",
                    Address = "Stationsplein 4, 6411 NE Heerlen",
                    ActionType = "Bezorgen",
                    Size = "Groot",
                    WeightKg = 6.4
                },
                new PackageItem
                {
                    SequenceNumber = 3,
                    CustomerName = "Mila Hendriks",
                    Address = "Caumerbeeklaan 18, 6417 XR Heerlen",
                    ActionType = "Ophalen",
                    Size = "Middel",
                    WeightKg = 2.2
                }
            }
            };

            tomorrowRide.PackageCount = tomorrowRide.Packages.Count;

            var overTomorrowRide1 = new Ride
            {
                Region = "Regio Kerkrade",
                BusName = "Bus 11",
                RideDate = DateTime.Today.AddDays(2),
                StartTime = DateTime.Today.AddDays(2).AddHours(9),
                EndTime = DateTime.Today.AddDays(2).AddHours(11).AddMinutes(30),
                BranchLocation = "Matrix Depot Kerkrade",
                Driver = driver,
                Packages = new List<PackageItem>
            {
                new PackageItem
                {
                    SequenceNumber = 1,
                    CustomerName = "Roy Maassen",
                    Address = "Kruisstraat 59, 6461 AB Kerkrade",
                    ActionType = "Bezorgen",
                    Size = "Klein",
                    WeightKg = 0.9
                },
                new PackageItem
                {
                    SequenceNumber = 2,
                    CustomerName = "Daan Jacobs",
                    Address = "Einderstraat 14, 6461 EM Kerkrade",
                    ActionType = "Bezorgen",
                    Size = "Middel",
                    WeightKg = 2.8
                }
            }
            };

            overTomorrowRide1.PackageCount = overTomorrowRide1.Packages.Count;

            var overTomorrowRide2 = new Ride
            {
                Region = "Regio Maastricht",
                BusName = "Bus 2",
                RideDate = DateTime.Today.AddDays(2),
                StartTime = DateTime.Today.AddDays(2).AddHours(12),
                EndTime = DateTime.Today.AddDays(2).AddHours(15),
                BranchLocation = "Matrix Depot Maastricht",
                Driver = driver,
                Packages = new List<PackageItem>
            {
                new PackageItem
                {
                    SequenceNumber = 1,
                    CustomerName = "Hotel Maaszicht",
                    Address = "Vrijthof 10, 6211 LD Maastricht",
                    ActionType = "Bezorgen",
                    Size = "Groot",
                    WeightKg = 7.5
                },
                new PackageItem
                {
                    SequenceNumber = 2,
                    CustomerName = "Emma Claessen",
                    Address = "Brusselsestraat 88, 6211 PG Maastricht",
                    ActionType = "Bezorgen",
                    Size = "Klein",
                    WeightKg = 1.1
                },
                new PackageItem
                {
                    SequenceNumber = 3,
                    CustomerName = "Studentenhuis Wyck",
                    Address = "Stationsstraat 22, 6221 BN Maastricht",
                    ActionType = "Ophalen",
                    Size = "Middel",
                    WeightKg = 3.0
                }
            }
            };

            overTomorrowRide2.PackageCount = overTomorrowRide2.Packages.Count;

            var overTomorrowRide3 = new Ride
            {
                Region = "Regio Sittard",
                BusName = "Bus 5",
                RideDate = DateTime.Today.AddDays(2),
                StartTime = DateTime.Today.AddDays(2).AddHours(16),
                EndTime = DateTime.Today.AddDays(2).AddHours(18).AddMinutes(30),
                BranchLocation = "Matrix Depot Sittard",
                Driver = driver,
                Packages = new List<PackageItem>
            {
                new PackageItem
                {
                    SequenceNumber = 1,
                    CustomerName = "Noah Siemers",
                    Address = "Dorpstraat 7, 6131 BK Sittard",
                    ActionType = "Bezorgen",
                    Size = "Middel",
                    WeightKg = 2.4
                },
                new PackageItem
                {
                    SequenceNumber = 2,
                    CustomerName = "Fietsenwinkel Zuid",
                    Address = "Voorstad 3, 6131 CR Sittard",
                    ActionType = "Ophalen",
                    Size = "Groot",
                    WeightKg = 5.6
                },
                new PackageItem
                {
                    SequenceNumber = 3,
                    CustomerName = "Anouk Frissen",
                    Address = "Putstraat 41, 6131 HK Sittard",
                    ActionType = "Bezorgen",
                    Size = "Klein",
                    WeightKg = 0.6
                }
            }
            };

            overTomorrowRide3.PackageCount = overTomorrowRide3.Packages.Count;

            db.Drivers.Add(driver);
            db.Rides.Add(todayRide);
            db.Rides.Add(tomorrowRide);
            db.Rides.Add(overTomorrowRide1);
            db.Rides.Add(overTomorrowRide2);
            db.Rides.Add(overTomorrowRide3);

            db.SaveChanges();
        }
    }
}