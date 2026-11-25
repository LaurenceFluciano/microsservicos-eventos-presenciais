using Microsoft.EntityFrameworkCore;
using Api.Model;


namespace Api
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<Evento> Eventos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Evento>().HasKey(p => p.Id);

            modelBuilder.Entity<Evento>()
                .HasIndex(e => new
                {
                    e.Slug,
                    e.SlugNumero
                })
                .IsUnique();


            base.OnModelCreating(modelBuilder);
        }
    }
}
