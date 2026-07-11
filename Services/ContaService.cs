namespace AgendaPessoal.Services;

/// <summary>Conta única do dono do app. Usuário e hash da senha ficam na tabela de configurações.</summary>
public class ContaService(ConfiguracaoService config)
{
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
