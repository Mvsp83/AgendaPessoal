namespace AgendaPessoal.Models;

/// <summary>
/// Aniversário de uma pessoa. Dia/mês obrigatórios; ano opcional (permite calcular a idade).
/// Os campos UltimoAnoAvisado* impedem aviso duplicado dentro da mesma ocorrência anual.
/// </summary>
public class Aniversario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int Dia { get; set; } = 1;
    public int Mes { get; set; } = 1;
    public int? Ano { get; set; }

    public int? TemaId { get; set; }
    public Tema? Tema { get; set; }
    public string? Observacoes { get; set; }

    public bool LembreteAtivo { get; set; } = true;
    /// <summary>Hora do dia (0-23) a partir da qual o aviso é enviado.</summary>
    public int HoraAviso { get; set; } = 8;
    /// <summary>Dias de antecedência do aviso extra (0 = sem aviso antecipado).</summary>
    public int DiasAntecedencia { get; set; }

    public int? UltimoAnoAvisado { get; set; }
    public int? UltimoAnoAvisoAntecipado { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
