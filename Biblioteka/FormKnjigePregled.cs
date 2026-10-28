using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Pregled knjiga: tabela s autorom, žanrom i brojem dostupnih primjeraka,
    /// pretraga uživo te dugmad za dodavanje, uređivanje i brisanje.
    /// </summary>
    public class FormKnjigePregled : Form
    {
        private TextBox txtPretraga;
        private DataGridView dgv;
        private Button btnDodaj;
        private Button btnUredi;
        private Button btnObrisi;
        private Button btnZatvori;

        /// <summary>
        /// Konstruktor: gradi izgled forme i učitava knjige.
        /// </summary>
        public FormKnjigePregled()
        {
            InitializeUI();
            Ucitaj();
        }

        /// <summary>
        /// Pravi sve kontrole u kodu (bez Designera).
        /// </summary>
        private void InitializeUI()
        {
            Text = "Knjige";
            Size = new Size(980, 480);
            StartPosition = FormStartPosition.CenterParent;

            // Gornji panel s pretragom i dugmadima
            Panel panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 50;

            Label lblPretraga = new Label();
            lblPretraga.Text = "Pretraga (naslov, autor, žanr, ISBN):";
            lblPretraga.Location = new Point(10, 17);
            lblPretraga.AutoSize = true;

            txtPretraga = new TextBox();
            txtPretraga.Location = new Point(240, 14);
            txtPretraga.Width = 240;
            // Pretraga radi uživo, pri svakoj promjeni teksta
            txtPretraga.TextChanged += (s, e) => Ucitaj();

            btnDodaj = NovoDugme("Dodaj", 510);
            btnUredi = NovoDugme("Uredi", 590);
            btnObrisi = NovoDugme("Obriši", 670);
            btnZatvori = NovoDugme("Zatvori", 750);

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
            colId.Name = "KnjigaID";
            colId.DataPropertyName = "KnjigaID";
            colId.Visible = false;
            dgv.Columns.Add(colId);

            dgv.Columns.Add(NovaKolona("Naslov", "Naslov"));
            dgv.Columns.Add(NovaKolona("Autor", "Autor"));
            dgv.Columns.Add(NovaKolona("Zanr", "Žanr"));
            dgv.Columns.Add(NovaKolona("GodinaIzdanja", "Godina"));
            dgv.Columns.Add(NovaKolona("ISBN", "ISBN"));
            dgv.Columns.Add(NovaKolona("UkupnoPrimjeraka", "Ukupno"));
            dgv.Columns.Add(NovaKolona("Dostupno", "Dostupno"));

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
        /// Učitava knjige iz baze (s filterom pretrage) u tabelu.
        /// Dostupno = ukupno primjeraka minus broj posudbi koje još nisu vraćene.
        /// </summary>
        private void Ucitaj()
        {
            // Parametar pretrage (nikad spajanje stringova u SQL)
            SqlParameter p = new SqlParameter("@p", SqlDbType.NVarChar, 200);
            p.Value = "%" + txtPretraga.Text.Trim() + "%";

            dgv.DataSource = Db.Upit(
                "SELECT k.KnjigaID, k.Naslov, " +
                "a.Ime + ' ' + a.Prezime AS Autor, " +
                "z.Naziv AS Zanr, k.GodinaIzdanja, k.ISBN, k.UkupnoPrimjeraka, " +
                "k.UkupnoPrimjeraka - (SELECT COUNT(*) FROM Posudbe po " +
                "WHERE po.KnjigaID = k.KnjigaID AND po.DatumVracanja IS NULL) AS Dostupno " +
                "FROM Knjige k " +
                "JOIN Autori a ON a.AutorID = k.AutorID " +
                "JOIN Zanrovi z ON z.ZanrID = k.ZanrID " +
                "WHERE k.Naslov LIKE @p OR (a.Ime + ' ' + a.Prezime) LIKE @p " +
                "OR z.Naziv LIKE @p OR k.ISBN LIKE @p " +
                "ORDER BY k.Naslov", p);
        }

        /// <summary>
        /// Vraća ID označene knjige ili null ako ništa nije označeno.
        /// </summary>
        private int? OdabraniId()
        {
            if (dgv.CurrentRow == null) return null;
            return Convert.ToInt32(dgv.CurrentRow.Cells["KnjigaID"].Value);
        }

        /// <summary>
        /// Otvara formu za unos nove knjige.
        /// </summary>
        private void Dodaj()
        {
            using (FormKnjigeDetalji f = new FormKnjigeDetalji(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Otvara formu za uređivanje označene knjige.
        /// </summary>
        private void Uredi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite knjigu.", "Upozorenje",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FormKnjigeDetalji f = new FormKnjigeDetalji(id))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Briše označenu knjigu nakon potvrde. Ako knjiga ima posudbe, baza javlja grešku 547.
        /// </summary>
        private void Obrisi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite knjigu.", "Upozorenje",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string naslov = Convert.ToString(dgv.CurrentRow.Cells["Naslov"].Value);
            DialogResult odgovor = MessageBox.Show(
                "Obrisati knjigu \"" + naslov + "\"?", "Potvrda brisanja",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (odgovor != DialogResult.Yes) return;

            try
            {
                SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                pId.Value = id.Value;
                Db.Izvrsi("DELETE FROM Knjige WHERE KnjigaID = @id", pId);
                Ucitaj();
            }
            catch (SqlException ex)
            {
                // 547 = kršenje stranog ključa (knjiga se koristi u tabeli Posudbe)
                if (ex.Number == 547)
                    MessageBox.Show("Knjiga ima posudbe i ne može se obrisati.", "Brisanje nije moguće",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("Greška baze: " + ex.Message, "Greška",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}