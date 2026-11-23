using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<CredencialAcesso> CredenciaisAcesso => Set<CredencialAcesso>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CredencialAcesso>(entity =>
            {
                entity.ToTable("credenciais_acesso");

                entity.HasKey(c => c.Id);


                entity.Property(c => c.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(c => c.DataEmissao)
                    .IsRequired();

                entity.HasIndex(c => c.IngressoId).IsUnique();
                entity.HasIndex(c => c.EventoId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
