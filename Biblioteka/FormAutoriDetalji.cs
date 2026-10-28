using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Forma za unos novog ili uređivanje postojećeg autora (dva načina rada).
    /// Ako je id null radi se o unosu, inače o uređivanju autora s tim ID-em.
    /// </summary>
    public class FormAutoriDetalji : Form
    {
        private TextBox txtIme, txtPrezime, txtDrzava, txtGodina;
        private Button btnSnimi, btnOtkazi;

        private readonly int? autorId;     // null = unos, vrijednost = uređivanje
        private bool nijePronadjen = false; // true ako autor za uređivanje više ne postoji

        /// <summary>
        /// Konstruktor: postavlja način rada i, kod uređivanja, popunjava polja.
        /// </summary>
        /// <param name="id">ID autora za uređivanje, ili null za unos novog.</param>
        public FormAutoriDetalji(int? id)
        {
            autorId = id;
            InitializeUI();

            // Naslov ovisi o načinu rada
            Text = autorId == null ? "Unos novog autora" : "Uređivanje autora";

            if (autorId != null) UcitajAutora();

            // Ako autor nije nađen, zatvaramo formu čim se prikaže
            Shown += (s, e) =>
            {
                if (nijePronadjen)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        /// <summary>
        /// Kreira polja za unos i dugmad.
        /// </summary>
        private void InitializeUI()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 230);

            txtIme = DodajRed("Ime *", 20, 50);
            txtPrezime = DodajRed("Prezime *", 55, 50);
            txtDrzava = DodajRed("Država", 90, 60);
            txtGodina = DodajRed("Godina rođenja", 125, 4);

            btnSnimi = new Button();
            btnSnimi.Text = "Snimi";
            btnSnimi.Location = new Point(150, 180);
            btnSnimi.Size = new Size(90, 30);
            btnSnimi.Click += (s, e) => Snimi();

            btnOtkazi = new Button();
            btnOtkazi.Text = "Otkaži";
            btnOtkazi.Location = new Point(250, 180);
            btnOtkazi.Size = new Size(90, 30);
            btnOtkazi.DialogResult = DialogResult.Cancel;

            Controls.Add(btnSnimi);
            Controls.Add(btnOtkazi);

            // Enter = Snimi, Esc = Otkaži
            AcceptButton = btnSnimi;
            CancelButton = btnOtkazi;
        }

        /// <summary>
        /// Dodaje natpis i tekstualno polje u jedan red forme.
        /// </summary>
        /// <param name="natpis">Tekst ispred polja.</param>
        /// <param name="y">Vertikalna pozicija reda.</param>
        /// <param name="maxDuzina">Najveći dozvoljeni broj znakova (isto kao u bazi).</param>
        private TextBox DodajRed(string natpis, int y, int maxDuzina)
        {
            Label l = new Label();
            l.Text = natpis;
            l.Location = new Point(15, y + 3);
            l.AutoSize = true;

            TextBox t = new TextBox();
            t.Location = new Point(130, y);
            t.Width = 210;
            t.MaxLength = maxDuzina;

            Controls.Add(l);
            Controls.Add(t);
            return t;
        }

        /// <summary>
        /// Čita autora iz baze i popunjava polja (samo u načinu uređivanja).
        /// </summary>
        private void UcitajAutora()
        {
            try
            {
                SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                pId.Value = autorId.Value;

                DataTable t = Db.Upit(
                    "SELECT Ime, Prezime, Drzava, GodinaRodjenja FROM Autori WHERE AutorID = @id", pId);

                if (t.Rows.Count == 0)
                {
                    MessageBox.Show("Autor više ne postoji u bazi.", "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    nijePronadjen = true;
                    return;
                }

                // NULL iz baze pretvaramo u prazan tekst jer TextBox ne prihvata DBNull
                DataRow r = t.Rows[0];
                txtIme.Text = Convert.ToString(r["Ime"]);
                txtPrezime.Text = Convert.ToString(r["Prezime"]);
                txtDrzava.Text = r["Drzava"] == DBNull.Value ? "" : Convert.ToString(r["Drzava"]);
                txtGodina.Text = r["GodinaRodjenja"] == DBNull.Value ? "" : Convert.ToString(r["GodinaRodjenja"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                nijePronadjen = true;
            }
        }

        /// <summary>
        /// Validira unos i snima (INSERT ili UPDATE zavisno od načina rada).
        /// </summary>
        private void Snimi()
        {
            string ime = txtIme.Text.Trim();
            string prezime = txtPrezime.Text.Trim();
            string drzava = txtDrzava.Text.Trim();
            string godinaTekst = txtGodina.Text.Trim();

            // Validacija: obavezna polja
            if (ime.Length == 0)
            {
                Greska("Ime je obavezno.", txtIme);
                return;
            }
            if (prezime.Length == 0)
            {
                Greska("Prezime je obavezno.", txtPrezime);
                return;
            }

            // Validacija: godina je opcionalna, ali ako je upisana mora biti smislen broj
            object godina = DBNull.Value;
            if (godinaTekst.Length > 0)
            {
                int g;
                if (!int.TryParse(godinaTekst, out g) || g < 1 || g > DateTime.Now.Year)
                {
                    Greska("Godina rođenja mora biti broj između 1 i " + DateTime.Now.Year + ".", txtGodina);
                    return;
                }
                godina = g;
            }

            try
            {
                // Parametri su isti za INSERT i UPDATE; prazna država postaje NULL
                SqlParameter pIme = new SqlParameter("@ime", SqlDbType.NVarChar, 50);
                pIme.Value = ime;
                SqlParameter pPrezime = new SqlParameter("@prezime", SqlDbType.NVarChar, 50);
                pPrezime.Value = prezime;
                SqlParameter pDrzava = new SqlParameter("@drzava", SqlDbType.NVarChar, 60);
                pDrzava.Value = Db.UDb(drzava);
                SqlParameter pGodina = new SqlParameter("@godina", SqlDbType.Int);
                pGodina.Value = godina;

                if (autorId == null)
                {
                    // Način rada: unos novog autora
                    Db.Izvrsi("INSERT INTO Autori (Ime, Prezime, Drzava, GodinaRodjenja) " +
                              "VALUES (@ime, @prezime, @drzava, @godina)",
                              pIme, pPrezime, pDrzava, pGodina);
                }
                else
                {
                    // Način rada: uređivanje, WHERE osigurava da mijenjamo samo ovog autora
                    SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                    pId.Value = autorId.Value;
                    Db.Izvrsi("UPDATE Autori SET Ime = @ime, Prezime = @prezime, " +
                              "Drzava = @drzava, GodinaRodjenja = @godina WHERE AutorID = @id",
                              pIme, pPrezime, pDrzava, pGodina, pId);
                }

                // OK zatvara formu, a pregled na osnovu toga osvježava tabelu
                DialogResult = DialogResult.OK;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Greška baze: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Prikazuje poruku o neispravnom unosu i vraća fokus na pogrešno polje.
        /// </summary>
        /// <param name="poruka">Tekst poruke.</param>
        /// <param name="polje">Polje na koje se vraća fokus.</param>
        private void Greska(string poruka, TextBox polje)
        {
            MessageBox.Show(poruka, "Neispravan unos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            polje.Focus();
        }
    }
}