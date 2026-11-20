using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SpesaCondivisa.Services;

public class Articolo
{
    [JsonIgnore] public string Id { get; set; } = "";
    [JsonPropertyName("testo")] public string Testo { get; set; } = "";
    [JsonPropertyName("preso")] public bool Preso { get; set; }
    [JsonPropertyName("aggiuntoDa")] public string AggiuntoDa { get; set; } = "";
    [JsonPropertyName("presoDa")] public string? PresoDa { get; set; }
    [JsonPropertyName("creato")] public long Creato { get; set; }
}

public class ModificaPreso
{
    [JsonPropertyName("preso")] public bool Preso { get; set; }
    // Scritto anche quando è null: per Firebase null significa "cancella il campo".
    [JsonPropertyName("presoDa")] public string? PresoDa { get; set; }
}

public class NuovoId
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

// JSON generato in compilazione: funziona anche con il trimming della build Release.
[JsonSerializable(typeof(Dictionary<string, Articolo>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(ModificaPreso))]
[JsonSerializable(typeof(NuovoId))]
public partial class JsonFirebase : JsonSerializerContext;

/// <summary>
/// Legge e scrive la lista su Firebase Realtime Database tramite le API REST.
/// Struttura: /liste/{codice}/articoli/{id}
/// </summary>
public class ListaService(HttpClient http, IConfiguration config)
{
    private readonly string _baseUrl = (config["FirebaseUrl"] ?? "").Trim().TrimEnd('/');

    public bool Configurato =>
        _baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
        !_baseUrl.Contains("INSERISCI", StringComparison.OrdinalIgnoreCase);

    private string Url(string codice, string? id = null) =>
        id is null
            ? $"{_baseUrl}/liste/{Uri.EscapeDataString(codice)}/articoli.json"
            : $"{_baseUrl}/liste/{Uri.EscapeDataString(codice)}/articoli/{Uri.EscapeDataString(id)}.json";

    public async Task<List<Articolo>> CaricaAsync(string codice)
    {
        var dati = await http.GetFromJsonAsync(Url(codice), JsonFirebase.Default.DictionaryStringArticolo);
        if (dati is null) return [];
        foreach (var (id, a) in dati) a.Id = id;
        return dati.Values.ToList();
    }

    public async Task<Articolo> AggiungiAsync(string codice, string testo, string nome)
    {
        var a = new Articolo
        {
            Testo = testo,
            AggiuntoDa = nome,
            Creato = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        var risposta = await http.PostAsJsonAsync(Url(codice), a, JsonFirebase.Default.Articolo);
        risposta.EnsureSuccessStatusCode();
        var creato = await risposta.Content.ReadFromJsonAsync(JsonFirebase.Default.NuovoId);
        a.Id = creato!.Name;
        return a;
    }

    public async Task ImpostaPresoAsync(string codice, string id, bool preso, string nome)
    {
        var modifica = new ModificaPreso { Preso = preso, PresoDa = preso ? nome : null };
        var risposta = await http.PatchAsJsonAsync(Url(codice, id), modifica, JsonFirebase.Default.ModificaPreso);
        risposta.EnsureSuccessStatusCode();
    }

    public async Task EliminaAsync(string codice, string id)
    {
        var risposta = await http.DeleteAsync(Url(codice, id));
        risposta.EnsureSuccessStatusCode();
    }

    public async Task EliminaTantiAsync(string codice, IEnumerable<string> ids)
    {
        // Un solo PATCH con valori null cancella tutti gli articoli indicati.
        var modifica = ids.ToDictionary(id => id, _ => (string?)null);
        if (modifica.Count == 0) return;
        var risposta = await http.PatchAsJsonAsync(Url(codice), modifica, JsonFirebase.Default.DictionaryStringString);
        risposta.EnsureSuccessStatusCode();
    }
}
