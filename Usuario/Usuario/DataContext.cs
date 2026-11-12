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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioModel>(entity =>
            {
                entity.ToTable("Usuarios");

                entity.HasKey(u => u.Id);

                entity.Property(u => u.Id)
                      .ValueGeneratedNever(); 

                entity.Property(u => u.Nome)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(u => u.Telefone)
                      .HasMaxLength(20);
            });

            modelBuilder.Entity<ContaModel>(entity =>
            {
                entity.ToTable("contas");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id).ValueGeneratedNever();
                entity.Property(c => c.UsuarioId).IsRequired();
                entity.HasIndex(c => c.UsuarioId).IsUnique();
                entity.Property(c => c.SenhaHash).IsRequired().HasMaxLength(255);
                entity.Property(c => c.EmailVerificado).IsRequired();
                entity.Property(c => c.CriadoEm).IsRequired();

                entity.HasOne(c => c.Usuario)
                      .WithOne(u => u.Conta)
                      .HasForeignKey<ContaModel>(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
