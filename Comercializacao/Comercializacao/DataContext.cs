
using Comercializacao.Model;
using Microsoft.EntityFrameworkCore;

namespace Comercializacao;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    public DbSet<Ingresso> Ingressos => Set<Ingresso>();
    public DbSet<OfertaIngresso> OfertasIngresso => Set<OfertaIngresso>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========================================
        // INGRESSO
        // ========================================

        modelBuilder.Entity<Ingresso>(entity =>
        {
            entity.ToTable("ingressos");

            entity.HasKey(i => i.Id);

            entity.Property(i => i.CodigoIngresso)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(i => i.CodigoIngresso)
                .IsUnique();

            entity.Property(i => i.EmitidoEm)
                .IsRequired();

            entity.HasOne(i => i.Pedido)
                .WithMany(p => p.Ingressos)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================
        // OFERTA DE INGRESSO
        // ========================================

        modelBuilder.Entity<OfertaIngresso>(entity =>
        {
            entity.ToTable("ofertas_ingresso", table =>
            {
                table.HasCheckConstraint(
                    "CK_Oferta_Preco",
                    "Preco >= 0"
                );

                table.HasCheckConstraint(
                    "CK_Oferta_Quantidade",
                    "QuantidadeTotal > 0"
                );
            });

            entity.HasKey(o => o.Id);

            entity.Property(o => o.Nome)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(o => o.Preco)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(o => o.QuantidadeTotal)
                .IsRequired();

            entity.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(o => o.CriadoEm)
                .IsRequired();

            entity.HasIndex(o => o.EventoId);
        });

        // ========================================
        // PEDIDO
        // ========================================

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("pedidos", table =>
            {
                table.HasCheckConstraint(
                    "CK_Pedido_Quantidade",
                    "Quantidade > 0"
                );

                table.HasCheckConstraint(
                    "CK_Pedido_ValorUnitario",
                    "ValorUnitario >= 0"
                );

                table.HasCheckConstraint(
                    "CK_Pedido_ValorTotal",
                    "ValorTotal >= 0"
                );
            });

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Quantidade)
                .IsRequired();

            entity.Property(p => p.ValorUnitario)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(p => p.ValorTotal)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(p => p.CriadoEm)
                .IsRequired();

            entity.Property(p => p.ExpiraEm);

            entity.HasOne(p => p.OfertaIngresso)
                .WithMany(o => o.Pedidos)
                .HasForeignKey(p => p.OfertaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.CompradorId);
            entity.HasIndex(p => p.Status);
        });
    }
}
