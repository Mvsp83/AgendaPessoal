using AgendaPessoal.Data;
using AgendaPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPessoal.Services;

public class ItemService(IDbContextFactory<AgendaDbContext> fabrica)
{
    public async Task<Item?> ObterAsync(int id)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Itens.AsNoTracking()
            .Include(i => i.Tema)
            .Include(i => i.Tags)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <param name="temaId">-1 filtra itens sem tema.</param>
    public async Task<List<Item>> ListarAsync(string? texto = null, int? temaId = null,
        string? tag = null, StatusItem? status = StatusItem.Ativo)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        IQueryable<Item> consulta = db.Itens.AsNoTracking()
            .Include(i => i.Tema)
            .Include(i => i.Tags);

        if (status != null)
            consulta = consulta.Where(i => i.Status == status);
        if (temaId == -1)
            consulta = consulta.Where(i => i.TemaId == null);
        else if (temaId != null)
            consulta = consulta.Where(i => i.TemaId == temaId);
        if (!string.IsNullOrWhiteSpace(tag))
            consulta = consulta.Where(i => i.Tags.Any(t => t.Nome == tag));
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var termo = texto.Trim().ToLower();
            consulta = consulta.Where(i => i.Titulo.ToLower().Contains(termo)
                || (i.Conteudo != null && i.Conteudo.ToLower().Contains(termo)));
        }

        return await consulta
            .OrderBy(i => i.DataHora == null)
            .ThenBy(i => i.DataHora)
            .ThenByDescending(i => i.AtualizadoEm)
            .ToListAsync();
    }

    public async Task<(List<Item> Atrasados, List<Item> Hoje, List<Item> Proximos, int SemData)> ObterAgendaAsync()
    {
        var hoje = FusoHorario.Agora().Date;
        var limite = hoje.AddDays(8);
        await using var db = await fabrica.CreateDbContextAsync();

        var agendados = await db.Itens.AsNoTracking()
            .Include(i => i.Tema)
            .Include(i => i.Tags)
            .Where(i => i.Status == StatusItem.Ativo && i.DataHora != null && i.DataHora < limite)
            .OrderBy(i => i.DataHora)
            .ToListAsync();
        var semData = await db.Itens
            .CountAsync(i => i.Status == StatusItem.Ativo && i.DataHora == null);

        return (
            agendados.Where(i => i.DataHora!.Value.Date < hoje).ToList(),
            agendados.Where(i => i.DataHora!.Value.Date == hoje).ToList(),
            agendados.Where(i => i.DataHora!.Value.Date > hoje).ToList(),
            semData);
    }

    public async Task SalvarAsync(Item item, IEnumerable<string> nomesTags)
    {
        await using var db = await fabrica.CreateDbContextAsync();

        var nomes = nomesTags.Select(n => n.Trim()).Where(n => n != "")
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var tagsExistentes = await db.Tags.ToListAsync();
        var tags = nomes.Select(nome =>
            tagsExistentes.FirstOrDefault(t => string.Equals(t.Nome, nome, StringComparison.OrdinalIgnoreCase))
            ?? new Tag { Nome = nome }).ToList();

        item.Tema = null; // navegação não deve ser anexada; só o TemaId conta
        item.AtualizadoEm = FusoHorario.Agora();

        if (item.Id == 0)
        {
            item.CriadoEm = item.AtualizadoEm;
            item.Tags = tags;
            db.Itens.Add(item);
        }
        else
        {
            var existente = await db.Itens.Include(i => i.Tags).FirstAsync(i => i.Id == item.Id);
            var dataAnterior = existente.DataHora;
            db.Entry(existente).CurrentValues.SetValues(item);
            if (existente.DataHora != dataAnterior)
                existente.UltimoLembreteEnviado = null; // data mudou: o lembrete vale de novo
            existente.Tags.Clear();
            foreach (var tag in tags)
                existente.Tags.Add(tag);
        }
        await db.SaveChangesAsync();
    }

    public async Task DefinirStatusAsync(int id, StatusItem status)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var item = await db.Itens.FindAsync(id);
        if (item == null) return;
        item.Status = status;
        item.AtualizadoEm = FusoHorario.Agora();
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var item = await db.Itens.FindAsync(id);
        if (item == null) return;
        db.Itens.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<Tag>> ListarTagsAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Tags.AsNoTracking().OrderBy(t => t.Nome).ToListAsync();
    }
}
