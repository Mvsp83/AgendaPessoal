namespace AgendaPessoal.Models;

/// <summary>
/// Histórico de lembretes enviados (ou tentativas com falha).
/// Sem FK para Item: o histórico permanece mesmo se o item for excluído.
/// </summary>
public class Notificacao
{
    public int Id { get; set; }
    public int? ItemId { get; set; }
    public string TituloItem { get; set; } = "";
    public string Canal { get; set; } = "";
    public DateTime EnviadaEm { get; set; }
    public bool Sucesso { get; set; }
    public string? Detalhe { get; set; }
}
