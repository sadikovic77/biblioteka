using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Forma za unos novog ili uređivanje postojećeg člana.
    /// Ako je id null radi se unos, inače uređivanje.
    /// </summary>
    public class FormClanoviDetalji : Form
    {
        private readonly int? clanId;
        private bool nijePronadjen;
        private TextBox txtIme;
        private TextBox txtPrezime;
        private TextBox txtEmail;
        private TextBox txtTelefon;
        private DateTimePicker dtpDatum;
        private Button btnSnimi;
        private Button btnOtkazi;

        /// <summary>
        /// Konstruktor: id je null za novog člana, a ID postojećeg za uređivanje.
        /// </summary>
        public FormClanoviDetalji(int? id)
        {
            clanId = id;
            InitializeUI();

            Text = clanId.HasValue ? "Uređivanje člana" : "Unos novog člana";

            if (clanId.HasValue) UcitajClana();

            // Ako zapis više ne postoji (neko ga je obrisao), zatvaramo formu čim se prikaže
            Shown += (s, e) =>
            {
                if (nijePronadjen)
                {
                    MessageBox.Show("Član više ne postoji.", "Upozorenje",
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
            ClientSize = new Size(380, 270);

            txtIme = DodajRed("Ime:", 20, 50);
            txtPrezime = DodajRed("Prezime:", 55, 50);
            txtEmail = DodajRed("Email:", 90, 100);
            txtTelefon = DodajRed("Telefon:", 125, 30);

            Label lblDatum = new Label();
            lblDatum.Text = "Datum učlanjenja:";
            lblDatum.Location = new Point(15, 163);
            lblDatum.AutoSize = true;

            dtpDatum = new DateTimePicker();
            dtpDatum.Format = DateTimePickerFormat.Short;
            dtpDatum.Location = new Point(130, 159);
            dtpDatum.Width = 130;
            // Kod unosa je zadani datum današnji
            dtpDatum.Value = DateTime.Today;

            btnSnimi = new Button();
            btnSnimi.Text = "Snimi";
            btnSnimi.Location = new Point(190, 220);
            btnSnimi.Size = new Size(80, 28);
            btnSnimi.Click += (s, e) => Snimi();

            btnOtkazi = new Button();
            btnOtkazi.Text = "Otkaži";
            btnOtkazi.Location = new Point(280, 220);
            btnOtkazi.Size = new Size(80, 28);
            btnOtkazi.DialogResult = DialogResult.Cancel;

            // Enter = Snimi, Esc = Otkaži
            AcceptButton = btnSnimi;
            CancelButton = btnOtkazi;

            Controls.Add(lblDatum);
            Controls.Add(dtpDatum);
            Controls.Add(btnSnimi);
            Controls.Add(btnOtkazi);
        }

        /// <summary>
        /// Pomoćna metoda: dodaje labelu i tekstualno polje u isti red.
        /// MaxLength je jednak veličini kolone u bazi.
        /// </summary>
        private TextBox DodajRed(string naslov, int y, int maxDuzina)
        {
            Label lbl = new Label();
            lbl.Text = naslov;
            lbl.Location = new Point(15, y + 3);
            lbl.AutoSize = true;

            TextBox txt = new TextBox();
            txt.Location = new Point(130, y);
            txt.Width = 230;
            txt.MaxLength = maxDuzina;

            Controls.Add(lbl);
            Controls.Add(txt);
            return txt;
        }

        /// <summary>
        /// Učitava podatke člana iz baze u polja (samo kod uređivanja).
        /// </summary>
        private void UcitajClana()
        {
            SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
            pId.Value = clanId.Value;

            DataTable t = Db.Upit(
                "SELECT Ime, Prezime, Email, Telefon, DatumUclanjenja FROM Clanovi WHERE ClanID = @id", pId);
            if (t.Rows.Count == 0)
            {
                nijePronadjen = true;
                return;
            }

            DataRow r = t.Rows[0];
            txtIme.Text = Convert.ToString(r["Ime"]);
            txtPrezime.Text = Convert.ToString(r["Prezime"]);
            txtEmail.Text = Convert.ToString(r["Email"]);
            // Telefon može biti NULL u bazi, Convert.ToString(DBNull) daje prazan tekst
            txtTelefon.Text = Convert.ToString(r["Telefon"]);
            dtpDatum.Value = Convert.ToDateTime(r["DatumUclanjenja"]);
        }

        /// <summary>
        /// Provjerava da li je tekst ispravna email adresa.
        /// </summary>
        private bool EmailIspravan(string email)
        {
            try
            {
                // MailAddress baca izuzetak ako format nije ispravan
                MailAddress adresa = new MailAddress(email);
                // Dodatno tražimo da je cijeli tekst adresa i da domena ima tačku
                return adresa.Address == email && adresa.Host.Contains(".");
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Provjerava unos i snima člana (INSERT ili UPDATE). Duplikat emaila daje grešku 2627/2601.
        /// </summary>
        private void Snimi()
        {
            string ime = txtIme.Text.Trim();
            string prezime = txtPrezime.Text.Trim();
            string email = txtEmail.Text.Trim();
            string telefon = txtTelefon.Text.Trim();

            // Validacija obaveznih polja
            if (ime.Length == 0) { Greska("Ime je obavezno.", txtIme); return; }
            if (prezime.Length == 0) { Greska("Prezime je obavezno.", txtPrezime); return; }
            if (email.Length == 0) { Greska("Email je obavezan.", txtEmail); return; }
            if (!EmailIspravan(email)) { Greska("Email nije u ispravnom formatu.", txtEmail); return; }

            try
            {
                SqlParameter pIme = new SqlParameter("@ime", SqlDbType.NVarChar, 50);
                pIme.Value = ime;
                SqlParameter pPrezime = new SqlParameter("@prezime", SqlDbType.NVarChar, 50);
                pPrezime.Value = prezime;
                SqlParameter pEmail = new SqlParameter("@email", SqlDbType.NVarChar, 100);
                pEmail.Value = email;
                // Telefon je opcionalan: prazan tekst postaje NULL
                SqlParameter pTelefon = new SqlParameter("@telefon", SqlDbType.NVarChar, 30);
                pTelefon.Value = Db.UDb(telefon);
                SqlParameter pDatum = new SqlParameter("@datum", SqlDbType.Date);
                pDatum.Value = dtpDatum.Value.Date;

                if (clanId.HasValue)
                {
                    // Uređivanje postojećeg člana
                    SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                    pId.Value = clanId.Value;
                    Db.Izvrsi(
                        "UPDATE Clanovi SET Ime = @ime, Prezime = @prezime, Email = @email, " +
                        "Telefon = @telefon, DatumUclanjenja = @datum WHERE ClanID = @id",
                        pIme, pPrezime, pEmail, pTelefon, pDatum, pId);
                }
                else
                {
                    // Unos novog člana
                    Db.Izvrsi(
                        "INSERT INTO Clanovi (Ime, Prezime, Email, Telefon, DatumUclanjenja) " +
                        "VALUES (@ime, @prezime, @email, @telefon, @datum)",
                        pIme, pPrezime, pEmail, pTelefon, pDatum);
                }

                // OK zatvara formu, a pregled zna da treba osvježiti tabelu
                DialogResult = DialogResult.OK;
            }
            catch (SqlException ex)
            {
                // 2627 / 2601 = kršenje UNIQUE ograničenja (email već postoji)
                if (ex.Number == 2627 || ex.Number == 2601)
                    Greska("Član s tim emailom već postoji.", txtEmail);
                else
                    Greska("Greška baze: " + ex.Message, txtIme);
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