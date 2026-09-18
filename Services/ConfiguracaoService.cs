using AgendaPessoal.Data;
using AgendaPessoal.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPessoal.Services;

public class ConfiguracaoService(IDbContextFactory<AgendaDbContext> fabrica)
{
    public const string CallMeBotTelefone = "callmebot_telefone";
    public const string CallMeBotApiKey = "callmebot_apikey";
    public const string TokenVerificacao = "token_verificacao";
    public const string LoginUsuario = "login_usuario";
    public const string LoginSenhaHash = "login_senha_hash";
    public const string LoginFalhas = "login_falhas";
    public const string LoginBloqueioAte = "login_bloqueio_ate";

    public async Task<string?> ObterAsync(string chave)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return (await db.Configuracoes.FindAsync(chave))?.Valor;
    }

    public async Task SalvarAsync(string chave, string? valor)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var configuracao = await db.Configuracoes.FindAsync(chave);
        if (configuracao == null)
            db.Configuracoes.Add(new Configuracao { Chave = chave, Valor = valor });
        else
            configuracao.Valor = valor;
        await db.SaveChangesAsync();
    }
}
