namespace AgendaPessoal.Services;

/// <summary>
/// Envia WhatsApp para o próprio número via CallMeBot (gratuito, uso pessoal).
/// Telefone e API key ficam na tabela de configurações, editáveis pela tela Configurações.
/// </summary>
public class CallMeBotNotificador(IHttpClientFactory fabricaHttp, ConfiguracaoService config) : INotificador
{
    public string Canal => "WhatsApp (CallMeBot)";

    public async Task<(bool Sucesso, string Detalhe)> EnviarAsync(string mensagem)
    {
        var telefone = await config.ObterAsync(ConfiguracaoService.CallMeBotTelefone);
        var apiKey = await config.ObterAsync(ConfiguracaoService.CallMeBotApiKey);
        if (string.IsNullOrWhiteSpace(telefone) || string.IsNullOrWhiteSpace(apiKey))
            return (false, "CallMeBot não configurado: preencha telefone e API key em Configurações.");

        var url = "https://api.callmebot.com/whatsapp.php" +
                  $"?phone={Uri.EscapeDataString(telefone)}" +
                  $"&text={Uri.EscapeDataString(mensagem)}" +
                  $"&apikey={Uri.EscapeDataString(apiKey)}";
        try
        {
            var http = fabricaHttp.CreateClient("notificador");
            var resposta = await http.GetAsync(url);
            if (resposta.IsSuccessStatusCode)
                return (true, "Enviado");

            var corpo = await resposta.Content.ReadAsStringAsync();
            if (corpo.Length > 200) corpo = corpo[..200];
            return (false, $"HTTP {(int)resposta.StatusCode}: {corpo}");
        }
        catch (Exception erro)
        {
            return (false, erro.Message);
        }
    }
}
