using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Dijalog za unos nove posudbe: odabir knjige (samo s dostupnim primjercima),
    /// odabir člana i roka vraćanja. Provjera dostupnosti radi se u samom INSERT upitu.
    /// </summary>
    public class FormPosudiDetalji : Form
    {
        private ComboBox cmbKnjiga, cmbClan;
        private DateTimePicker dtpRok;
        private Button btnSnimi, btnOtkazi;

        /// <summary>
        /// Konstruktor: gradi sučelje i puni liste knjiga i članova.
        /// </summary>
        public FormPosudiDetalji()
        {
            InitializeUI();
            UcitajListe();
        }

        /// <summary>
        /// Kreira kontrole forme (bez Designera).
        /// </summary>
        private void InitializeUI()
        {
            Text = "Nova posudba";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 210);

            // Knjiga i član se biraju iz liste, ne upisuju ručno (nema pogrešnog unosa)
            cmbKnjiga = DodajCombo("Knjiga *", 20);
            cmbClan = DodajCombo("Član *", 60);

            Label lblRok = new Label();
            lblRok.Text = "Rok vraćanja *";
            lblRok.Location = new Point(15, 103);
            lblRok.AutoSize = true;

            // Rok ne može biti u prošlosti (baza to također provjerava CHECK ograničenjem)
            dtpRok = new DateTimePicker();
            dtpRok.Format = DateTimePickerFormat.Short;
            dtpRok.MinDate = DateTime.Today;
            dtpRok.Value = DateTime.Today.AddDays(14); // standardni rok posudbe: 14 dana
            dtpRok.Location = new Point(130, 100);
            dtpRok.Width = 120;

            Controls.Add(lblRok);
            Controls.Add(dtpRok);

            btnSnimi = new Button();
            btnSnimi.Text = "Posudi";
            btnSnimi.Location = new Point(240, 160);
            btnSnimi.Size = new Size(90, 30);
            btnSnimi.Click += (s, e) => Snimi();

            btnOtkazi = new Button();
            btnOtkazi.Text = "Otkaži";
            btnOtkazi.Location = new Point(340, 160);
            btnOtkazi.Size = new Size(90, 30);
            btnOtkazi.DialogResult = DialogResult.Cancel;

            Controls.Add(btnSnimi);
            Controls.Add(btnOtkazi);

            // Enter = Posudi, Esc = Otkaži
            AcceptButton = btnSnimi;
            CancelButton = btnOtkazi;
        }

        /// <summary>
        /// Dodaje natpis i ComboBox (samo odabir, bez ručnog upisa) u jedan red forme.
        /// </summary>
        /// <param name="natpis">Tekst ispred liste.</param>
        /// <param name="y">Vertikalna pozicija reda.</param>
        private ComboBox DodajCombo(string natpis, int y)
        {
            Label l = new Label();
            l.Text = natpis;
            l.Location = new Point(15, y + 3);
            l.AutoSize = true;

            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Location = new Point(130, y);
            c.Width = 295;

            Controls.Add(l);
            Controls.Add(c);
            return c;
        }

        /// <summary>
        /// Puni liste: knjige s barem jednim slobodnim primjerkom i svi članovi.
        /// </summary>
        private void UcitajListe()
        {
            try
            {
                // Dostupno se računa (Ukupno minus nevraćene posudbe), kao i u pregledu knjiga
                DataTable knjige = Db.Upit(
                    "SELECT k.KnjigaID, " +
                    "k.Naslov + N' (' + a.Prezime + N') - dostupno: ' + " +
                    "CAST(k.UkupnoPrimjeraka - (SELECT COUNT(*) FROM Posudbe p " +
                    "WHERE p.KnjigaID = k.KnjigaID AND p.DatumVracanja IS NULL) AS NVARCHAR(10)) AS Opis " +
                    "FROM Knjige k INNER JOIN Autori a ON a.AutorID = k.AutorID " +
                    "WHERE k.UkupnoPrimjeraka - (SELECT COUNT(*) FROM Posudbe p " +
                    "WHERE p.KnjigaID = k.KnjigaID AND p.DatumVracanja IS NULL) > 0 " +
                    "ORDER BY k.Naslov");

                DataTable clanovi = Db.Upit(
                    "SELECT ClanID, Ime + N' ' + Prezime + N' (' + Email + N')' AS Opis " +
                    "FROM Clanovi ORDER BY Prezime, Ime");

                // DisplayMember i ValueMember postavljamo prije DataSource-a
                cmbKnjiga.DisplayMember = "Opis";
                cmbKnjiga.ValueMember = "KnjigaID";
                cmbKnjiga.DataSource = knjige;

                cmbClan.DisplayMember = "Opis";
                cmbClan.ValueMember = "ClanID";
                cmbClan.DataSource = clanovi;

                // Ako nema šta posuditi ili nema članova, snimanje nema smisla
                if (knjige.Rows.Count == 0 || clanovi.Rows.Count == 0)
                {
                    btnSnimi.Enabled = false;
                    string razlog = knjige.Rows.Count == 0
                        ? "Nema knjiga sa slobodnim primjercima."
                        : "Nema upisanih članova.";
                    MessageBox.Show(razlog, "Posudba nije moguća",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                btnSnimi.Enabled = false;
                MessageBox.Show("Greška pri učitavanju: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Validira unos i upisuje posudbu. Dostupnost se provjerava unutar istog INSERT upita,
        /// pa se između provjere i upisa ne može umetnuti druga posudba.
        /// </summary>
        private void Snimi()
        {
            // Validacija: mora biti odabrana i knjiga i član
            if (cmbKnjiga.SelectedValue == null || cmbClan.SelectedValue == null)
            {
                MessageBox.Show("Odaberite knjigu i člana.", "Neispravan unos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SqlParameter pKnjiga = new SqlParameter("@knjiga", SqlDbType.Int);
                pKnjiga.Value = Convert.ToInt32(cmbKnjiga.SelectedValue);
                SqlParameter pClan = new SqlParameter("@clan", SqlDbType.Int);
                pClan.Value = Convert.ToInt32(cmbClan.SelectedValue);
                SqlParameter pRok = new SqlParameter("@rok", SqlDbType.Date);
                pRok.Value = dtpRok.Value.Date;

                // INSERT ... SELECT ... WHERE: red se upisuje samo ako ima slobodnih primjeraka.
                // Ako uvjet nije ispunjen, upit vrati 0 redova i ništa se ne upiše.
                int upisano = Db.Izvrsi(
                    "INSERT INTO Posudbe (KnjigaID, ClanID, DatumPosudbe, RokVracanja) " +
                    "SELECT @knjiga, @clan, CAST(GETDATE() AS DATE), @rok " +
                    "WHERE (SELECT UkupnoPrimjeraka FROM Knjige WHERE KnjigaID = @knjiga) " +
                    "- (SELECT COUNT(*) FROM Posudbe WHERE KnjigaID = @knjiga AND DatumVracanja IS NULL) > 0",
                    pKnjiga, pClan, pRok);

                if (upisano == 0)
                {
                    // Neko je u međuvremenu posudio zadnji primjerak
                    MessageBox.Show("Nema slobodnih primjeraka ove knjige. Osvježite listu.",
                        "Posudba nije moguća", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    UcitajListe();
                    return;
                }

                // OK zatvara formu, a pregled posudbi na osnovu toga osvježava tabelu
                DialogResult = DialogResult.OK;
            }
            catch (SqlException ex)
            {
                // 547 = knjiga ili član je u međuvremenu obrisan (prekršen strani ključ)
                if (ex.Number == 547)
                {
                    MessageBox.Show("Odabrana knjiga ili član više ne postoji. Osvježite podatke.",
                        "Greška", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Greška baze: " + ex.Message, "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}