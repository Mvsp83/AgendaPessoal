using AgendaPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPessoal.Data;

public class AgendaDbContext(DbContextOptions<AgendaDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Item> Itens => Set<Item>();
    public DbSet<Tema> Temas => Set<Tema>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<Aniversario> Aniversarios => Set<Aniversario>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Configuracao>().HasKey(c => c.Chave);

        modelo.Entity<Item>()
            .HasOne(i => i.Tema)
            .WithMany(t => t.Itens)
            .HasForeignKey(i => i.TemaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelo.Entity<Item>()
            .HasMany(i => i.Tags)
            .WithMany(t => t.Itens);

        modelo.Entity<Tag>().HasIndex(t => t.Nome).IsUnique();

        modelo.Entity<Aniversario>()
            .HasOne(a => a.Tema)
            .WithMany()
            .HasForeignKey(a => a.TemaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
