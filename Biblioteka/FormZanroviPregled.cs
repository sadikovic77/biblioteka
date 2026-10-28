using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Pregled žanrova: tabela, pretraga uživo te dugmad za dodavanje, uređivanje i brisanje.
    /// </summary>
    public class FormZanroviPregled : Form
    {
        private TextBox txtPretraga;
        private DataGridView dgv;
        private Button btnDodaj;
        private Button btnUredi;
        private Button btnObrisi;
        private Button btnZatvori;

        /// <summary>
        /// Konstruktor: gradi izgled forme i učitava žanrove.
        /// </summary>
        public FormZanroviPregled()
        {
            InitializeUI();
            Ucitaj();
        }

        /// <summary>
        /// Pravi sve kontrole u kodu (bez Designera).
        /// </summary>
        private void InitializeUI()
        {
            Text = "Žanrovi";
            Size = new Size(640, 440);
            StartPosition = FormStartPosition.CenterParent;

            // Gornji panel s pretragom i dugmadima
            Panel panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 50;

            Label lblPretraga = new Label();
            lblPretraga.Text = "Pretraga:";
            lblPretraga.Location = new Point(10, 17);
            lblPretraga.AutoSize = true;

            txtPretraga = new TextBox();
            txtPretraga.Location = new Point(70, 14);
            txtPretraga.Width = 180;
            // Pretraga radi uživo, pri svakoj promjeni teksta
            txtPretraga.TextChanged += (s, e) => Ucitaj();

            btnDodaj = NovoDugme("Dodaj", 270);
            btnUredi = NovoDugme("Uredi", 350);
            btnObrisi = NovoDugme("Obriši", 430);
            btnZatvori = NovoDugme("Zatvori", 510);

            btnDodaj.Click += (s, e) => Dodaj();
            btnUredi.Click += (s, e) => Uredi();
            btnObrisi.Click += (s, e) => Obrisi();
            btnZatvori.Click += (s, e) => Close();

            panel.Controls.Add(lblPretraga);
            panel.Controls.Add(txtPretraga);
            panel.Controls.Add(btnDodaj);
            panel.Controls.Add(btnUredi);
            panel.Controls.Add(btnObrisi);
            panel.Controls.Add(btnZatvori);

            // Tabela: samo za čitanje, selektuje se cijeli red
            dgv = new DataGridView();
            dgv.Dock = DockStyle.Fill;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoGenerateColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Skrivena ID kolona (treba nam za uređivanje i brisanje)
            DataGridViewTextBoxColumn colId = new DataGridViewTextBoxColumn();
            colId.Name = "ZanrID";
            colId.DataPropertyName = "ZanrID";
            colId.Visible = false;

            DataGridViewTextBoxColumn colNaziv = new DataGridViewTextBoxColumn();
            colNaziv.Name = "Naziv";
            colNaziv.HeaderText = "Naziv";
            colNaziv.DataPropertyName = "Naziv";

            DataGridViewTextBoxColumn colBroj = new DataGridViewTextBoxColumn();
            colBroj.Name = "BrojKnjiga";
            colBroj.HeaderText = "Broj knjiga";
            colBroj.DataPropertyName = "BrojKnjiga";

            dgv.Columns.Add(colId);
            dgv.Columns.Add(colNaziv);
            dgv.Columns.Add(colBroj);

            // Dupli klik na red otvara uređivanje
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Uredi(); };

            // Redoslijed je bitan: prvo tabela, pa panel (da panel ostane na vrhu)
            Controls.Add(dgv);
            Controls.Add(panel);
        }

        /// <summary>
        /// Pomoćna metoda: pravi dugme zadate širine na zadatoj X poziciji.
        /// </summary>
        private Button NovoDugme(string tekst, int x)
        {
            Button b = new Button();
            b.Text = tekst;
            b.Location = new Point(x, 11);
            b.Size = new Size(75, 28);
            return b;
        }

        /// <summary>
        /// Učitava žanrove iz baze (s filterom pretrage) u tabelu.
        /// </summary>
        private void Ucitaj()
        {
            // Parametar pretrage (nikad spajanje stringova u SQL)
            SqlParameter p = new SqlParameter("@p", SqlDbType.NVarChar, 100);
            p.Value = "%" + txtPretraga.Text.Trim() + "%";

            // Podupit računa koliko knjiga ima svaki žanr
            dgv.DataSource = Db.Upit(
                "SELECT z.ZanrID, z.Naziv, " +
                "(SELECT COUNT(*) FROM Knjige k WHERE k.ZanrID = z.ZanrID) AS BrojKnjiga " +
                "FROM Zanrovi z WHERE z.Naziv LIKE @p ORDER BY z.Naziv", p);
        }

        /// <summary>
        /// Vraća ID označenog žanra ili null ako ništa nije označeno.
        /// </summary>
        private int? OdabraniId()
        {
            if (dgv.CurrentRow == null) return null;
            return Convert.ToInt32(dgv.CurrentRow.Cells["ZanrID"].Value);
        }

        /// <summary>
        /// Otvara formu za unos novog žanra.
        /// </summary>
        private void Dodaj()
        {
            using (FormZanroviDetalji f = new FormZanroviDetalji(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Otvara formu za uređivanje označenog žanra.
        /// </summary>
        private void Uredi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite žanr.", "Upozorenje",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FormZanroviDetalji f = new FormZanroviDetalji(id))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Briše označeni žanr nakon potvrde. Ako žanr ima knjige, baza javlja grešku 547.
        /// </summary>
        private void Obrisi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite žanr.", "Upozorenje",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string naziv = Convert.ToString(dgv.CurrentRow.Cells["Naziv"].Value);
            DialogResult odgovor = MessageBox.Show(
                "Obrisati žanr \"" + naziv + "\"?", "Potvrda brisanja",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (odgovor != DialogResult.Yes) return;

            try
            {
                SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                pId.Value = id.Value;
                Db.Izvrsi("DELETE FROM Zanrovi WHERE ZanrID = @id", pId);
                Ucitaj();
            }
            catch (SqlException ex)
            {
                // 547 = kršenje stranog ključa (žanr se koristi u tabeli Knjige)
                if (ex.Number == 547)
                    MessageBox.Show("Žanr ima knjige i ne može se obrisati.", "Brisanje nije moguće",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("Greška baze: " + ex.Message, "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}