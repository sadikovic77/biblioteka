using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Biblioteka
{
    /// <summary>
    /// Pregled autora: tabela, pretraga i dugmad Dodaj / Uredi / Obriši / Osvježi.
    /// Ovo je ŠABLON po kojem se prave pregledi za ostale tabele.
    /// </summary>
    public class FormAutoriPregled : Form
    {
        private DataGridView dgv;
        private TextBox txtPretraga;
        private Button btnDodaj, btnUredi, btnObrisi, btnOsvjezi;

        /// <summary>
        /// Konstruktor: gradi sučelje i učitava podatke.
        /// </summary>
        public FormAutoriPregled()
        {
            InitializeUI();
            Ucitaj();
        }

        /// <summary>
        /// Kreira kontrole: gornji panel s pretragom i dugmadima te tabelu.
        /// </summary>
        private void InitializeUI()
        {
            Text = "Autori - pregled";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(800, 500);

            // Gornji panel s pretragom i dugmadima
            Panel panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 50;

            Label lblPretraga = new Label();
            lblPretraga.Text = "Pretraga:";
            lblPretraga.Location = new Point(10, 17);
            lblPretraga.AutoSize = true;

            txtPretraga = new TextBox();
            txtPretraga.Location = new Point(80, 14);
            txtPretraga.Width = 200;
            // Pretraga radi uživo: svaki upisani znak ponovo filtrira tabelu
            txtPretraga.TextChanged += (s, e) => Ucitaj();

            btnDodaj = NapraviDugme("Dodaj", 300);
            btnUredi = NapraviDugme("Uredi", 390);
            btnObrisi = NapraviDugme("Obriši", 480);
            btnOsvjezi = NapraviDugme("Osvježi", 570);

            btnDodaj.Click += (s, e) => Dodaj();
            btnUredi.Click += (s, e) => Uredi();
            btnObrisi.Click += (s, e) => Obrisi();
            btnOsvjezi.Click += (s, e) => Ucitaj();

            panel.Controls.Add(lblPretraga);
            panel.Controls.Add(txtPretraga);
            panel.Controls.Add(btnDodaj);
            panel.Controls.Add(btnUredi);
            panel.Controls.Add(btnObrisi);
            panel.Controls.Add(btnOsvjezi);

            // Tabela: samo za čitanje, cijeli red se označava, kolone ručno definisane
            dgv = new DataGridView();
            dgv.Dock = DockStyle.Fill;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.RowHeadersVisible = false;
            dgv.AutoGenerateColumns = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // ID kolona je skrivena, ali nam treba da znamo koji red uređujemo/brišemo
            DodajKolonu("AutorID", "ID", false);
            DodajKolonu("Ime", "Ime", true);
            DodajKolonu("Prezime", "Prezime", true);
            DodajKolonu("Drzava", "Država", true);
            DodajKolonu("GodinaRodjenja", "Godina rođenja", true);

            // Dupli klik na red otvara uređivanje (RowIndex >= 0 isključuje klik na zaglavlje)
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Uredi(); };

            // Fill kontrola ide prva, panel poslije, da panel ostane na vrhu
            Controls.Add(dgv);
            Controls.Add(panel);
        }

        /// <summary>
        /// Pravi dugme jednake veličine na zadatoj horizontalnoj poziciji.
        /// </summary>
        /// <param name="tekst">Natpis na dugmetu.</param>
        /// <param name="x">Horizontalna pozicija u panelu.</param>
        private Button NapraviDugme(string tekst, int x)
        {
            Button b = new Button();
            b.Text = tekst;
            b.Location = new Point(x, 11);
            b.Size = new Size(80, 28);
            return b;
        }

        /// <summary>
        /// Dodaje kolonu u tabelu povezanu s kolonom iz DataTable-a.
        /// </summary>
        /// <param name="naziv">Naziv kolone u bazi (i u tabeli).</param>
        /// <param name="zaglavlje">Bosanski naslov kolone.</param>
        /// <param name="vidljiva">Da li se kolona prikazuje korisniku.</param>
        private void DodajKolonu(string naziv, string zaglavlje, bool vidljiva)
        {
            DataGridViewTextBoxColumn k = new DataGridViewTextBoxColumn();
            k.Name = naziv;
            k.DataPropertyName = naziv;
            k.HeaderText = zaglavlje;
            k.Visible = vidljiva;
            dgv.Columns.Add(k);
        }

        /// <summary>
        /// Učitava autore iz baze, uz filtriranje prema tekstu u polju za pretragu.
        /// </summary>
        private void Ucitaj()
        {
            try
            {
                // Parametar sa % znakovima: traži tekst bilo gdje u imenu, prezimenu ili državi
                SqlParameter p = new SqlParameter("@p", SqlDbType.NVarChar, 100);
                p.Value = "%" + txtPretraga.Text.Trim() + "%";

                dgv.DataSource = Db.Upit(
                    "SELECT AutorID, Ime, Prezime, Drzava, GodinaRodjenja FROM Autori " +
                    "WHERE Ime LIKE @p OR Prezime LIKE @p OR Drzava LIKE @p " +
                    "ORDER BY Prezime, Ime", p);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Vraća ID trenutno označenog autora ili null ako ništa nije označeno.
        /// </summary>
        private int? OdabraniId()
        {
            if (dgv.CurrentRow == null) return null;

            object v = dgv.CurrentRow.Cells["AutorID"].Value;
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToInt32(v);
        }

        /// <summary>
        /// Otvara formu za unos novog autora; po uspjehu osvježava tabelu.
        /// </summary>
        private void Dodaj()
        {
            // id = null znači "unos novog"
            using (FormAutoriDetalji f = new FormAutoriDetalji(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Otvara formu za uređivanje označenog autora.
        /// </summary>
        private void Uredi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite autora u tabeli.", "Obavijest",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FormAutoriDetalji f = new FormAutoriDetalji(id))
            {
                if (f.ShowDialog(this) == DialogResult.OK) Ucitaj();
            }
        }

        /// <summary>
        /// Briše označenog autora nakon potvrde. Hvata grešku 547 (povezane knjige).
        /// </summary>
        private void Obrisi()
        {
            int? id = OdabraniId();
            if (id == null)
            {
                MessageBox.Show("Prvo označite autora u tabeli.", "Obavijest",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ime = Convert.ToString(dgv.CurrentRow.Cells["Ime"].Value);
            string prezime = Convert.ToString(dgv.CurrentRow.Cells["Prezime"].Value);

            // Brisanje je nepovratno, zato tražimo potvrdu
            DialogResult odgovor = MessageBox.Show(
                $"Da li ste sigurni da želite obrisati autora {ime} {prezime}?",
                "Potvrda brisanja", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (odgovor != DialogResult.Yes) return;

            try
            {
                SqlParameter pId = new SqlParameter("@id", SqlDbType.Int);
                pId.Value = id.Value;
                Db.Izvrsi("DELETE FROM Autori WHERE AutorID = @id", pId);
                Ucitaj();
            }
            catch (SqlException ex)
            {
                // 547 = prekršen strani ključ: autor još ima knjige (nema kaskadnog brisanja)
                if (ex.Number == 547)
                {
                    MessageBox.Show("Autor ima knjige i ne može se obrisati.\n" +
                        "Prvo obrišite ili promijenite njegove knjige.", "Brisanje nije moguće",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
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