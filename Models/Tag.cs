namespace AgendaPessoal.Models;

public class Tag
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public List<Item> Itens { get; set; } = new();
}
