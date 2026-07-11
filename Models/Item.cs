namespace AgendaPessoal.Models;

/// <summary>
/// Registro único e flexível: nota, lembrete ou tarefa.
/// O que muda é ter ou não data/hora e lembrete ativado.
/// </summary>
public class Item
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Conteudo { get; set; }

    public int? TemaId { get; set; }
    public Tema? Tema { get; set; }
    public List<Tag> Tags { get; set; } = new();

    public DateTime? DataHora { get; set; }
    public Recorrencia Recorrencia { get; set; }
    public bool LembreteAtivo { get; set; }
    public int AvisarMinutosAntes { get; set; }
    public DateTime? UltimoLembreteEnviado { get; set; }

    public StatusItem Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
