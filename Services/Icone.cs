using System.Globalization;
using System.Text;

namespace SpesaCondivisa.Services;

/// <summary>Sceglie un'emoji per l'articolo in base alle parole che contiene, e un colore per ogni persona.</summary>
public static class Icone
{
    // L'ordine conta: vince la prima parola trovata (es. "latte di soia" → 🥛, "gelato" prima di "latte").
    private static readonly (string Parola, string Emoji)[] Mappa =
    [
        ("gelat", "🍨"), ("yogurt", "🥛"), ("latte", "🥛"), ("panna", "🥛"), ("burro", "🧈"),
        ("formagg", "🧀"), ("mozzarell", "🧀"), ("parmigian", "🧀"), ("grana", "🧀"), ("ricotta", "🧀"), ("stracchino", "🧀"),
        ("uova", "🥚"), ("uovo", "🥚"),
        ("pane", "🍞"), ("piadin", "🫓"), ("cracker", "🍘"), ("grissin", "🥖"), ("baguette", "🥖"), ("cornetti", "🥐"), ("brioche", "🥐"),
        ("biscott", "🍪"), ("cioccolat", "🍫"), ("nutella", "🍫"), ("merendin", "🧁"), ("torta", "🍰"), ("caramell", "🍬"),
        ("pasta", "🍝"), ("spaghett", "🍝"), ("penne", "🍝"), ("fusilli", "🍝"), ("riso", "🍚"), ("farina", "🌾"), ("pizza", "🍕"),
        ("pollo", "🍗"), ("tacchino", "🍗"), ("carne", "🥩"), ("bistecc", "🥩"), ("macinat", "🥩"), ("salsicc", "🌭"), ("wurstel", "🌭"),
        ("prosciutto", "🥓"), ("pancetta", "🥓"), ("salame", "🥓"), ("bresaola", "🥓"), ("mortadella", "🥓"),
        ("pesce", "🐟"), ("salmone", "🐟"), ("tonno", "🐟"), ("merluzz", "🐟"), ("gamber", "🦐"), ("cozze", "🦪"),
        ("melanzan", "🍆"), ("mela", "🍎"), ("mele", "🍎"), ("pera", "🍐"), ("pere", "🍐"), ("banan", "🍌"), ("arancia", "🍊"), ("arance", "🍊"), ("mandarin", "🍊"),
        ("limon", "🍋"), ("fragol", "🍓"), ("uva", "🍇"), ("kiwi", "🥝"), ("ananas", "🍍"), ("pesc", "🍑"), ("anguria", "🍉"), ("melone", "🍈"),
        ("ciliegi", "🍒"), ("avocado", "🥑"), ("frutta", "🍎"),
        ("pomodor", "🍅"), ("passata", "🍅"), ("insalata", "🥬"), ("lattuga", "🥬"), ("spinac", "🥬"), ("zucchin", "🥒"), ("cetriol", "🥒"),
        ("carot", "🥕"), ("patat", "🥔"), ("cipoll", "🧅"), ("aglio", "🧄"), ("peperon", "🫑"),("broccol", "🥦"),
        ("funghi", "🍄"), ("mais", "🌽"), ("piselli", "🫛"), ("fagiol", "🫘"), ("ceci", "🫘"), ("lenticch", "🫘"), ("verdur", "🥦"),
        ("olio", "🫒"), ("olive", "🫒"), ("sale", "🧂"), ("pepe", "🧂"), ("zucchero", "🍬"), ("miele", "🍯"), ("marmellat", "🍯"),
        ("caffe", "☕"), ("caffè", "☕"), (" te ", "🍵"), ("tisan", "🍵"), ("cereali", "🥣"), ("muesli", "🥣"),
        ("acqua", "💧"), ("birra", "🍺"), ("vino", "🍷"), ("prosecco", "🥂"), ("succo", "🧃"), ("coca", "🥤"), ("bibit", "🥤"),
        ("surgelat", "🧊"), ("ghiaccio", "🧊"),
        ("carta igienica", "🧻"), ("scottex", "🧻"), ("tovagliol", "🧻"), ("fazzolett", "🤧"),
        ("detersiv", "🧴"), ("ammorbident", "🧴"), ("candeggin", "🧴"), ("sgrassator", "🧴"), ("piatti", "🍽️"), ("lavastoviglie", "🍽️"),
        ("spugn", "🧽"), ("sacchetti", "🛍️"), ("spazzatura", "🗑️"), ("pellicola", "📜"), ("alluminio", "📜"),
        ("shampoo", "🧴"), ("bagnoschiuma", "🧼"), ("sapone", "🧼"), ("dentifricio", "🪥"), ("spazzolin", "🪥"), ("deodorant", "🧴"),
        ("rasoi", "🪒"), ("assorbent", "🩹"), ("cerott", "🩹"), ("medicin", "💊"), ("tachipirina", "💊"), ("crema", "🧴"),
        ("pannolin", "👶"), ("croccantin", "🐾"), ("cane", "🐶"), ("gatto", "🐱"), ("lettiera", "🐱"),
        ("lampadin", "💡"), ("pile", "🔋"), ("batteri", "🔋"), ("fiori", "💐"),
    ];

    public static string Per(string testo)
    {
        var t = " " + SenzaAccenti(testo.ToLowerInvariant()) + " ";
        foreach (var (parola, emoji) in Mappa)
        {
            if (t.Contains(SenzaAccenti(parola))) return emoji;
        }
        return "🛒";
    }

    /// <summary>Tinta (0-360) stabile per ogni nome, per il pallino con l'iniziale.</summary>
    public static int Tinta(string nome)
    {
        var h = 0;
        foreach (var c in nome.Trim().ToLowerInvariant()) h = (h * 31 + c) % 360;
        return h;
    }

    public static string Iniziale(string nome) =>
        string.IsNullOrWhiteSpace(nome) ? "?" : nome.Trim()[..1].ToUpperInvariant();

    public static string SenzaAccenti(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString();
    }
}
