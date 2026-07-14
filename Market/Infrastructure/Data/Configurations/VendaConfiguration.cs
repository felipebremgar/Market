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
