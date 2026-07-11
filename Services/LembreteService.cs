using AgendaPessoal.Data;
using AgendaPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPessoal.Services;

public class LembreteService(IDbContextFactory<AgendaDbContext> fabrica, INotificador notificador)
{
    /// <summary>
    /// Dispara os lembretes vencidos. Chamado pelo serviço de fundo a cada minuto
    /// e pelo endpoint /api/lembretes/verificar (cron externo em hospedagem gratuita).
    /// </summary>
    public async Task<int> VerificarEDispararAsync()
    {
        var agora = FusoHorario.Agora();
        await using var db = await fabrica.CreateDbContextAsync();
        var candidatos = await db.Itens
            .Where(i => i.Status == StatusItem.Ativo && i.LembreteAtivo && i.DataHora != null)
            .ToListAsync();

        var enviados = 0;
        foreach (var item in candidatos)
        {
            var momentoDoAviso = item.DataHora!.Value.AddMinutes(-item.AvisarMinutosAntes);
            if (agora < momentoDoAviso)
                continue;
            if (item.UltimoLembreteEnviado != null && item.UltimoLembreteEnviado >= momentoDoAviso)
                continue;

            var (sucesso, detalhe) = await notificador.EnviarAsync(MontarMensagem(item));
            db.Notificacoes.Add(new Notificacao
            {
                ItemId = item.Id,
                TituloItem = item.Titulo,
                Canal = notificador.Canal,
                EnviadaEm = agora,
                Sucesso = sucesso,
                Detalhe = detalhe
            });
            if (sucesso) enviados++;

            // Marca como tratado mesmo em falha, para não repetir a cada minuto;
            // o histórico em Configurações registra o erro.
            item.UltimoLembreteEnviado = agora;
            if (item.Recorrencia != Recorrencia.Nenhuma)
            {
                item.DataHora = ProximaOcorrencia(item.DataHora.Value, item.Recorrencia, agora);
                item.UltimoLembreteEnviado = null;
            }
        }
        enviados += await VerificarAniversariosAsync(db, agora);
        await db.SaveChangesAsync();
        return enviados;
    }

    private async Task<int> VerificarAniversariosAsync(AgendaDbContext db, DateTime agora)
    {
        var aniversarios = await db.Aniversarios
            .Where(a => a.LembreteAtivo)
            .ToListAsync();

        var enviados = 0;
        foreach (var aniversario in aniversarios)
        {
            if (agora.Hour < aniversario.HoraAviso)
                continue;

            var proxima = AniversarioService.ProximaOcorrencia(aniversario.Dia, aniversario.Mes, agora);
            var idade = aniversario.Ano == null ? (int?)null : proxima.Year - aniversario.Ano.Value;

            string? mensagem = null;
            var ehAvisoAntecipado = false;

            if (proxima == agora.Date && aniversario.UltimoAnoAvisado != proxima.Year)
            {
                mensagem = $"🎂 Hoje é aniversário de *{aniversario.Nome}*"
                           + (idade != null ? $" ({idade} anos)" : "") + "!";
            }
            else if (aniversario.DiasAntecedencia > 0
                     && proxima > agora.Date
                     && proxima <= agora.Date.AddDays(aniversario.DiasAntecedencia)
                     && aniversario.UltimoAnoAvisoAntecipado != proxima.Year)
            {
                var faltam = (proxima - agora.Date).Days;
                mensagem = $"🎁 Aniversário de *{aniversario.Nome}* em {faltam} dia(s) — {proxima:dd/MM}"
                           + (idade != null ? $" (fará {idade} anos)" : "") + ".";
                ehAvisoAntecipado = true;
            }

            if (mensagem == null)
                continue;

            if (!string.IsNullOrWhiteSpace(aniversario.Observacoes))
            {
                var observacoes = aniversario.Observacoes.Trim();
                if (observacoes.Length > 200)
                    observacoes = observacoes[..200] + "...";
                mensagem += $"\n📝 {observacoes}";
            }

            var (sucesso, detalhe) = await notificador.EnviarAsync(mensagem);
            db.Notificacoes.Add(new Notificacao
            {
                TituloItem = $"🎂 {aniversario.Nome}",
                Canal = notificador.Canal,
                EnviadaEm = agora,
                Sucesso = sucesso,
                Detalhe = detalhe
            });
            if (sucesso) enviados++;

            // marca a ocorrência anual como tratada mesmo em falha (o histórico registra o erro)
            if (ehAvisoAntecipado)
                aniversario.UltimoAnoAvisoAntecipado = proxima.Year;
            else
                aniversario.UltimoAnoAvisado = proxima.Year;
        }
        return enviados;
    }

    public async Task<List<Notificacao>> ListarNotificacoesAsync(int quantidade = 20)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Notificacoes.AsNoTracking()
            .OrderByDescending(n => n.EnviadaEm)
            .Take(quantidade)
            .ToListAsync();
    }

    public static DateTime ProximaOcorrencia(DateTime atual, Recorrencia recorrencia, DateTime depoisDe)
    {
        var proxima = atual;
        while (proxima <= depoisDe)
        {
            proxima = recorrencia switch
            {
                Recorrencia.Diaria => proxima.AddDays(1),
                Recorrencia.Semanal => proxima.AddDays(7),
                Recorrencia.Quinzenal => proxima.AddDays(14),
                Recorrencia.Mensal => proxima.AddMonths(1),
                Recorrencia.Anual => proxima.AddYears(1),
                _ => depoisDe.AddDays(1)
            };
        }
        return proxima;
    }

    private static string MontarMensagem(Item item)
    {
        var texto = $"⏰ *{item.Titulo}*\n🗓 {item.DataHora:dd/MM/yyyy HH:mm}";
        if (!string.IsNullOrWhiteSpace(item.Conteudo))
        {
            var resumo = item.Conteudo.Trim();
            if (resumo.Length > 300)
                resumo = resumo[..300] + "...";
            texto += $"\n\n{resumo}";
        }
        return texto;
    }
}
