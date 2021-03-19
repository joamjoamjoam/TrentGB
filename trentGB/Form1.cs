using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace trentGB
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void loadRomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.ValidateNames = true;
            dlg.Multiselect = false;
            //dlg.ShowDialog();
            //dlg.FileName = "Roms\cpu_instrs.gb";
            dlg.FileName = $"Roms\\06-ld r,r.gb";
            if (dlg.FileName != null && dlg.FileName != "")
            {
                Gameboy gb = null;
                try
                {
                    gb = new Gameboy(new ROM(dlg.FileName), this.pictureBox1);
                }
                catch
                {
                    MessageBox.Show($"Error Reading ROM File {dlg.FileName}");
                }

                try
                {
                    Thread t = new Thread(gb.Start);
                    t.Start();
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Crash Report {ex.GetType().ToString()} -> {ex.Message}");
                }
            }
        }
    }
}
