using KE03_INTDEV_SE_3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace KE03_INTDEV_SE_3.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<Ride> Rides => Set<Ride>();
        public DbSet<PackageItem> Packages => Set<PackageItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Driver>()
                .HasMany(d => d.Rides)
                .WithOne(r => r.Driver)
                .HasForeignKey(r => r.DriverId);

            modelBuilder.Entity<Ride>()
                .HasMany(r => r.Packages)
                .WithOne(p => p.Ride)
                .HasForeignKey(p => p.RideId);
        }
    }
}
