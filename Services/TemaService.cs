using AgendaPessoal.Data;
using AgendaPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPessoal.Services;

public class TemaService(IDbContextFactory<AgendaDbContext> fabrica)
{
    public async Task<List<Tema>> ListarAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Temas.AsNoTracking().OrderBy(t => t.Nome).ToListAsync();
    }

    public async Task SalvarAsync(Tema tema)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        if (tema.Id == 0)
            db.Temas.Add(tema);
        else
            db.Temas.Update(tema);
        await db.SaveChangesAsync();
    }

    /// <summary>Itens do tema excluído ficam "sem tema" (FK com SET NULL).</summary>
    public async Task ExcluirAsync(int id)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var tema = await db.Temas.FindAsync(id);
        if (tema == null) return;
        db.Temas.Remove(tema);
        await db.SaveChangesAsync();
    }
}
