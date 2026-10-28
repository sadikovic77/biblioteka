using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Forma za unos novog ili uređivanje postojećeg žanra.
    /// Ako je id null radi se unos, inače uređivanje.
    /// </summary>
    public class FormZanroviDetalji : Form
    {
        private readonly int? zanrId;
        private bool nijePronadjen;
        private TextBox txtNaziv;
        private Button btnSnimi;
        private Button btnOtkazi;

        /// <summary>
        /// Konstruktor: id je null za novi žanr, a ID postojećeg za uređivanje.
        /// </summary>
        public FormZanroviDetalji(int? id)
        {
            zanrId = id;
            InitializeUI();

            Text = zanrId.HasValue ? "Uređivanje žanra" : "Unos novog žanra";

            if (zanrId.HasValue) UcitajZanr();

            // Ako zapis više ne postoji (neko ga je obrisao), zatvaramo formu čim se prikaže
            Shown += (s, e) =>
            {
                if (nijePronadjen)
                {
                    MessageBox.Show("Žanr više ne postoji.", "Upozorenje",
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
            ClientSize = new Size(320, 120);

            Label lbl = new Label();
            lbl.Text = "Naziv:";
            lbl.Location = new Point(15, 22);
            lbl.AutoSize = true;

            txtNaziv = new TextBox();
            txtNaziv.Location = new Point(70, 19);
            txtNaziv.Width = 230;
            // MaxLength jednak veličini kolone u bazi (NVARCHAR(50))
            txtNaziv.MaxLength = 50;

            btnSnimi = new Button();
            btnSnimi.Text = "Snimi";
            btnSnimi.Location = new Point(135, 70);
            btnSnimi.Size = new Size(80, 28);
            btnSnimi.Click += (s, e) => Snimi();

            btnOtkazi = new Button();
            btnOtkazi.Text = "Otkaži";
            btnOtkazi.Location = new Point(225, 70);
            btnOtkazi.Size = new Size(80, 28);
            btnOtkazi.DialogResult = DialogResult.Cancel;

            // Enter = Snimi, Esc = Otkaži
            AcceptButton = btnSnimi;
            CancelButton = btnOtkazi;

            Controls.Add(lbl);
            Controls.Add(txtNaziv);
            Controls.Add(btnSnimi);
            Controls.Add(btnOtkazi);
        }

        /// <summary>
        /// Učitava naziv žanra iz baze u polje (samo kod uređivanja).
        /// </summary>
        private void UcitajZanr()
        {
            SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
            pId.Value = zanrId.Value;

            DataTable t = Db.Upit("SELECT Naziv FROM Zanrovi WHERE ZanrID = @id", pId);
            if (t.Rows.Count == 0)
            {
                nijePronadjen = true;
                return;
            }
            txtNaziv.Text = Convert.ToString(t.Rows[0]["Naziv"]);
        }

        /// <summary>
        /// Provjerava unos i snima žanr (INSERT ili UPDATE). Duplikat naziva daje grešku 2627/2601.
        /// </summary>
        private void Snimi()
        {
            string naziv = txtNaziv.Text.Trim();

            // Validacija: naziv je obavezan
            if (naziv.Length == 0)
            {
                Greska("Naziv je obavezan.");
                return;
            }

            try
            {
                SqlParameter pNaziv = new SqlParameter("@naziv", SqlDbType.NVarChar, 50);
                pNaziv.Value = naziv;

                if (zanrId.HasValue)
                {
                    // Uređivanje postojećeg žanra
                    SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                    pId.Value = zanrId.Value;
                    Db.Izvrsi("UPDATE Zanrovi SET Naziv = @naziv WHERE ZanrID = @id", pNaziv, pId);
                }
                else
                {
                    // Unos novog žanra
                    Db.Izvrsi("INSERT INTO Zanrovi (Naziv) VALUES (@naziv)", pNaziv);
                }

                // OK zatvara formu, a pregled zna da treba osvježiti tabelu
                DialogResult = DialogResult.OK;
            }
            catch (SqlException ex)
            {
                // 2627 / 2601 = kršenje UNIQUE ograničenja (naziv već postoji)
                if (ex.Number == 2627 || ex.Number == 2601)
                    Greska("Žanr s tim nazivom već postoji.");
                else
                    Greska("Greška baze: " + ex.Message);
            }
        }

        /// <summary>
        /// Prikazuje poruku o grešci i vraća fokus na polje.
        /// </summary>
        private void Greska(string poruka)
        {
            MessageBox.Show(poruka, "Upozorenje", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtNaziv.Focus();
        }
    }
}