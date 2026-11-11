using Microsoft.EntityFrameworkCore;

namespace AppTemplate
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        //public DbSet<Exemplo> Exemplos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Exemplo>().HasKey(p => p.Id);


            base.OnModelCreating(modelBuilder);
        }
    }
}
