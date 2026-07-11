using System.Security.Cryptography;

namespace AgendaPessoal.Services;

/// <summary>PBKDF2-SHA256 com sal aleatório. Formato armazenado: "iterações.salBase64.hashBase64".</summary>
public static class SenhaHasher
{
    private const int Iteracoes = 100_000;

    public static string Gerar(string senha)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, 32);
        return $"{Iteracoes}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string armazenado)
    {
        var partes = armazenado.Split('.');
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteracoes))
            return false;
        try
        {
            var sal = Convert.FromBase64String(partes[1]);
            var esperado = Convert.FromBase64String(partes[2]);
            var hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(hash, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
