using Microsoft.EntityFrameworkCore;

namespace Evento
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


            base.OnModelCreating(modelBuilder);
        }
    }
}
