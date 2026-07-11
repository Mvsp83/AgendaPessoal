namespace AgendaPessoal.Services;

/// <summary>
/// Verifica lembretes vencidos a cada minuto enquanto o app estiver de pé.
/// Em hospedagem gratuita que hiberna, o cron externo cobre os períodos de sono.
/// </summary>
public class LembreteBackgroundService(LembreteService lembretes, ILogger<LembreteBackgroundService> registrador)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        try
        {
            // espera o app subir e o banco ser criado
            await Task.Delay(TimeSpan.FromSeconds(10), parada);
            using var relogio = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (!parada.IsCancellationRequested)
            {
                try
                {
                    await lembretes.VerificarEDispararAsync();
                }
                catch (Exception erro)
                {
                    registrador.LogError(erro, "Erro ao verificar lembretes");
                }
                await relogio.WaitForNextTickAsync(parada);
            }
        }
        catch (OperationCanceledException)
        {
            // encerramento normal do app
        }
    }
}
