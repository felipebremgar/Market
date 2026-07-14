using Market.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Cliente");

        builder.HasKey(c => c.Cpf);
        builder.Property(c => c.Cpf).ValueGeneratedNever();   // CPF é informado, não gerado
        builder.Property(c => c.Nome).IsRequired();

        builder.HasIndex(c => c.Nome).HasDatabaseName("IX_Cliente_Nome");
    }
}
