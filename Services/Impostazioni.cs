using Microsoft.JSInterop;

namespace SpesaCondivisa.Services;

/// <summary>Nome e codice lista, salvati nel localStorage del telefono.</summary>
public class Impostazioni(IJSRuntime js)
{
    public string Nome { get; private set; } = "";
    public string Codice { get; private set; } = "";
    public bool Completa => Nome.Length > 0 && Codice.Length > 0;

    public async Task CaricaAsync()
    {
        Nome = await js.InvokeAsync<string?>("localStorage.getItem", "spesa.nome") ?? "";
        Codice = await js.InvokeAsync<string?>("localStorage.getItem", "spesa.codice") ?? "";
    }

    public async Task SalvaAsync(string nome, string codice)
    {
        Nome = nome.Trim();
        Codice = NormalizzaCodice(codice);
        await js.InvokeVoidAsync("localStorage.setItem", "spesa.nome", Nome);
        await js.InvokeVoidAsync("localStorage.setItem", "spesa.codice", Codice);
    }

    /// <summary>Minuscolo, solo lettere, cifre e trattini (Firebase non accetta . $ # [ ] /).</summary>
    public static string NormalizzaCodice(string codice) =>
        new string(codice.Trim().ToLowerInvariant().Where(c => char.IsAsciiLetterOrDigit(c) || c == '-').ToArray());

    public static string GeneraCodice()
    {
        const string caratteri = "abcdefghjkmnpqrstuvwxyz23456789";
        var parti = Enumerable.Range(0, 3).Select(_ =>
            new string(Enumerable.Range(0, 4).Select(_ => caratteri[Random.Shared.Next(caratteri.Length)]).ToArray()));
        return string.Join("-", parti);
    }
}
