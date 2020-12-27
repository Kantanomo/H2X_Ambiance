using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Ambiance
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            try
            {
                StreamReader sr = new StreamReader(Application.StartupPath + "\\Settings.cfg");
                txtMM.Text = sr.ReadLine();
                txtS.Text = sr.ReadLine();
                txtSPS.Text = sr.ReadLine();
                sr.Close();
            }
            catch { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Main Menu (mainmenu.map)|mainmenu.map";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtMM.Text = ofd.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Shared (shared.map)|shared.map";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtS.Text = ofd.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Single Player Shared (single_player_shared.map)|single_player_shared.map";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtSPS.Text = ofd.FileName;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            StreamWriter sw = new StreamWriter(Application.StartupPath + "\\Settings.cfg");
            sw.WriteLine(txtMM.Text);
            sw.WriteLine(txtS.Text);
            sw.WriteLine(txtSPS.Text);
            sw.Close();
            MessageBox.Show("Done!");
        }
    }
}
