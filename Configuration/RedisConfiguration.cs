namespace ParcialProgramacion.Configuration;

public static class RedisConfiguration
{
    /// <summary>
    /// StackExchange.Redis no interpreta una URI redis:// / rediss:// completa como cadena de
    /// configuracion: la toma literalmente como endpoint y termina connectando a host:puerto:0.
    /// Redis Cloud, Upstash y Render entregan justamente ese formato, asi que aqui se traduce
    /// al formato nativo "host:puerto,user=,password=,ssl=" que sientiende el cliente.
    /// Si la cadena ya viene en formato nativo, se devuelve sin tocar.
    /// </summary>
    public static string Normalizar(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        var valor = connectionString.Trim();

        if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri))
        {
            return valor;
        }

        if (uri.Scheme is not ("redis" or "rediss"))
        {
            return valor;
        }

        var partes = new List<string> { $"{uri.Host}:{uri.Port}" };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var credenciales = uri.UserInfo.Split(':', 2);

            if (credenciales[0].Length > 0)
            {
                partes.Add($"user={Uri.UnescapeDataString(credenciales[0])}");
            }

            if (credenciales.Length > 1 && credenciales[1].Length > 0)
            {
                partes.Add($"password={Uri.UnescapeDataString(credenciales[1])}");
            }
        }

        if (uri.Scheme == "rediss")
        {
            partes.Add("ssl=True");
        }

        var baseDeDatos = uri.AbsolutePath.Trim('/');
        if (baseDeDatos.Length > 0 && int.TryParse(baseDeDatos, out var indice))
        {
            partes.Add($"defaultDatabase={indice}");
        }

        return string.Join(",", partes);
    }
}
