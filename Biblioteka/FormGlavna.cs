using System;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Glavna forma: meni za otvaranje svih formi i početni ekran s osnovnim brojkama.
    /// </summary>
    public class FormGlavna : Form
    {
        private MenuStrip meni;
        private Label lblStatistika;

        /// <summary>
        /// Konstruktor: gradi korisničko sučelje u kodu.
        /// </summary>
        public FormGlavna()
        {
            InitializeUI();
        }

        /// <summary>
        /// Kreira sve kontrole forme (bez Designera) i povezuje stavke menija.
        /// </summary>
        private void InitializeUI()
        {
            Text = "Biblioteka - glavni prozor";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(700, 450);

            // Meni na vrhu prozora
            meni = new MenuStrip();
            ToolStripMenuItem miKnjige = new ToolStripMenuItem("Knjige");
            ToolStripMenuItem miAutori = new ToolStripMenuItem("Autori");
            ToolStripMenuItem miZanrovi = new ToolStripMenuItem("Žanrovi");
            ToolStripMenuItem miClanovi = new ToolStripMenuItem("Članovi");
            ToolStripMenuItem miPosudbe = new ToolStripMenuItem("Posudbe");
            ToolStripMenuItem miIzlaz = new ToolStripMenuItem("Izlaz");

            // Svaka stavka otvara svoju formu kao modalni prozor
            miKnjige.Click += (s, e) => { using (FormKnjigePregled f = new FormKnjigePregled()) f.ShowDialog(this); };
            miAutori.Click += (s, e) => { using (FormAutoriPregled f = new FormAutoriPregled()) f.ShowDialog(this); };
            miZanrovi.Click += (s, e) => { using (FormZanroviPregled f = new FormZanroviPregled()) f.ShowDialog(this); };
            miClanovi.Click += (s, e) => { using (FormClanoviPregled f = new FormClanoviPregled()) f.ShowDialog(this); };
            miPosudbe.Click += (s, e) => { using (FormPosudbe f = new FormPosudbe()) f.ShowDialog(this); };
            miIzlaz.Click += (s, e) => Close();

            meni.Items.AddRange(new ToolStripItem[] { miKnjige, miAutori, miZanrovi, miClanovi, miPosudbe, miIzlaz });
            MainMenuStrip = meni;

            // Veliki natpis u sredini s osnovnim brojkama
            lblStatistika = new Label();
            lblStatistika.Dock = DockStyle.Fill;
            lblStatistika.TextAlign = ContentAlignment.MiddleCenter;
            lblStatistika.Font = new Font("Segoe UI", 14F);

            // Fill kontrola se dodaje prvo, pa meni, da meni ostane na vrhu
            Controls.Add(lblStatistika);
            Controls.Add(meni);

            // Brojke se osvježavaju kad god se glavni prozor vrati u fokus
            // (npr. nakon zatvaranja neke od formi)
            Activated += (s, e) => OsvjeziStatistiku();
        }

        /// <summary>
        /// Čita osnovne brojke iz baze i ispisuje ih na početnom ekranu.
        /// </summary>
        private void OsvjeziStatistiku()
        {
            try
            {
                int knjige = Convert.ToInt32(Db.Skalar("SELECT COUNT(*) FROM Knjige"));
                int autori = Convert.ToInt32(Db.Skalar("SELECT COUNT(*) FROM Autori"));
                int clanovi = Convert.ToInt32(Db.Skalar("SELECT COUNT(*) FROM Clanovi"));

                // Aktivna posudba = knjiga još nije vraćena (DatumVracanja je NULL)
                int aktivne = Convert.ToInt32(Db.Skalar(
                    "SELECT COUNT(*) FROM Posudbe WHERE DatumVracanja IS NULL"));
                int kasnjenja = Convert.ToInt32(Db.Skalar(
                    "SELECT COUNT(*) FROM Posudbe WHERE DatumVracanja IS NULL AND RokVracanja < CAST(GETDATE() AS DATE)"));

                lblStatistika.Text =
                    $"Knjiga: {knjige}\n" +
                    $"Autora: {autori}\n" +
                    $"Članova: {clanovi}\n" +
                    $"Aktivnih posudbi: {aktivne}\n" +
                    $"Posudbi u kašnjenju: {kasnjenja}";
            }
            catch (Exception ex)
            {
                lblStatistika.Text = "Greška pri čitanju podataka:\n" + ex.Message;
            }
        }
    }
}