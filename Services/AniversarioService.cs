using AgendaPessoal.Data;
using AgendaPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPessoal.Services;

/// <summary>Aniversário projetado na próxima ocorrência (a partir de hoje).</summary>
public record AniversarioProximo(Aniversario Aniversario, DateTime Data, int DiasRestantes, int? Idade);

public class AniversarioService(IDbContextFactory<AgendaDbContext> fabrica)
{
    public async Task<List<AniversarioProximo>> ListarAsync()
    {
        var hoje = FusoHorario.Agora().Date;
        await using var db = await fabrica.CreateDbContextAsync();
        var todos = await db.Aniversarios.AsNoTracking().Include(a => a.Tema).ToListAsync();
        return todos.Select(a => Projetar(a, hoje))
            .OrderBy(p => p.DiasRestantes)
            .ThenBy(p => p.Aniversario.Nome)
            .ToList();
    }

    public async Task<List<AniversarioProximo>> ProximosAsync(int dias)
        => (await ListarAsync()).Where(p => p.DiasRestantes <= dias).ToList();

    public async Task SalvarAsync(Aniversario aniversario)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        aniversario.Tema = null; // só o TemaId conta
        aniversario.AtualizadoEm = FusoHorario.Agora();

        if (aniversario.Id == 0)
        {
            aniversario.CriadoEm = aniversario.AtualizadoEm;
            db.Aniversarios.Add(aniversario);
        }
        else
        {
            var existente = await db.Aniversarios.FirstAsync(a => a.Id == aniversario.Id);
            var mudouData = existente.Dia != aniversario.Dia || existente.Mes != aniversario.Mes;
            db.Entry(existente).CurrentValues.SetValues(aniversario);
            if (mudouData)
            {
                existente.UltimoAnoAvisado = null;
                existente.UltimoAnoAvisoAntecipado = null;
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var aniversario = await db.Aniversarios.FindAsync(id);
        if (aniversario == null) return;
        db.Aniversarios.Remove(aniversario);
        await db.SaveChangesAsync();
    }

    public static AniversarioProximo Projetar(Aniversario aniversario, DateTime hoje)
    {
        var data = ProximaOcorrencia(aniversario.Dia, aniversario.Mes, hoje);
        var idade = aniversario.Ano == null ? (int?)null : data.Year - aniversario.Ano.Value;
        return new AniversarioProximo(aniversario, data, (data - hoje).Days, idade);
    }

    /// <summary>Próxima ocorrência a partir da data informada (inclusive).
    /// Quem nasceu em 29/02 é celebrado em 28/02 nos anos não bissextos.</summary>
    public static DateTime ProximaOcorrencia(int dia, int mes, DateTime aPartirDe)
    {
        var data = DataNoAno(dia, mes, aPartirDe.Year);
        if (data < aPartirDe.Date)
            data = DataNoAno(dia, mes, aPartirDe.Year + 1);
        return data;
    }

    private static DateTime DataNoAno(int dia, int mes, int ano)
        => new(ano, mes, Math.Min(dia, DateTime.DaysInMonth(ano, mes)));
}
