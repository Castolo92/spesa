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

/// <summary>Un articolo già preso che torna in lista: conta come aggiunto adesso.</summary>
public class Rimessa
{
    [JsonPropertyName("preso")] public bool Preso { get; set; }
    [JsonPropertyName("presoDa")] public string? PresoDa { get; set; }
    [JsonPropertyName("aggiuntoDa")] public string AggiuntoDa { get; set; } = "";
    [JsonPropertyName("creato")] public long Creato { get; set; }
}

/// <summary>Quante volte un articolo è stato aggiunto: serve per i suggerimenti.</summary>
public class VoceStorico
{
    [JsonIgnore] public string Chiave { get; set; } = "";
    [JsonPropertyName("testo")] public string Testo { get; set; } = "";
    [JsonPropertyName("volte")] public int Volte { get; set; }
    [JsonPropertyName("ultimo")] public long Ultimo { get; set; }
}

public class NuovoId
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

// JSON generato in compilazione: funziona anche con il trimming della build Release.
[JsonSerializable(typeof(Dictionary<string, Articolo>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(ModificaPreso))]
[JsonSerializable(typeof(Rimessa))]
[JsonSerializable(typeof(NuovoId))]
[JsonSerializable(typeof(Dictionary<string, VoceStorico>))]
public partial class JsonFirebase : JsonSerializerContext;

/// <summary>
/// Legge e scrive la lista su Firebase Realtime Database tramite le API REST.
/// Struttura: /liste/{codice}/articoli/{id} e /liste/{codice}/storico/{chiave}
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

    public async Task RimettiInListaAsync(string codice, Articolo a, string nome)
    {
        var modifica = new Rimessa { Preso = false, PresoDa = null, AggiuntoDa = nome, Creato = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        var risposta = await http.PatchAsJsonAsync(Url(codice, a.Id), modifica, JsonFirebase.Default.Rimessa);
        risposta.EnsureSuccessStatusCode();
        a.Preso = false;
        a.PresoDa = null;
        a.AggiuntoDa = nome;
        a.Creato = modifica.Creato;
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

    // ---------- Storico per i suggerimenti ----------

    private string UrlStorico(string codice, string? chiave = null) =>
        chiave is null
            ? $"{_baseUrl}/liste/{Uri.EscapeDataString(codice)}/storico.json"
            : $"{_baseUrl}/liste/{Uri.EscapeDataString(codice)}/storico/{Uri.EscapeDataString(chiave)}.json";

    public async Task<List<VoceStorico>> CaricaStoricoAsync(string codice)
    {
        var dati = await http.GetFromJsonAsync(UrlStorico(codice), JsonFirebase.Default.DictionaryStringVoceStorico);
        if (dati is null) return [];
        foreach (var (chiave, v) in dati) v.Chiave = chiave;
        return dati.Values.ToList();
    }

    /// <summary>Registra un'aggiunta. Restituisce la voce aggiornata.</summary>
    public async Task<VoceStorico> RegistraStoricoAsync(string codice, string testo, int volteFinora)
    {
        var v = new VoceStorico
        {
            Chiave = ChiaveStorico(testo),
            Testo = testo,
            Volte = volteFinora + 1,
            Ultimo = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        var risposta = await http.PutAsJsonAsync(UrlStorico(codice, v.Chiave), v, JsonFirebase.Default.VoceStorico);
        risposta.EnsureSuccessStatusCode();
        return v;
    }

    public async Task DimenticaAsync(string codice, string chiave)
    {
        var risposta = await http.DeleteAsync(UrlStorico(codice, chiave));
        risposta.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Chiave Firebase per un testo: minuscolo e spazi normalizzati, così "Latte" e "latte " sono la stessa voce.
    /// Firebase non accetta . $ # [ ] / nelle chiavi.
    /// </summary>
    public static string ChiaveStorico(string testo)
    {
        var parole = testo.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chiave = string.Join(' ', parole);
        foreach (var c in ".$#[]/") chiave = chiave.Replace(c, '_');
        return chiave.Length > 0 ? chiave : "_";
    }
}
