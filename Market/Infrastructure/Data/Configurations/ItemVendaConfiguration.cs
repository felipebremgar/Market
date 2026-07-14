using Market.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Data.Configurations;

public class ItemVendaConfiguration : IEntityTypeConfiguration<ItemVenda>
{
    public void Configure(EntityTypeBuilder<ItemVenda> builder)
    {
        builder.ToTable("ItemVenda");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.Quantidade).IsRequired();
        builder.Property(i => i.PrecoUnitario).IsRequired();
        builder.Property(i => i.PrecoCusto).IsRequired();

        // Excluir a venda cascateia para seus itens.
        builder.HasOne(i => i.Venda)
            .WithMany(v => v.Itens)
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mercadoria referenciada não pode ser apagada fisicamente enquanto houver itens
        // (usa-se exclusão lógica via Ativo = 0).
        builder.HasOne(i => i.Mercadoria)
            .WithMany(m => m.ItensVenda)
            .HasForeignKey(i => i.MercadoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.VendaId).HasDatabaseName("IX_ItemVenda_VendaId");
        builder.HasIndex(i => i.MercadoriaId).HasDatabaseName("IX_ItemVenda_MercadoriaId");
    }
}
