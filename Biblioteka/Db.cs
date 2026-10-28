using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Biblioteka
{
    /// <summary>
    /// Statička pomoćna klasa za rad s bazom. Sve forme pozivaju samo ove metode,
    /// pa se connection string i otvaranje/zatvaranje konekcije nalaze na jednom mjestu.
    /// </summary>
    public static class Db
    {
        /// <summary>
        /// Connection string pročitan iz App.config (ne piše se direktno u kod
        /// da bi se server mogao promijeniti bez ponovnog kompajliranja).
        /// </summary>
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["BibliotekaDB"].ConnectionString;

        /// <summary>
        /// Izvršava SELECT upit i vraća rezultat kao DataTable (npr. za DataGridView).
        /// </summary>
        /// <param name="sql">Parametrizovan SQL upit (vrijednosti idu kao @Parametar).</param>
        /// <param name="p">Parametri upita (SqlParameter).</param>
        public static DataTable Upit(string sql, params SqlParameter[] p)
        {
            DataTable tabela = new DataTable();

            // "using" garantuje zatvaranje konekcije čak i ako se desi greška
            using (SqlConnection konekcija = new SqlConnection(ConnectionString))
            using (SqlCommand komanda = new SqlCommand(sql, konekcija))
            {
                // Parametri se šalju odvojeno od teksta upita (zaštita od SQL injectiona)
                if (p != null) komanda.Parameters.AddRange(p);

                // DataAdapter sam otvara konekciju, puni tabelu i zatvara konekciju
                using (SqlDataAdapter adapter = new SqlDataAdapter(komanda))
                {
                    adapter.Fill(tabela);
                }
            }
            return tabela;
        }

        /// <summary>
        /// Izvršava INSERT, UPDATE ili DELETE. Vraća broj promijenjenih redova.
        /// </summary>
        /// <param name="sql">Parametrizovan SQL upit.</param>
        /// <param name="p">Parametri upita (SqlParameter).</param>
        public static int Izvrsi(string sql, params SqlParameter[] p)
        {
            using (SqlConnection konekcija = new SqlConnection(ConnectionString))
            using (SqlCommand komanda = new SqlCommand(sql, konekcija))
            {
                if (p != null) komanda.Parameters.AddRange(p);

                // Ovdje konekciju moramo otvoriti ručno (nema DataAdaptera)
                konekcija.Open();
                return komanda.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Vraća jednu vrijednost (prvu kolonu prvog reda), npr. COUNT(*) ili SCOPE_IDENTITY().
        /// Ako upit ne vrati ništa, vraća null.
        /// </summary>
        /// <param name="sql">Parametrizovan SQL upit.</param>
        /// <param name="p">Parametri upita (SqlParameter).</param>
        public static object Skalar(string sql, params SqlParameter[] p)
        {
            using (SqlConnection konekcija = new SqlConnection(ConnectionString))
            using (SqlCommand komanda = new SqlCommand(sql, konekcija))
            {
                if (p != null) komanda.Parameters.AddRange(p);

                konekcija.Open();
                return komanda.ExecuteScalar();
            }
        }

        /// <summary>
        /// Pretvara C# vrijednost u vrijednost za bazu: null ili prazan tekst postaju DBNull.Value.
        /// Koristi se za opcionalna polja (npr. Drzava, Telefon) jer SqlParameter ne prihvata null.
        /// </summary>
        /// <param name="vrijednost">Vrijednost iz forme.</param>
        public static object UDb(object vrijednost)
        {
            if (vrijednost == null) return DBNull.Value;

            // Tekst koji sadrži samo razmake tretiramo kao "nema vrijednosti"
            string tekst = vrijednost as string;
            if (tekst != null && tekst.Trim().Length == 0) return DBNull.Value;

            return vrijednost;
        }

        /// <summary>
        /// Provjerava može li se aplikacija spojiti na bazu.
        /// </summary>
        /// <param name="poruka">Prazno ako je uspjelo, inače tekst greške.</param>
        /// <returns>true ako je konekcija uspješna.</returns>
        public static bool TestKonekcije(out string poruka)
        {
            try
            {
                using (SqlConnection konekcija = new SqlConnection(ConnectionString))
                {
                    konekcija.Open();
                }
                poruka = "";
                return true;
            }
            catch (Exception ex)
            {
                // Vraćamo tekst greške da ga možemo prikazati korisniku
                poruka = ex.Message;
                return false;
            }
        }
    }
}