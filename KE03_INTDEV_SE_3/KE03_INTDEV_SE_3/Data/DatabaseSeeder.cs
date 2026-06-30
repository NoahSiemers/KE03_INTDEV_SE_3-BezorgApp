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
                Region = "Regio Heerlen",
                BusName = "Bus 1",
                PackageCount = 5,
                RideDate = DateTime.Today,
                StartTime = DateTime.Today.AddHours(12),
                EndTime = DateTime.Today.AddHours(15),
                BranchLocation = "Matrix Depot Heerlen",
                Driver = driver,
                Packages = new List<PackageItem>
                {
                    new PackageItem
                    {
                        SequenceNumber = 1,
                        CustomerName = "Jan de School",
                        Address = "Hoofdstraat 12, Heerlen",
                        ActionType = "Bezorgen",
                        Size = "Middel",
                        WeightKg = 2.5
                    },
                    new PackageItem
                    {
                        SequenceNumber = 2,
                        CustomerName = "Marie de Boer",
                        Address = "Kruisweg 59, Kerkrade",
                        ActionType = "Ophalen",
                        Size = "Klein",
                        WeightKg = 1
                    },
                    new PackageItem
                    {
                        SequenceNumber = 3,
                        CustomerName = "M. Jansen",
                        Address = "Akerstraat 22, Heerlen",
                        ActionType = "Bezorgen",
                        Size = "Groot",
                        WeightKg = 4.2
                    },
                    new PackageItem
                    {
                        SequenceNumber = 4,
                        CustomerName = "M. Jansen",
                        Address = "Akerstraat 22, Heerlen",
                        ActionType = "Bezorgen",
                        Size = "Klein",
                        WeightKg = 0.8
                    },
                    new PackageItem
                    {
                        SequenceNumber = 5,
                        CustomerName = "Opalijn",
                        Address = "Kruisweg 59, Kerkrade",
                        ActionType = "Ophalen",
                        Size = "Klein",
                        WeightKg = 1
                    }
                }
            };

            var todayRide2 = new Ride
            {
                Region = "Regio Maastricht",
                BusName = "Bus 2",
                PackageCount = 4,
                RideDate = DateTime.Today,
                StartTime = DateTime.Today.AddHours(13),
                EndTime = DateTime.Today.AddHours(16),
                BranchLocation = "Matrix Depot Maastricht",
                Driver = driver,
                Packages = new List<PackageItem>
                {
                    new PackageItem
                    {
                        SequenceNumber = 1,
                        CustomerName = "Lisa Peters",
                        Address = "Vrijthof 10, Maastricht",
                        ActionType = "Bezorgen",
                        Size = "Middel",
                        WeightKg = 3
                    },
                    new PackageItem
                    {
                        SequenceNumber = 2,
                        CustomerName = "Ahmed Kaya",
                        Address = "Stationsstraat 22, Maastricht",
                        ActionType = "Bezorgen",
                        Size = "Klein",
                        WeightKg = 1.4
                    },
                    new PackageItem
                    {
                        SequenceNumber = 3,
                        CustomerName = "Sophie Janssen",
                        Address = "Brusselsestraat 88, Maastricht",
                        ActionType = "Ophalen",
                        Size = "Groot",
                        WeightKg = 5.1
                    },
                    new PackageItem
                    {
                        SequenceNumber = 4,
                        CustomerName = "Hotel Maaszicht",
                        Address = "Wycker Brugstraat 7, Maastricht",
                        ActionType = "Bezorgen",
                        Size = "Middel",
                        WeightKg = 2.2
                    }
                }
            };

            var todayRide3 = new Ride
            {
                Region = "Regio Geleen",
                BusName = "Bus 3",
                PackageCount = 4,
                RideDate = DateTime.Today,
                StartTime = DateTime.Today.AddHours(15).AddMinutes(30),
                EndTime = DateTime.Today.AddHours(18),
                BranchLocation = "Matrix Depot Geleen",
                Driver = driver,
                Packages = new List<PackageItem>
                {
                    new PackageItem
                    {
                        SequenceNumber = 1,
                        CustomerName = "GameState",
                        Address = "Centrum 5, Geleen",
                        ActionType = "Ophalen",
                        Size = "Klein",
                        WeightKg = 1
                    },
                    new PackageItem
                    {
                        SequenceNumber = 2,
                        CustomerName = "Bakkerij Smeets",
                        Address = "Rijksweg Zuid 44, Geleen",
                        ActionType = "Bezorgen",
                        Size = "Middel",
                        WeightKg = 2.7
                    },
                    new PackageItem
                    {
                        SequenceNumber = 3,
                        CustomerName = "Daan Verbeek",
                        Address = "Markt 18, Geleen",
                        ActionType = "Bezorgen",
                        Size = "Klein",
                        WeightKg = 0.9
                    },
                    new PackageItem
                    {
                        SequenceNumber = 4,
                        CustomerName = "Fysiopraktijk Zuid",
                        Address = "Mauritslaan 30, Geleen",
                        ActionType = "Bezorgen",
                        Size = "Groot",
                        WeightKg = 6.3
                    }
                }
            };

            var futureRide = new Ride
            {
                Region = "Regio Maastricht",
                BusName = "Bus 4",
                PackageCount = 1,
                RideDate = DateTime.Today.AddDays(2),
                StartTime = DateTime.Today.AddDays(2).AddHours(16),
                EndTime = DateTime.Today.AddDays(2).AddHours(18).AddMinutes(30),
                BranchLocation = "Matrix Depot Maastricht",
                Driver = driver,
                Packages = new List<PackageItem>
                {
                    new PackageItem
                    {
                        SequenceNumber = 1,
                        CustomerName = "Lisa Peters",
                        Address = "Vrijthof 10, Maastricht",
                        ActionType = "Bezorgen",
                        Size = "Middel",
                        WeightKg = 3
                    }
                }
            };

            var futureRide2 = new Ride
            {
                Region = "Regio Sittard",
                BusName = "Bus 5",
                PackageCount = 1,
                RideDate = DateTime.Today.AddDays(7),
                StartTime = DateTime.Today.AddDays(7).AddHours(15).AddMinutes(30),
                EndTime = DateTime.Today.AddDays(7).AddHours(17).AddMinutes(30),
                BranchLocation = "Matrix Depot Sittard",
                Driver = driver,
                Packages = new List<PackageItem>
                {
                    new PackageItem
                    {
                        SequenceNumber = 1,
                        CustomerName = "Opalijn",
                        Address = "Centrum 5, Sittard",
                        ActionType = "Ophalen",
                        Size = "Klein",
                        WeightKg = 1
                    }
                }
            };

            db.Drivers.Add(driver);
            db.Drivers.Add(driver2);

            db.Rides.Add(todayRide);
            db.Rides.Add(todayRide2);
            db.Rides.Add(todayRide3);
            db.Rides.Add(futureRide);
            db.Rides.Add(futureRide2);

            db.SaveChanges();
        }
    }
}