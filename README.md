# Biblioteka - seminarski rad

Windows Forms aplikacija (C#, .NET Framework 4.8) za evidenciju knjiga, autora,
žanrova, članova i posudbi u biblioteci. Baza: Microsoft SQL Server LocalDB.

Autor: [Tarik Sadiković]
Predmet: [.NET Tehnologije]
GitHub: [https://github.com/sadikovic77/biblioteka]

## Funkcionalnosti
- CRUD (unos, pregled, uređivanje, brisanje) za autore, žanrove, članove i knjige
- Pretraga u svakom pregledu (parametrizovan LIKE)
- Posudba i vraćanje knjiga s provjerom dostupnosti primjeraka
- Filter "samo kašnjenja"
- Validacija unosa i razumljive poruke pri kršenju ograničenja (strani ključevi, duplikati)

## Struktura baze (ER opis)
- **Autori** 1:N **Knjige** (FK AutorID)
- **Zanrovi** 1:N **Knjige** (FK ZanrID)
- **Knjige** 1:N **Posudbe** (FK KnjigaID)
- **Clanovi** 1:N **Posudbe** (FK ClanID)
- Dostupni primjerci se ne čuvaju, nego se računaju:
  `UkupnoPrimjeraka - broj posudbi gdje je DatumVracanja IS NULL`.
- Strani ključevi nemaju kaskadno brisanje.

## Pokretanje
1. Instalirati Visual Studio 2022 s komponentom ".NET desktop development"
   i SQL Server LocalDB (dolazi s "Data storage and processing").
2. Otvoriti SQL Server Management Studio, spojiti se na `(localdb)\MSSQLLocalDB`.
3. Otvoriti `SQL/BibliotekaDB_dump.sql` i pokrenuti ga u cijelosti (Ctrl + A, pa F5).
   Skripta kreira bazu `BibliotekaDB` i probne podatke.
4. Otvoriti `Biblioteka.sln` u Visual Studiju i pokrenuti s F5.
5. Ako se koristi drugi server, promijeniti connection string u `App.config`.

## Screenshotovi dostupni u folderu na sistemu fakulteta


