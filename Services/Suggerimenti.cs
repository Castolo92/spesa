namespace SpesaCondivisa.Services;

public record Suggerimento(string Testo, string? Chiave, int Volte);

/// <summary>Propone articoli mentre si scrive: prima quelli già usati da voi, poi un elenco di articoli comuni.</summary>
public static class Suggerimenti
{
    // Elenco base, così i suggerimenti funzionano anche quando lo storico è ancora vuoto.
    private static readonly string[] Comuni =
    [
        "Acqua", "Aceto", "Aglio", "Albicocche", "Alluminio", "Ammorbidente", "Ananas", "Arance", "Assorbenti", "Avocado",
        "Bagnoschiuma", "Banane", "Basilico", "Biscotti", "Birra", "Bresaola", "Brioche", "Broccoli", "Burro",
        "Caffè", "Candeggina", "Carne macinata", "Carote", "Carta forno", "Carta igienica", "Cereali", "Ceci", "Cetrioli",
        "Cioccolato", "Cipolle", "Coca Cola", "Cotton fioc", "Cracker", "Crema mani",
        "Dentifricio", "Deodorante", "Detersivo lavatrice", "Detersivo piatti", "Farina", "Fagioli", "Fazzoletti",
        "Formaggio grattugiato", "Fragole", "Funghi", "Gelato", "Grissini", "Insalata", "Kiwi",
        "Latte", "Lenticchie", "Lievito", "Limoni", "Mais", "Mandarini", "Marmellata", "Mele", "Melanzane", "Merluzzo",
        "Miele", "Mozzarella", "Nutella", "Olio di semi", "Olio extravergine", "Olive", "Pane", "Pane in cassetta",
        "Panna", "Parmigiano", "Passata di pomodoro", "Pasta", "Patate", "Pepe", "Peperoni", "Pere", "Pesce",
        "Petto di pollo", "Piadine", "Piselli", "Pizza surgelata", "Pollo", "Pomodori", "Prosciutto cotto",
        "Prosciutto crudo", "Rasoi", "Ricotta", "Riso", "Rucola", "Sacchetti spazzatura", "Sale", "Salame", "Salmone",
        "Salsiccia", "Sapone mani", "Scottex", "Sgrassatore", "Shampoo", "Spinaci", "Spugne", "Stracchino", "Succo di frutta",
        "Tonno", "Tovaglioli", "Uova", "Uva", "Vino", "Wurstel", "Yogurt", "Zucchero", "Zucchine",
    ];

    /// <summary>Fino a <paramref name="quanti"/> suggerimenti per il testo scritto.</summary>
    public static List<Suggerimento> PerTesto(string scritto, IEnumerable<VoceStorico> storico,
        IEnumerable<string> giaInLista, int quanti = 5)
    {
        var q = Normalizza(scritto);
        if (q.Length == 0) return [];

        var esclusi = giaInLista.Select(Normalizza).ToHashSet();
        var visti = new HashSet<string>();
        var risultati = new List<(Suggerimento S, int Punti)>();

        void Valuta(string testo, string? chiave, int volte)
        {
            var n = Normalizza(testo);
            if (n == q || esclusi.Contains(n) || !visti.Add(n)) return;

            int punti;
            if (n.StartsWith(q)) punti = 3;                                   // "lat" → Latte
            else if (n.Split(' ').Any(p => p.StartsWith(q))) punti = 2;       // "soia" → Latte di soia
            else if (chiave is not null && q.Length >= 3 && n.Contains(q)) punti = 1; // solo per le voci già usate
            else return;

            // Le voci già usate valgono di più, e di più ancora se usate spesso.
            var peso = chiave is null ? punti * 1000 : punti * 1000 + 500 + Math.Min(volte, 400);
            risultati.Add((new Suggerimento(testo, chiave, volte), peso));
        }

        foreach (var v in storico) Valuta(v.Testo, v.Chiave, v.Volte);
        foreach (var c in Comuni) Valuta(c, null, 0);

        return risultati
            .OrderByDescending(r => r.Punti)
            .ThenBy(r => r.S.Testo.Length)
            .Take(quanti)
            .Select(r => r.S)
            .ToList();
    }

    /// <summary>Gli articoli comprati più spesso che ora non sono in lista.</summary>
    public static List<VoceStorico> Frequenti(IEnumerable<VoceStorico> storico, IEnumerable<string> giaInLista, int quanti = 10)
    {
        var esclusi = giaInLista.Select(Normalizza).ToHashSet();
        return storico
            .Where(v => v.Volte >= 2 && !esclusi.Contains(Normalizza(v.Testo)))
            .OrderByDescending(v => v.Volte)
            .ThenByDescending(v => v.Ultimo)
            .Take(quanti)
            .ToList();
    }

    public static string Normalizza(string s) =>
        string.Join(' ', Icone.SenzaAccenti(s.ToLowerInvariant()).Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
