using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace PictDisp
{
    public partial class MainForm : Form
    {
        private string folder;
        private string[] files;
        private int current = 0;
        string VersionTitle = "PictDisp 0.1 by COB";
        private SaveData savedata = new SaveData();
        private Brush DarkBrush = new SolidBrush(Color.FromArgb(22, 22, 22));
        private double Ratio; // ratio between pictureBox1.Width and img.Width (put in global for futur use)
        private string Lastfile;

        public MainForm()
        {
            InitializeComponent();
            this.Text = VersionTitle;
            pictureBox1.MouseWheel += new MouseEventHandler((o, e) =>
            {
                if (e.Delta > 0)
                    ScrollUp(e.Delta);
                else
                    ScrollDown(-e.Delta);

            });
            //panel1.MouseEnter += new EventHandler((o, e) => { panel1.Focus(); });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!RecentFiles())
                if (folderBrowserDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    this.folder = folderBrowserDialog1.SelectedPath;

            checkBox1.CheckedChanged += new EventHandler(this.checkBox1_CheckedChanged);

            if (this.folder != "")
                if (ListPictures())
                {
                    current = files.ToList().IndexOf(Lastfile);
                    ChangePicture(Direction.Actual);
                    return;
                }
            MessageBox.Show(this, "Closing, no picture found", VersionTitle);
            Environment.Exit(0);
        }

        private bool RecentFiles()
        {
            try
            {
                savedata = ReadSaves();
                if (savedata != null)
                {
                    checkBox1.Checked = savedata.Dark;
                    if (savedata.Entries != null)
                    {
                        RecentForm recentForm = new RecentForm(savedata);
                        if (recentForm.ShowDialog(this) == DialogResult.OK)
                        {
                            var entry = savedata.EntriesDict.ElementAt(recentForm.Selected);
                            this.folder = entry.Key;
                            this.Lastfile = entry.Value; 
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private SaveData ReadSaves()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                using (FileStream file = File.OpenRead(SavePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
                    SaveData localdata = (SaveData)serializer.Deserialize(file);
                    if (localdata.Entries != null)
                        localdata.ReadEntries();
                    return localdata;
                }
            }
            catch { return null; }
        }

        enum Direction
        {
            Actual = 0,
            Next = +1,
            Previous = -1
        };
        private void ChangePicture(Direction direction)
        {
            //panel1.ScrollControlIntoView(pictureBox1);
            pictureBox1.Location = new Point(0, 5);

            current += (int)direction;
            if (current < 0) current = 0;
            if (current >= files.Length) current = (int)files.Length - 1;

            string currentfile = files[current];
            Text = string.Format("{0} - index {1} / {2}", VersionTitle, current + 1, currentfile);

            Bitmap bmp = (Bitmap)Bitmap.FromFile(currentfile);
            if (savedata.Dark) bmp = InvertColor(bmp);

            pictureBox1.Image = bmp;
            UpdatePictureBoxSize();

            comboBox1.SelectedIndex = current;
            textBox1.Text = (current + 1).ToString();
        }

       
        // Credit to http://www.vcskicks.com/image-invert.php
        private Bitmap InvertColor(Bitmap bmp)
        {
            Bitmap inverted = new Bitmap(bmp.Width, bmp.Height);

            ColorMatrix clrMatrix = new ColorMatrix(new float[][]
                                                    {
                                                    new float[] {-1, 0, 0, 0, 0},
                                                    new float[] {0, -1, 0, 0, 0},
                                                    new float[] {0, 0, -1, 0, 0},
                                                    new float[] {0, 0, 0, 1, 0},
                                                    new float[] {1, 1, 1, 0, 1}
                                                    });
            using (ImageAttributes attr = new ImageAttributes())
            {
                attr.SetColorMatrix(clrMatrix);
                using (Graphics g = Graphics.FromImage(inverted))
                {
                    g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
                }
            }
            return inverted;
        }

        private void UpdatePictureBoxSize()
        {

            Image img = pictureBox1.Image;
            if (img == null) return;
            pictureBox1.Width = ClientSize.Width;// panel1.Width - 22;

            Ratio = (double)pictureBox1.Width / (double)img.Width;
            pictureBox1.Height = (int)(Ratio * (double)img.Height);

            int maxY = ClientSize.Height - pictureBox1.Height;
            if (ClientSize.Height > pictureBox1.Height) maxY = -maxY;

            if (pictureBox1.Location.Y < maxY)
                pictureBox1.Location = new Point(0, maxY);
        }

        private bool ListPictures()
        {
            try
            {
                string[] filters = ".jpeg|.jpg|.png|.bmp".Split('|');
                files = (from file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories) where (file.ToLower().EndsWith(filters[0]) || file.ToLower().EndsWith(filters[1]) || file.ToLower().EndsWith(filters[2]) || file.ToLower().EndsWith(filters[3])) select file).ToArray();
                if (files.Length == 0)
                    MessageBox.Show(this, "No file found", VersionTitle);
                else
                {
                    comboBox1.Items.AddRange((from f in files select Path.GetFileName(f)).ToArray());
                    return true;                   
                }
            }
            catch { }
            return false;
        }

        private void Form1_ClientSizeChanged(object sender, EventArgs e)
        {
            button6.Left = ClientSize.Width - button6.Width;
            button6.Top = 5;
            UpdatePictureBoxSize();
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            pictureBox1.Focus();
            //panel1.Focus();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    ChangePicture(Direction.Next);
                    break;
                case MouseButtons.Right:
                    ChangePicture(Direction.Previous);
                    break;
                case MouseButtons.Middle:
                    ShowHideGroupBox();
                    break;
            };
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (folder != "")
            {
                savedata.EntriesDict[folder] = files[current]; // save current file

                SaveData FinalSaveData = ReadSaves(); // read actual saves (we need only entries)
                if (FinalSaveData != null)
                {
                    foreach (var pair in savedata.EntriesDict) // merge actual and new EntryDict (add/replace)
                        FinalSaveData.EntriesDict[pair.Key] = pair.Value;
                    FinalSaveData.Dark = savedata.Dark; // copy actual nightmode setting
                }
                else
                {
                    FinalSaveData = savedata;
                }
                FinalSaveData.SetEntries(); // convert dictionarry to list of entries

                using (MemoryStream ms = new MemoryStream())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
                    serializer.Serialize(ms, FinalSaveData);
                    File.WriteAllBytes(SavePath, ms.ToArray());                
                }
                //using (FileStream file = File.OpenWrite(SavePath))
                //{
                //    file.SetLength(0);
                //    XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
                //    serializer.Serialize(file, FinalSaveData);
                //}
            }
        }

        public string SavePath { get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\savedata.xml"; } }

        private void button1_Click(object sender, EventArgs e)
        {
            SelectForm slct = new SelectForm(files, folder, current) { TopMost = this.TopMost };
            if (slct.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (slct.selectedFile != "")
                {
                    int selected = files.ToList().IndexOf(slct.selectedFile);
                    if (selected > 0 && selected < files.Length)
                        current = selected;
                    else
                    {
                        MessageBox.Show(this, "Error selecting file", VersionTitle);
                        return;
                    }
                }
            }
            ChangePicture(Direction.Actual);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            savedata.Dark = checkBox1.Checked;
            ChangePicture(Direction.Actual);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    ShowHideGroupBox();
                    break;
                case Keys.F11:
                    checkBox2.CheckedChanged -= checkBox2_CheckedChanged;
                    checkBox2.Checked = !checkBox2.Checked;
                    ToggleFullScreen();
                    checkBox2.CheckedChanged += checkBox2_CheckedChanged;
                    break;
                case Keys.Left:
                    ChangePicture(Direction.Previous);
                    break;
                case Keys.Right:
                    ChangePicture(Direction.Next);
                    break;
            }
        }

        private void ToggleFullScreen()
        {
            //panel1.AutoScroll = false; // to avoid horizontal scroll bar (I don't know why it appears, but this fix it)
            if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                this.TopMost = false;
                checkBox2.Checked = false;
            }
            else
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.TopMost = true;
                this.WindowState = FormWindowState.Normal;
                this.WindowState = FormWindowState.Maximized;
                checkBox2.Checked = true;
            }
            button6.Visible = checkBox2.Checked && groupBox1.Visible;
            //panel1.AutoScroll = true;
        }

        private void ShowHideGroupBox()
        {
            groupBox1.Top = 0;
            groupBox1.Visible = !groupBox1.Visible;
            button6.Visible = checkBox2.Checked && groupBox1.Visible;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    ScrollDown(/*panel1,*/ 120);
                    break;
                case Keys.Up:
                    ScrollUp(/*panel1,*/ 120);
                    break;
            }
        }
        public void ScrollDown(/*Panel p,*/ int step)
        {
            //using (Control c = new Control() { Parent = p, Height = 1, Top = p.ClientSize.Height + p.ClientSize.Height * step / 100 })
            //{
            //    p.ScrollControlIntoView(c);
            //}
            if (ClientSize.Height > pictureBox1.Height) return;

            int newY = pictureBox1.Location.Y - step;
            newY = Math.Max(newY, ClientSize.Height - pictureBox1.Height);
            pictureBox1.Location = new Point(pictureBox1.Location.X, newY);
        }
        public void ScrollUp(/*Panel p,*/ int step)
        {
            int newY = pictureBox1.Location.Y + step;
            if (newY > 5) newY = 5;
            pictureBox1.Location = new Point(0, newY);
            //using (Control c = new Control() { Parent = p, Height = 1, Top = -p.ClientSize.Height * step / 100 })
            //{
            //    p.ScrollControlIntoView(c);
            //}
        }

        private void groupBox1_LocationChanged(object sender, EventArgs e)
        {
            groupBox1.Top = 0;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            //panel1.Size = ClientSize;
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(DarkBrush, panel2.ClientRectangle);
            double ActualProgress = -(double)(pictureBox1.Location.Y-5) / (double)(pictureBox1.Height - ClientSize.Height+5);// panel1.ClientSize.Height);
            int ProgressBarWidth = (int)(ActualProgress * panel2.Width);
            e.Graphics.FillRectangle(Brushes.DodgerBlue, new Rectangle(0, 0, ProgressBarWidth, 5));
        }

        private void pictureBox1_LocationChanged(object sender, EventArgs e)
        {
            panel2.Invalidate();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ChangePicture(Direction.Previous);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ChangePicture(Direction.Next);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            current = 0;
            ChangePicture(Direction.Actual);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            current = files.Length;
            ChangePicture(Direction.Actual);
        }

        private void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int value = int.Parse(textBox1.Text);
                if (value >0 && value <= files.Length)
                {
                    current = value - 1;
                    ChangePicture(Direction.Actual);
                }
            }
                
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsControl(e.KeyChar) && !Char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            current = comboBox1.SelectedIndex;
            ChangePicture(Direction.Actual);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
