using System;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Ulazna tačka aplikacije: provjerava konekciju i pokreće glavnu formu.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Glavna metoda programa.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Ako baza nije dostupna, nema smisla otvarati aplikaciju
            string greska;
            if (!Db.TestKonekcije(out greska))
            {
                MessageBox.Show("Konekcija nije uspjela:\n" + greska, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Pokrećemo glavnu formu; aplikacija se gasi kad se ona zatvori
            Application.Run(new FormGlavna());
        }
    }
}