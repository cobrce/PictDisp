using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictDisp
{
    public partial class RecentForm : Form
    {
        public int Selected;
        private SaveData savedata;
        public RecentForm(SaveData savedata)
        {
            this.savedata = savedata;
            InitializeComponent();
        }

        private void RecentForm_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox1.Items.AddRange(savedata.EntriesDict.Keys.ToArray());
        }
        private void SelectEntry()
        {
            if (listBox1.SelectedIndex == -1) return;
            this.DialogResult = DialogResult.OK;
            Selected = listBox1.SelectedIndex;
            Close();
        }
        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            SelectEntry();
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    SelectEntry();
                    break;
                case Keys.Escape:
                    Close();
                    break;
            }
        }
    }
}
