using Microsoft.EntityFrameworkCore;
using SistemaEsporte.Modelos;

namespace SistemaEsporte.Dados
{
    public class ContextoBanco : DbContext
    {
        public ContextoBanco(DbContextOptions<ContextoBanco> opcoes) : base(opcoes) { }

        public DbSet<Time>          Times          { get; set; }
        public DbSet<Partida>       Partidas       { get; set; }
        public DbSet<Torneio>       Torneios       { get; set; }
        public DbSet<TorneioTime>   TorneioTimes   { get; set; }
        public DbSet<PartidaTorneio> PartidasTorneio { get; set; }
        public DbSet<Usuario>       Usuarios       { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Usuario>()
              .HasIndex(u => u.NomeUsuario).IsUnique();

            mb.Entity<Usuario>()
              .HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

            mb.Entity<Torneio>()
              .HasIndex(t => t.CodigoConvite).IsUnique().HasFilter("[CodigoConvite] IS NOT NULL");

            mb.Entity<Torneio>()
              .Property(t => t.Status).HasConversion<int>();
            mb.Entity<Torneio>()
              .Property(t => t.Formato).HasConversion<int>();

            mb.Entity<PartidaTorneio>()
              .Property(p => p.Fase).HasConversion<int>();

            // Evita múltiplos caminhos de cascade para Partida→Time
            mb.Entity<Partida>()
              .HasOne(p => p.Time1).WithMany().HasForeignKey(p => p.Time1Id).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Partida>()
              .HasOne(p => p.Time2).WithMany().HasForeignKey(p => p.Time2Id).OnDelete(DeleteBehavior.Restrict);

            mb.Entity<TorneioTime>()
              .HasOne(tt => tt.Torneio).WithMany(t => t.Times).HasForeignKey(tt => tt.TorneioId).OnDelete(DeleteBehavior.Cascade);

            mb.Entity<PartidaTorneio>()
              .HasOne(p => p.Torneio).WithMany(t => t.Partidas).HasForeignKey(p => p.TorneioId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
