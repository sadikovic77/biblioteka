using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Forma za unos nove ili uređivanje postojeće knjige.
    /// Ako je id null radi se unos, inače uređivanje.
    /// </summary>
    public class FormKnjigeDetalji : Form
    {
        private readonly int? knjigaId;
        private bool nijePronadjen;
        private bool nemaSifarnika;
        private TextBox txtNaslov;
        private ComboBox cmbAutor;
        private ComboBox cmbZanr;
        private TextBox txtGodina;
        private TextBox txtIsbn;
        private NumericUpDown nudUkupno;
        private Button btnSnimi;
        private Button btnOtkazi;

        /// <summary>
        /// Konstruktor: id je null za novu knjigu, a ID postojeće za uređivanje.
        /// </summary>
        public FormKnjigeDetalji(int? id)
        {
            knjigaId = id;
            InitializeUI();
            UcitajSifarnike();

            Text = knjigaId.HasValue ? "Uređivanje knjige" : "Unos nove knjige";

            if (knjigaId.HasValue) UcitajKnjigu();

            // Ako nešto nedostaje, zatvaramo formu čim se prikaže
            Shown += (s, e) =>
            {
                if (nemaSifarnika)
                {
                    MessageBox.Show("Prvo unesite barem jednog autora i jedan žanr.", "Upozorenje",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                }
                else if (nijePronadjen)
                {
                    MessageBox.Show("Knjiga više ne postoji.", "Upozorenje",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                }
            };
        }

        /// <summary>
        /// Pravi kontrole u kodu.
        /// </summary>
        private void InitializeUI()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 330);

            // MaxLength je jednak veličini kolone u bazi
            txtNaslov = DodajTekst("Naslov:", 20, 150);

            cmbAutor = DodajCombo("Autor:", 55);
            cmbZanr = DodajCombo("Žanr:", 90);

            txtGodina = DodajTekst("Godina izdanja:", 125, 4);
            txtIsbn = DodajTekst("ISBN:", 160, 20);

            Label lblUkupno = new Label();
            lblUkupno.Text = "Ukupno primjeraka:";
            lblUkupno.Location = new Point(15, 198);
            lblUkupno.AutoSize = true;

            // NumericUpDown ne dozvoljava slova ni vrijednost manju od 1
            nudUkupno = new NumericUpDown();
            nudUkupno.Location = new Point(150, 195);
            nudUkupno.Width = 80;
            nudUkupno.Minimum = 1;
            nudUkupno.Maximum = 1000;
            nudUkupno.Value = 1;

            btnSnimi = new Button();
            btnSnimi.Text = "Snimi";
            btnSnimi.Location = new Point(230, 280);
            btnSnimi.Size = new Size(80, 28);
            btnSnimi.Click += (s, e) => Snimi();

            btnOtkazi = new Button();
            btnOtkazi.Text = "Otkaži";
            btnOtkazi.Location = new Point(320, 280);
            btnOtkazi.Size = new Size(80, 28);
            btnOtkazi.DialogResult = DialogResult.Cancel;

            // Enter = Snimi, Esc = Otkaži
            AcceptButton = btnSnimi;
            CancelButton = btnOtkazi;

            Controls.Add(lblUkupno);
            Controls.Add(nudUkupno);
            Controls.Add(btnSnimi);
            Controls.Add(btnOtkazi);
        }

        /// <summary>
        /// Pomoćna metoda: dodaje labelu i tekstualno polje u isti red.
        /// </summary>
        private TextBox DodajTekst(string naslov, int y, int maxDuzina)
        {
            Label lbl = new Label();
            lbl.Text = naslov;
            lbl.Location = new Point(15, y + 3);
            lbl.AutoSize = true;

            TextBox txt = new TextBox();
            txt.Location = new Point(150, y);
            txt.Width = 250;
            txt.MaxLength = maxDuzina;

            Controls.Add(lbl);
            Controls.Add(txt);
            return txt;
        }

        /// <summary>
        /// Pomoćna metoda: dodaje labelu i padajuću listu (samo odabir, bez upisivanja).
        /// </summary>
        private ComboBox DodajCombo(string naslov, int y)
        {
            Label lbl = new Label();
            lbl.Text = naslov;
            lbl.Location = new Point(15, y + 3);
            lbl.AutoSize = true;

            ComboBox cmb = new ComboBox();
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Location = new Point(150, y);
            cmb.Width = 250;

            Controls.Add(lbl);
            Controls.Add(cmb);
            return cmb;
        }

        /// <summary>
        /// Puni padajuće liste autorima i žanrovima iz baze.
        /// DisplayMember je tekst koji se vidi, a ValueMember ID koji se snima.
        /// </summary>
        private void UcitajSifarnike()
        {
            DataTable autori = Db.Upit(
                "SELECT AutorID, Ime + ' ' + Prezime AS Naziv FROM Autori ORDER BY Prezime, Ime");
            DataTable zanrovi = Db.Upit("SELECT ZanrID, Naziv FROM Zanrovi ORDER BY Naziv");

            if (autori.Rows.Count == 0 || zanrovi.Rows.Count == 0)
            {
                nemaSifarnika = true;
                return;
            }

            cmbAutor.DataSource = autori;
            cmbAutor.DisplayMember = "Naziv";
            cmbAutor.ValueMember = "AutorID";

            cmbZanr.DataSource = zanrovi;
            cmbZanr.DisplayMember = "Naziv";
            cmbZanr.ValueMember = "ZanrID";
        }

        /// <summary>
        /// Učitava podatke knjige iz baze u polja (samo kod uređivanja).
        /// </summary>
        private void UcitajKnjigu()
        {
            if (nemaSifarnika) return;

            SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
            pId.Value = knjigaId.Value;

            DataTable t = Db.Upit(
                "SELECT Naslov, AutorID, ZanrID, GodinaIzdanja, ISBN, UkupnoPrimjeraka " +
                "FROM Knjige WHERE KnjigaID = @id", pId);
            if (t.Rows.Count == 0)
            {
                nijePronadjen = true;
                return;
            }

            DataRow r = t.Rows[0];
            txtNaslov.Text = Convert.ToString(r["Naslov"]);
            cmbAutor.SelectedValue = Convert.ToInt32(r["AutorID"]);
            cmbZanr.SelectedValue = Convert.ToInt32(r["ZanrID"]);
            // Godina i ISBN mogu biti NULL, Convert.ToString(DBNull) daje prazan tekst
            txtGodina.Text = Convert.ToString(r["GodinaIzdanja"]);
            txtIsbn.Text = Convert.ToString(r["ISBN"]);
            nudUkupno.Value = Convert.ToInt32(r["UkupnoPrimjeraka"]);
        }

        /// <summary>
        /// Provjerava unos i snima knjigu (INSERT ili UPDATE). Duplikat ISBN-a daje grešku 2627/2601.
        /// </summary>
        private void Snimi()
        {
            string naslov = txtNaslov.Text.Trim();
            string isbn = txtIsbn.Text.Trim();
            string godinaTekst = txtGodina.Text.Trim();

            // Validacija obaveznih polja
            if (naslov.Length == 0) { Greska("Naslov je obavezan.", txtNaslov); return; }
            if (isbn.Length == 0) { Greska("ISBN je obavezan.", txtIsbn); return; }

            // Godina je opcionalna, ali ako je upisana mora biti ispravan broj
            object godina = DBNull.Value;
            if (godinaTekst.Length > 0)
            {
                int g;
                if (!int.TryParse(godinaTekst, out g) || g < 1 || g > DateTime.Today.Year)
                {
                    Greska("Godina izdanja mora biti broj od 1 do " + DateTime.Today.Year + ".", txtGodina);
                    return;
                }
                godina = g;
            }

            int ukupno = (int)nudUkupno.Value;

            // Kod uređivanja ukupno ne smije biti manje od broja trenutno posuđenih primjeraka
            if (knjigaId.HasValue)
            {
                SqlParameter pProv = new SqlParameter("@id", SqlDbType.Int);
                pProv.Value = knjigaId.Value;
                int posudjeno = Convert.ToInt32(Db.Skalar(
                    "SELECT COUNT(*) FROM Posudbe WHERE KnjigaID = @id AND DatumVracanja IS NULL", pProv));

                if (ukupno < posudjeno)
                {
                    Greska("Trenutno je posuđeno " + posudjeno +
                           " primjeraka, pa ukupno ne može biti manje od toga.", nudUkupno);
                    return;
                }
            }

            try
            {
                SqlParameter pNaslov = new SqlParameter("@naslov", SqlDbType.NVarChar, 150);
                pNaslov.Value = naslov;
                SqlParameter pAutor = new SqlParameter("@autor", SqlDbType.Int);
                pAutor.Value = Convert.ToInt32(cmbAutor.SelectedValue);
                SqlParameter pZanr = new SqlParameter("@zanr", SqlDbType.Int);
                pZanr.Value = Convert.ToInt32(cmbZanr.SelectedValue);
                SqlParameter pGodina = new SqlParameter("@godina", SqlDbType.Int);
                pGodina.Value = godina;
                SqlParameter pIsbn = new SqlParameter("@isbn", SqlDbType.NVarChar, 20);
                pIsbn.Value = isbn;
                SqlParameter pUkupno = new SqlParameter("@ukupno", SqlDbType.Int);
                pUkupno.Value = ukupno;

                if (knjigaId.HasValue)
                {
                    // Uređivanje postojeće knjige
                    SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                    pId.Value = knjigaId.Value;
                    Db.Izvrsi(
                        "UPDATE Knjige SET Naslov = @naslov, AutorID = @autor, ZanrID = @zanr, " +
                        "GodinaIzdanja = @godina, ISBN = @isbn, UkupnoPrimjeraka = @ukupno " +
                        "WHERE KnjigaID = @id",
                        pNaslov, pAutor, pZanr, pGodina, pIsbn, pUkupno, pId);
                }
                else
                {
                    // Unos nove knjige
                    Db.Izvrsi(
                        "INSERT INTO Knjige (Naslov, AutorID, ZanrID, GodinaIzdanja, ISBN, UkupnoPrimjeraka) " +
                        "VALUES (@naslov, @autor, @zanr, @godina, @isbn, @ukupno)",
                        pNaslov, pAutor, pZanr, pGodina, pIsbn, pUkupno);
                }

                // OK zatvara formu, a pregled zna da treba osvježiti tabelu
                DialogResult = DialogResult.OK;
            }
            catch (SqlException ex)
            {
                // 2627 / 2601 = kršenje UNIQUE ograničenja (ISBN već postoji)
                if (ex.Number == 2627 || ex.Number == 2601)
                    Greska("Knjiga s tim ISBN-om već postoji.", txtIsbn);
                else
                    Greska("Greška baze: " + ex.Message, txtNaslov);
            }
        }

        /// <summary>
        /// Prikazuje poruku o grešci i vraća fokus na zadano polje.
        /// </summary>
        private void Greska(string poruka, Control polje)
        {
            MessageBox.Show(poruka, "Upozorenje", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            polje.Focus();
        }
    }
}