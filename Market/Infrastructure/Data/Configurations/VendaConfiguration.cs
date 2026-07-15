using Market.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Data.Configurations;

public class VendaConfiguration : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> builder)
    {
        builder.ToTable("Venda");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedOnAdd();

        builder.Property(v => v.ValorTotal).IsRequired();

        // Enum gravado como texto (nome) na coluna FormaPagamento; nulo em vendas antigas.
        builder.Property(v => v.Forma)
            .HasColumnName("FormaPagamento")
            .HasConversion<string>();

        // Fiado: situação (texto), vencimento (data) e baixa (data/hora) — todos opcionais.
        builder.Property(v => v.Status)
            .HasColumnName("StatusPagamento")
            .HasConversion<string>();
        builder.Property(v => v.DataVencimento).HasConversion(ValueConverters.IsoDate);
        builder.Property(v => v.DataBaixa).HasConversion(ValueConverters.IsoDateTime);

        builder.Property(v => v.DataVenda)
            .HasConversion(ValueConverters.IsoDateTime)
            .HasDefaultValueSql("strftime('%Y-%m-%dT%H:%M:%S','now','localtime')")
            .ValueGeneratedOnAdd();

        // ClienteCpf opcional; ao excluir o cliente, a venda permanece com cliente nulo.
        builder.HasOne(v => v.Cliente)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.ClienteCpf)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.DataVenda).HasDatabaseName("IX_Venda_DataVenda");
        builder.HasIndex(v => v.ClienteCpf).HasDatabaseName("IX_Venda_ClienteCpf");
    }
}
