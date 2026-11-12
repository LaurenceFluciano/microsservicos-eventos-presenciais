using Microsoft.EntityFrameworkCore;
using Usuario.Models;

namespace AppTemplate
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<UsuarioModel> Usuarios { get; set; }
        //public DbSet<Exemplo> Exemplos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioModel>().HasKey(p => p.Id);
            //modelBuilder.Entity<Exemplo>().HasKey(p => p.Id);


            base.OnModelCreating(modelBuilder);
        }
    }
}
