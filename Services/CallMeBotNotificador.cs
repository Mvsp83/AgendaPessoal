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
            var corpo = await resposta.Content.ReadAsStringAsync();

            // O CallMeBot responde 200 até em erro; a confirmação real vem no texto
            var texto = System.Text.RegularExpressions.Regex.Replace(corpo, "<[^>]+>", " ");
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\s+", " ").Trim();
            if (texto.Length > 250) texto = texto[..250];

            var confirmado = resposta.IsSuccessStatusCode
                && (texto.Contains("queued", StringComparison.OrdinalIgnoreCase)
                    || texto.Contains("sent", StringComparison.OrdinalIgnoreCase));

            return confirmado
                ? (true, "Enviado")
                : (false, $"CallMeBot HTTP {(int)resposta.StatusCode}: {texto}");
        }
        catch (Exception erro)
        {
            return (false, erro.Message);
        }
    }
}
