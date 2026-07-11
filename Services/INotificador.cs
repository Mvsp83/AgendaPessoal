namespace AgendaPessoal.Services;

public interface INotificador
{
    string Canal { get; }
    Task<(bool Sucesso, string Detalhe)> EnviarAsync(string mensagem);
}
