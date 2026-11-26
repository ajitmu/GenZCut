using GenZCut.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GenZCut.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Barber> Barbers { get; set; }

        public DbSet<Service> Services { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<BarberSchedule> BarberSchedules { get; set; }

        public DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Service>()
                .HasOne(s => s.Barber)
                .WithMany(b => b.Services)
                .HasForeignKey(s => s.BarberId);

            builder.Entity<Appointment>()
                .HasOne(a => a.Barber)
                .WithMany(b => b.Appointments)
                .HasForeignKey(a => a.BarberId);

            builder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId);

            builder.Entity<Feedback>()
                .ToTable("Feedback");
        }
    }
}