using System.Globalization;

namespace AgendaPessoal.Services;

/// <summary>Conta única do dono do app. Usuário e hash da senha ficam na tabela de configurações.</summary>
public class ContaService(ConfiguracaoService config)
{
    // Bloqueio por conta: após esse número de falhas seguidas, cada nova falha bloqueia o login
    // por um tempo crescente (backoff). Independe de IP, então não é burlável por spoof de X-Forwarded-For.
    private const int TentativasAntesDeBloquear = 5;
    private const double MaximoMinutosBloqueio = 15;

    /// <summary>Tempo restante de bloqueio do login, ou null se não está bloqueado.</summary>
    public async Task<TimeSpan?> TempoDeBloqueioRestanteAsync()
    {
        var ate = await config.ObterAsync(ConfiguracaoService.LoginBloqueioAte);
        if (string.IsNullOrEmpty(ate)
            || !DateTime.TryParse(ate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var quando))
            return null;
        var restante = quando - DateTime.UtcNow;
        return restante > TimeSpan.Zero ? restante : null;
    }

    /// <summary>Zera o contador de falhas após um login bem-sucedido.</summary>
    public async Task RegistrarSucessoAsync()
    {
        await config.SalvarAsync(ConfiguracaoService.LoginFalhas, "0");
        await config.SalvarAsync(ConfiguracaoService.LoginBloqueioAte, null);
    }

    /// <summary>Conta mais uma falha e, a partir do limite, aplica bloqueio temporário com backoff exponencial.</summary>
    public async Task RegistrarFalhaAsync()
    {
        int.TryParse(await config.ObterAsync(ConfiguracaoService.LoginFalhas), out var falhas);
        falhas++;
        await config.SalvarAsync(ConfiguracaoService.LoginFalhas, falhas.ToString(CultureInfo.InvariantCulture));

        if (falhas >= TentativasAntesDeBloquear)
        {
            var excedente = falhas - TentativasAntesDeBloquear; // 0,1,2,... -> 1,2,4,8,15,15... minutos
            var minutos = Math.Min(Math.Pow(2, excedente), MaximoMinutosBloqueio);
            var ate = DateTime.UtcNow.AddMinutes(minutos);
            await config.SalvarAsync(ConfiguracaoService.LoginBloqueioAte,
                ate.ToString("o", CultureInfo.InvariantCulture));
        }
    }

    public async Task<bool> TemUsuarioAsync()
        => !string.IsNullOrEmpty(await config.ObterAsync(ConfiguracaoService.LoginUsuario));

    public async Task<string?> ObterUsuarioAsync()
        => await config.ObterAsync(ConfiguracaoService.LoginUsuario);

    public async Task CriarAsync(string usuario, string senha)
    {
        await config.SalvarAsync(ConfiguracaoService.LoginUsuario, usuario);
        await config.SalvarAsync(ConfiguracaoService.LoginSenhaHash, SenhaHasher.Gerar(senha));
    }

    public async Task<bool> ValidarAsync(string usuario, string senha)
    {
        var usuarioSalvo = await config.ObterAsync(ConfiguracaoService.LoginUsuario);
        var hashSalvo = await config.ObterAsync(ConfiguracaoService.LoginSenhaHash);
        if (string.IsNullOrEmpty(usuarioSalvo) || string.IsNullOrEmpty(hashSalvo))
            return false;
        return string.Equals(usuario, usuarioSalvo, StringComparison.OrdinalIgnoreCase)
               && SenhaHasher.Verificar(senha, hashSalvo);
    }

    public async Task<bool> TrocarSenhaAsync(string senhaAtual, string novaSenha)
    {
        var hashSalvo = await config.ObterAsync(ConfiguracaoService.LoginSenhaHash);
        if (string.IsNullOrEmpty(hashSalvo) || !SenhaHasher.Verificar(senhaAtual, hashSalvo))
            return false;
        await config.SalvarAsync(ConfiguracaoService.LoginSenhaHash, SenhaHasher.Gerar(novaSenha));
        return true;
    }
}
