# Spesa condivisa (Blazor WebAssembly)

Una lista della spesa condivisa tra due iPhone, scritta in C# con Blazor WebAssembly.
È una **web app (PWA)**: si apre da Safari e si aggiunge alla schermata Home, dove si comporta come un'app.
Non serve l'App Store, né un Mac, né un account sviluppatore Apple. **Costo totale: 0 €.**

- **Dati**: Firebase Realtime Database (piano gratuito "Spark", nessuna carta di credito)
- **Hosting**: GitHub Pages (gratuito), pubblicato in automatico da GitHub Actions

Per mettere l'app online ci vogliono circa 20 minuti, e si fa una volta sola:

1. Crea il database Firebase (5 min)
2. Inserisci l'indirizzo del database nell'app (1 min)
3. Pubblica l'app su GitHub Pages (10 min)
4. Installala sui due iPhone (2 min ciascuno)

---

## 1. Crea il database Firebase

1. Vai su <https://console.firebase.google.com> ed entra con un account Google.
2. **Crea un progetto** (es. `spesa-casa`). Google Analytics non serve: puoi disattivarlo.
3. Nel menu a sinistra: **Build → Realtime Database → Crea database**.
4. Come posizione scegli **Belgio (europe-west1)**, poi **Avvia in modalità bloccata**.
5. Apri la scheda **Regole**, sostituisci tutto con il testo qui sotto e premi **Pubblica**:

```json
{
  "rules": {
    "liste": {
      "$codice": {
        ".read": true,
        ".write": true,
        ".validate": "$codice.length >= 6"
      }
    }
  }
}
```

   Con queste regole nessuno può elencare le liste esistenti: si può leggere o scrivere una lista solo se se ne conosce il codice.

6. Torna nella scheda **Dati** e copia l'indirizzo in alto, simile a
   `https://spesa-casa-default-rtdb.europe-west1.firebasedatabase.app`.

## 2. Inserisci l'indirizzo nell'app

Apri `wwwroot/appsettings.json` e incolla il tuo indirizzo:

```json
{
  "FirebaseUrl": "https://spesa-casa-default-rtdb.europe-west1.firebasedatabase.app"
}
```

Prova in locale (facoltativo): dalla cartella del progetto esegui `dotnet run` e apri l'indirizzo che compare (es. `http://localhost:5167`).

## 3. Pubblica su GitHub Pages

1. Crea un account gratuito su <https://github.com>.
2. Crea un **nuovo repository pubblico**, ad esempio `spesa` (non aggiungere README né altri file).
   GitHub Pages è gratuito solo per i repository pubblici.
3. Nel repository vai su **Settings → Pages** e in **Source** scegli **GitHub Actions**.
4. Carica il progetto. Dalla cartella del progetto, in PowerShell:

   ```powershell
   git init -b main
   git add .
   git commit -m "Prima versione"
   git remote add origin https://github.com/TUO-UTENTE/spesa.git
   git push -u origin main
   ```

   In alternativa puoi usare GitHub Desktop. Assicurati che venga caricata anche la cartella `.github`.
5. Apri la scheda **Actions** del repository: il workflow *Pubblica su GitHub Pages* compila l'app (2-3 minuti).
   Quando l'esecuzione è verde, l'app è online all'indirizzo

   **`https://TUO-UTENTE.github.io/spesa/`**

   Se l'esecuzione fallisce perché Pages non era ancora attivo, completa il punto 3 e poi, sempre in Actions, scegli il workflow e premi **Run workflow**.

Da qui in poi, ogni `git push` sul ramo `main` ripubblica l'app in automatico.

## 4. Installa l'app sui due iPhone

Ognuno di voi due, sul proprio iPhone:

1. Apri il link `https://TUO-UTENTE.github.io/spesa/` in **Safari**.
2. Tocca **Condividi** (il quadrato con la freccia), poi **Aggiungi alla schermata Home**, poi **Aggiungi**.
3. **Apri l'app dall'icona sulla Home** (non da Safari).
4. Scrivi il tuo nome e il **codice lista**:
   - il primo dei due tocca *Genera un nuovo codice*;
   - l'altra persona inserisce **lo stesso codice**.

> **Importante:** su iPhone l'app aggiunta alla Home ha una memoria separata da Safari.
> Nome e codice vanno quindi inseriti dopo averla aperta dall'icona.

**Per invitare la tua compagna** tocca 👥 nell'app: si apre il pannello di condivisione di iPhone (WhatsApp, Messaggi…) con il link e il codice.
Lei apre il link, aggiunge l'app alla Home come al punto 2 e, aprendola dall'icona, inserisce il codice che ha ricevuto nel messaggio.

## Come si usa

| Azione | Come |
|---|---|
| Aggiungere | Scrivi l'articolo e premi *Aggiungi* (o Invio). I doppioni vengono segnalati. |
| Segnare come preso | Tocca l'articolo: finisce in *Già presi*, con il nome di chi l'ha preso. |
| Togliere la spunta | Toccalo di nuovo. |
| Eliminare | Tocca ✕. |
| Svuotare i presi | *Rimuovi tutti*, accanto a *Già presi*. |
| Cambiare nome o lista | ⚙️ |

Le modifiche dell'altra persona compaiono entro circa 4 secondi.

## Aggiornare l'app

Modifichi il codice, poi `git commit` e `git push`. Sugli iPhone la nuova versione arriva dopo aver chiuso e riaperto l'app
(a volte servono due riaperture, perché l'app tiene una copia locale per avviarsi velocemente).

## Sicurezza

La lista è protetta solo dal codice: chi lo conosce può leggerla e modificarla. Per una lista di casa va bene; basta non diffondere il codice.
Il codice generato dall'app è casuale (12 caratteri), quindi è impossibile indovinarlo.
L'indirizzo del database è visibile nel repository pubblico, ma con le regole del punto 1 non permette di vedere le liste senza il codice.

## Struttura del progetto

| File | Contenuto |
|---|---|
| `Pages/Home.razor` | Tutta l'interfaccia: impostazioni e lista |
| `Services/ListaService.cs` | Lettura e scrittura su Firebase tramite le API REST |
| `Services/Impostazioni.cs` | Nome e codice lista salvati nel `localStorage` |
| `wwwroot/appsettings.json` | Indirizzo del database Firebase |
| `wwwroot/manifest.webmanifest`, `service-worker*.js` | Ciò che la rende una PWA installabile |
| `.github/workflows/deploy.yml` | Compilazione e pubblicazione automatica su GitHub Pages |
