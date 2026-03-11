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
        public DbSet<Empresa> Empresas { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Perfil> Perfis { get; set; } = null!;
        public DbSet<Mesa> Mesas { get; set; } = null!;

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

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Endereco)
                .HasMaxLength(250);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Empresa)
                .WithMany(e => e.Pedidos)
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Mesa)
                .WithMany(m => m.Pedidos)
                .HasForeignKey(p => p.MesaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Item>()
                .Property(i => i.Nome)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<Item>()
                .HasOne(i => i.Empresa)
                .WithMany(e => e.Itens)
                .HasForeignKey(i => i.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Mesa>()
                .Property(m => m.Numero)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Mesa>()
                .Property(m => m.Ativa)
                .HasDefaultValue(true)
                .IsRequired();

            modelBuilder.Entity<Mesa>()
                .HasOne(m => m.Empresa)
                .WithMany(e => e.Mesas)
                .HasForeignKey(m => m.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItensPedido>()
                .HasOne(ip => ip.Pedido)
                .WithMany(p => p.Itens)
                .HasForeignKey(ip => ip.PedidoId);

            modelBuilder.Entity<ItensPedido>()
                .HasOne(ip => ip.Item)
                .WithMany(i => i.ItensPedido)
                .HasForeignKey(ip => ip.ItemId);

            modelBuilder.Entity<Empresa>()
                .Property(e => e.Cnpj)
                .HasMaxLength(18)
                .IsRequired();

            modelBuilder.Entity<Empresa>()
                .Property(e => e.Nome)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Empresa>()
                .Property(e => e.RazaoSocial)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Empresa>()
                .Property(e => e.Seguimento)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Email)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Nome)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.SenhaHash)
                .HasMaxLength(200)
                .HasColumnName("Senha");

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Empresa)
                .WithMany(e => e.Usuarios)
                .HasForeignKey(u => u.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Perfil)
                .WithMany(p => p.Usuarios)
                .HasForeignKey(u => u.PerfilId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Perfil>()
                .Property(p => p.Nome)
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
