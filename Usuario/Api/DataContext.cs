using Microsoft.EntityFrameworkCore;
using Usuario.Models;

namespace Api
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<UsuarioModel> Usuarios { get; set; } = null!;

        public DbSet<ContaModel> Contas { get; set; } = null!;

        public DbSet<SolicitacaoCadastro> SolicitacoesCadastro { get; set; } = null!;

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
                      .HasMaxLength(255)
                      .UseCollation("NOCASE");

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.Telefone)
                      .HasMaxLength(20);
            });

            modelBuilder.Entity<SolicitacaoCadastro>(entity =>
            {
                entity.ToTable("solicitacoes_cadastro");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).ValueGeneratedNever();
                entity.Property(s => s.Nome).IsRequired().HasMaxLength(150);
                entity.Property(s => s.Email).IsRequired().HasMaxLength(255).UseCollation("NOCASE");
                entity.Property(s => s.Telefone).HasMaxLength(20);
                entity.Property(s => s.SenhaHash).IsRequired().HasMaxLength(255);
                entity.Property(s => s.TokenHash).IsRequired().HasMaxLength(64);
                entity.HasIndex(s => s.TokenHash).IsUnique();
                entity.HasIndex(s => s.Email);
                entity.Property(s => s.CriadoEm).IsRequired();
                entity.Property(s => s.ExpiraEm).IsRequired();
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
