using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Pregled članova: tabela, pretraga uživo te dugmad za dodavanje, uređivanje i brisanje.
    /// </summary>
    public class FormClanoviPregled : Form
    {
        private TextBox txtPretraga;
        private DataGridView dgv;
        private Button btnDodaj;
        private Button btnUredi;
        private Button btnObrisi;
        private Button btnZatvori;

        /// <summary>
        /// Konstruktor: gradi izgled forme i učitava članove.
        /// </summary>
        public FormClanoviPregled()
        {
            InitializeUI();
            Ucitaj();
        }

        /// <summary>
        /// Pravi sve kontrole u kodu (bez Designera).
        /// </summary>
        private void InitializeUI()
        {
            Text = "Članovi";
            Size = new Size(800, 460);
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
            txtPretraga.Width = 220;
            // Pretraga radi uživo, pri svakoj promjeni teksta
            txtPretraga.TextChanged += (s, e) => Ucitaj();

            btnDodaj = NovoDugme("Dodaj", 320);
            btnUredi = NovoDugme("Uredi", 400);
            btnObrisi = NovoDugme("Obriši", 480);
            btnZatvori = NovoDugme("Zatvori", 560);

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
            colId.Name = "ClanID";
            colId.DataPropertyName = "ClanID";
            colId.Visible = false;
            dgv.Columns.Add(colId);

            dgv.Columns.Add(NovaKolona("Ime", "Ime"));
            dgv.Columns.Add(NovaKolona("Prezime", "Prezime"));
            dgv.Columns.Add(NovaKolona("Email", "Email"));
            dgv.Columns.Add(NovaKolona("Telefon", "Telefon"));

            DataGridViewTextBoxColumn colDatum = NovaKolona("DatumUclanjenja", "Datum učlanjenja");
            colDatum.DefaultCellStyle.Format = "dd.MM.yyyy";
            dgv.Columns.Add(colDatum);

            // Dupli klik na red otvara uređivanje
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Uredi(); };

            // Redoslijed je bitan: prvo tabela, pa panel (da panel ostane na vrhu)
            Controls.Add(dgv);
            Controls.Add(panel);
        }

        /// <summary>
        /// Pomoćna metoda: pravi dugme na zadatoj X poziciji.
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
        /// Pomoćna metoda: pravi kolonu vezanu za istoimeno polje iz upita.
        /// </summary>
        private DataGridViewTextBoxColumn NovaKolona(string polje, string naslov)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn();
            c.Name = polje;
            c.DataPropertyName = polje;
            c.HeaderText = naslov;
            return c;
        }

        /// <summary>
        /// Učitava članove iz baze (s filterom pretrage) u tabelu.
        /// </summary>
        private void Ucitaj()
        {
            // Parametar pretrage (nikad spajanje stringova u SQL)
            SqlParameter p = new SqlParameter("@p", SqlDbType.NVarChar, 100);
            p.Value = "%" + txtPretraga.Text.Trim() + "%";

            dgv.DataSource = Db.Upit(
                "SELECT ClanID, Ime, Prezime, Email, Telefon, DatumUclanjenja " +
                "FROM Clanovi WHERE Ime LIKE @p OR Prezime LIKE @p OR Email LIKE @p " +
                "ORDER BY Prezime, Ime", p);
        }

        /// <summary>
        /// Vraća ID označenog člana ili null ako ništa nije označeno.
        /// </summary>
        private int? OdabraniId()
        {
            if (dgv.CurrentRow == null) return null;
            return Convert.ToInt32(dgv.CurrentRow.Cells["ClanID"].Value);
        }

        /// <summary>
        /// Otvara formu za unos novog člana.
        /// </summary>
        private void Dodaj()
        {
            using (FormClanoviDetalji f = new FormClanoviDetalji(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Otvara formu za uređivanje označenog člana.
        /// </summary>
        private void Uredi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite člana.", "Upozorenje",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FormClanoviDetalji f = new FormClanoviDetalji(id))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Briše označenog člana nakon potvrde. Ako član ima posudbe, baza javlja grešku 547.
        /// </summary>
        private void Obrisi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite člana.", "Upozorenje",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ime = Convert.ToString(dgv.CurrentRow.Cells["Ime"].Value) + " " +
                         Convert.ToString(dgv.CurrentRow.Cells["Prezime"].Value);
            DialogResult odgovor = MessageBox.Show(
                "Obrisati člana \"" + ime + "\"?", "Potvrda brisanja",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (odgovor != DialogResult.Yes) return;

            try
            {
                SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                pId.Value = id.Value;
                Db.Izvrsi("DELETE FROM Clanovi WHERE ClanID = @id", pId);
                Ucitaj();
            }
            catch (SqlException ex)
            {
                // 547 = kršenje stranog ključa (član se koristi u tabeli Posudbe)
                if (ex.Number == 547)
                    MessageBox.Show("Član ima posudbe i ne može se obrisati.", "Brisanje nije moguće",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("Greška baze: " + ex.Message, "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}