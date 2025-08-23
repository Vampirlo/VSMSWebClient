using Microsoft.EntityFrameworkCore;
using VSMSWebClient.Models;

namespace VSMSWebClient.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Request> Requests { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Request>().ToTable("requests");

            modelBuilder.Entity<Request>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.FirstName)
                    .HasColumnName("firstName");

                entity.Property(e => e.SecondName)
                    .HasColumnName("secondName");

                entity.Property(e => e.LastName)
                    .HasColumnName("lastName");

                entity.Property(e => e.PhoneNumber)
                    .HasColumnName("phoneNumber");

                entity.Property(e => e.Uuid)
                    .HasColumnName("uuid");

                entity.Property(e => e.Status)
                    .HasColumnName("status");

                entity.Property(e => e.Message)
                    .HasColumnName("message");

                entity.Property(e => e.SendTime)
                    .HasColumnName("sendTime");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}