namespace AgendaPessoal.Models;

public class Tema
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Cor { get; set; } = "#1E88E5";
    public List<Item> Itens { get; set; } = new();
}
