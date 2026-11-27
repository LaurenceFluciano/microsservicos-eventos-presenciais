
using Api.Infrastructure.Converters;
using Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Api;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {

    }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
    }

    public DbSet<Ingresso> Ingressos => Set<Ingresso>();
    public DbSet<OfertaIngresso> OfertasIngresso => Set<OfertaIngresso>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<PagamentoTentativa> PagamentoTentativas =>
        Set<PagamentoTentativa>();

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

            entity.HasIndex(o => o.IdEvento);

            entity.HasIndex(o => new
            {
                o.IdOrganizador,
                o.IdEvento
            });
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

            });

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Quantidade)
                .IsRequired();

            entity.Property(p => p.ValorUnitario)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(p => p.CriadoEm)
                .IsRequired();

            entity.Property(p => p.ExpiraEm)
                .IsRequired();

            entity.HasOne(p => p.OfertaIngresso)
                .WithMany(o => o.Pedidos)
                .HasForeignKey(p => p.OfertaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => new
            {
                p.Status,
                p.ExpiraEm
            });
            entity.HasIndex(p => p.CompradorId);
            entity.HasIndex(p => p.Status);
        });

        // ========================================
        // PAGAMENTO
        // ========================================

                
        modelBuilder.Entity<Pagamento>(entity =>
        {
            entity.ToTable("pagamentos");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.PedidoId)
                .IsRequired();

            entity.Property(p => p.Valor)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            // Identificação da cobrança externa
            entity.Property(p => p.IdExterno)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(p => p.UrlCheckout)
                .HasMaxLength(2048)
                .IsRequired();

            // Datas
            entity.Property(p => p.CriadoEm)
                .IsRequired();

            entity.Property(p => p.CobrancaGeradaEm)
                .IsRequired(false);

            entity.Property(p => p.DataConfirmacao)
                .IsRequired(false);

            // Relacionamentos
            entity.HasOne(p => p.Pedido)
                .WithMany(pedido => pedido.Pagamentos)
                .HasForeignKey(p => p.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices
            entity.HasIndex(p => p.PedidoId);

            entity.HasIndex(p => p.IdExterno)
                .IsUnique()
                .HasFilter("\"IdExterno\" <> ''");
        });



        modelBuilder.Entity<PagamentoTentativa>(entity =>
        {
            entity.ToTable("pagamento_tentativas");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(t => t.TentadoEm)
                .IsRequired();

            entity.HasOne<Pagamento>()
                .WithMany()
                .HasForeignKey(t => t.PagamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => new
            {
                t.PagamentoId,
                t.TentadoEm
            });
        });

    }
}
