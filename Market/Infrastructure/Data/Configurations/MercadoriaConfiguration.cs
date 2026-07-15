using Market.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Data.Configurations;

public class MercadoriaConfiguration : IEntityTypeConfiguration<Mercadoria>
{
    public void Configure(EntityTypeBuilder<Mercadoria> builder)
    {
        builder.ToTable("Mercadoria");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.Nome).IsRequired();
        builder.Property(m => m.Unidade).HasConversion<string>().IsRequired();
        builder.Property(m => m.PrecoCusto).IsRequired();
        builder.Property(m => m.PrecoVenda).IsRequired();
        builder.Property(m => m.Quantidade).IsRequired();
        builder.Property(m => m.Ativo).IsRequired();

        builder.Property(m => m.Validade).HasConversion(ValueConverters.IsoDate);

        // Preenchida pelo banco na inserção (mesmo default do DDL).
        builder.Property(m => m.DataCadastro)
            .HasConversion(ValueConverters.IsoDateTime)
            .HasDefaultValueSql("strftime('%Y-%m-%dT%H:%M:%S','now','localtime')")
            .ValueGeneratedOnAdd();

        builder.HasIndex(m => m.CodigoBarras)
            .IsUnique()
            .HasFilter("CodigoBarras IS NOT NULL")
            .HasDatabaseName("UQ_Mercadoria_CodigoBarras");
        builder.HasIndex(m => m.Nome).HasDatabaseName("IX_Mercadoria_Nome");
        builder.HasIndex(m => m.Fornecedor).HasDatabaseName("IX_Mercadoria_Fornecedor");
        builder.HasIndex(m => m.Validade).HasDatabaseName("IX_Mercadoria_Validade");
    }
}
