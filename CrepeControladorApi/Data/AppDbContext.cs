using CrepeControladorApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CrepeControladorApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<Item> Itens { get; set; } = null!;
        public DbSet<ItensPedido> ItensPedidos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Pedido>()
                .Property(p => p.Codigo)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<Pedido>()
                .Property(p => p.TipoPedido)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Status)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<Item>()
                .Property(i => i.Nome)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<ItensPedido>()
                .HasOne(ip => ip.Pedido)
                .WithMany(p => p.Itens)
                .HasForeignKey(ip => ip.PedidoId);

            modelBuilder.Entity<ItensPedido>()
                .HasOne(ip => ip.Item)
                .WithMany(i => i.ItensPedido)
                .HasForeignKey(ip => ip.ItemId);
        }
    }
}
