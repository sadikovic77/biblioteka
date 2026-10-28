using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Pregled aktivnih (nevraćenih) posudbi s filterom "samo kašnjenja",
    /// te dugmad za novu posudbu i vraćanje knjige.
    /// </summary>
    public class FormPosudbe : Form
    {
        private CheckBox chkKasnjenja;
        private DataGridView dgv;
        private Button btnPosudi;
        private Button btnVrati;
        private Button btnZatvori;

        /// <summary>
        /// Konstruktor: gradi izgled forme i učitava posudbe.
        /// </summary>
        public FormPosudbe()
        {
            InitializeUI();
            Ucitaj();
        }

        /// <summary>
        /// Pravi sve kontrole u kodu (bez Designera).
        /// </summary>
        private void InitializeUI()
        {
            Text = "Posudbe";
            Size = new Size(900, 460);
            StartPosition = FormStartPosition.CenterParent;

            // Gornji panel s filterom i dugmadima
            Panel panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 50;

            chkKasnjenja = new CheckBox();
            chkKasnjenja.Text = "Samo kašnjenja";
            chkKasnjenja.Location = new Point(10, 15);
            chkKasnjenja.AutoSize = true;
            // Svaka promjena kvačice odmah osvježava tabelu
            chkKasnjenja.CheckedChanged += (s, e) => Ucitaj();

            btnPosudi = NovoDugme("Posudi", 200);
            btnVrati = NovoDugme("Vrati", 280);
            btnZatvori = NovoDugme("Zatvori", 360);

            btnPosudi.Click += (s, e) => Posudi();
            btnVrati.Click += (s, e) => Vrati();
            btnZatvori.Click += (s, e) => Close();

            panel.Controls.Add(chkKasnjenja);
            panel.Controls.Add(btnPosudi);
            panel.Controls.Add(btnVrati);
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

            // Skrivena ID kolona (treba nam za vraćanje)
            DataGridViewTextBoxColumn colId = new DataGridViewTextBoxColumn();
            colId.Name = "PosudbaID";
            colId.DataPropertyName = "PosudbaID";
            colId.Visible = false;
            dgv.Columns.Add(colId);

            dgv.Columns.Add(NovaKolona("Naslov", "Knjiga", null));
            dgv.Columns.Add(NovaKolona("Clan", "Član", null));
            dgv.Columns.Add(NovaKolona("DatumPosudbe", "Posuđeno", "dd.MM.yyyy"));
            dgv.Columns.Add(NovaKolona("RokVracanja", "Rok vraćanja", "dd.MM.yyyy"));
            dgv.Columns.Add(NovaKolona("Status", "Status", null));

            // Nakon svakog punjenja tabele kasnjenja bojimo crveno
            dgv.DataBindingComplete += (s, e) => ObojiKasnjenja();

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
        /// Pomoćna metoda: pravi kolonu vezanu za polje iz upita (format je opcionalan).
        /// </summary>
        private DataGridViewTextBoxColumn NovaKolona(string polje, string naslov, string format)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn();
            c.Name = polje;
            c.DataPropertyName = polje;
            c.HeaderText = naslov;
            if (format != null) c.DefaultCellStyle.Format = format;
            return c;
        }

        /// <summary>
        /// Učitava aktivne posudbe (DatumVracanja je NULL). Ako je kvačica uključena,
        /// prikazuju se samo one kojima je rok prošao.
        /// </summary>
        private void Ucitaj()
        {
            // Dio upita koji dodajemo je fiksan tekst (ne dolazi od korisnika), pa nije opasan
            string filter = chkKasnjenja.Checked
                ? " AND p.RokVracanja < CAST(GETDATE() AS DATE)"
                : "";

            dgv.DataSource = Db.Upit(
                "SELECT p.PosudbaID, k.Naslov, c.Ime + ' ' + c.Prezime AS Clan, " +
                "p.DatumPosudbe, p.RokVracanja, " +
                "CASE WHEN p.RokVracanja < CAST(GETDATE() AS DATE) " +
                "THEN 'Kasni ' + CAST(DATEDIFF(DAY, p.RokVracanja, CAST(GETDATE() AS DATE)) AS NVARCHAR(10)) + ' dana' " +
                "ELSE 'U roku' END AS Status " +
                "FROM Posudbe p " +
                "JOIN Knjige k ON k.KnjigaID = p.KnjigaID " +
                "JOIN Clanovi c ON c.ClanID = p.ClanID " +
                "WHERE p.DatumVracanja IS NULL" + filter +
                " ORDER BY p.RokVracanja");
        }

        /// <summary>
        /// Boji crvenom redove čiji status počinje s "Kasni".
        /// </summary>
        private void ObojiKasnjenja()
        {
            foreach (DataGridViewRow red in dgv.Rows)
            {
                string status = Convert.ToString(red.Cells["Status"].Value);
                if (status.StartsWith("Kasni"))
                    red.DefaultCellStyle.ForeColor = Color.Firebrick;
            }
        }

        /// <summary>
        /// Otvara dijalog za novu posudbu.
        /// </summary>
        private void Posudi()
        {
            using (FormPosudiDetalji f = new FormPosudiDetalji())
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Evidentira vraćanje označene posudbe (upisuje današnji datum).
        /// </summary>
        private void Vrati()
        {
            if (dgv.CurrentRow == null)
            {
                MessageBox.Show("Prvo označite posudbu.", "Upozorenje",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int id = Convert.ToInt32(dgv.CurrentRow.Cells["PosudbaID"].Value);
            string naslov = Convert.ToString(dgv.CurrentRow.Cells["Naslov"].Value);

            DialogResult odgovor = MessageBox.Show(
                "Evidentirati vraćanje knjige \"" + naslov + "\"?", "Potvrda vraćanja",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (odgovor != DialogResult.Yes) return;

            try
            {
                SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                pId.Value = id;

                // Uvjet DatumVracanja IS NULL sprječava dvostruko vraćanje iste posudbe
                int promijenjeno = Db.Izvrsi(
                    "UPDATE Posudbe SET DatumVracanja = CAST(GETDATE() AS DATE) " +
                    "WHERE PosudbaID = @id AND DatumVracanja IS NULL", pId);

                if (promijenjeno == 0)
                    MessageBox.Show("Ova posudba je već vraćena.", "Upozorenje",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                Ucitaj();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Greška baze: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}