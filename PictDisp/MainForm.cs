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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

/*
    version 0.2 update:
    - picturebox is drawn manually
    - when the form is deactivated, the first click only activates it
    - smooth scroll down/up
    - home/end for beginig/end of the picture
    - "last picture" message is shown when no more file
    - press controlkey to switch on/off magnifier
    - added "reload" button (show recent and openfolder form)
*/
namespace PictDisp
{
    public partial class MainForm : Form
    {
        private string folder;
        private string[] files;
        private int current = 0;
        string VersionTitle = "PictDisp 0.2 by COB";
        private SaveData savedata = new SaveData();
        private Brush DarkBrush = new SolidBrush(Color.FromArgb(22, 22, 22));
        //private double Ratio; // ratio between pictureBox1.Width and img.Width (put in global for futur use)
        private string Lastfile;
        private int Y = 0;
        private double ActualProgress;
        private Thread ScrollThread;
        private int currentY;
        private int step;

        string message = "Last picture";
        Font font = new Font("Comic Sans MS", 20);
        private Brush brush = new SolidBrush(Color.FromArgb(220, Color.Black));
        private bool isFullScreen;
        private int maxY;
        private float Gain = 1.5f;
        private bool showMagnifier;
        private Point MouseLocation;


        public MainForm()
        {
            InitializeComponent();

            pictureBox1.Location = new Point(0, 5);

            this.Text = VersionTitle;
            pictureBox1.MouseWheel += new MouseEventHandler((o, e) =>
            {
                ScrollPicture(-e.Delta);
            });
            //panel1.MouseEnter += new EventHandler((o, e) => { panel1.Focus(); });
        }

        // get both the form and all of its controls double-buffered
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;    // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            if (!LoadPictures())
            {
                MessageBox.Show(this, "Closing, no picture found", VersionTitle);
                Environment.Exit(0);
            }
            else
            {
                ScrollThread = new Thread(new ThreadStart(SmoothScroll));
                ScrollThread.Start();
            }
        }
        private bool LoadPictures()
        {
            checkBox1.CheckedChanged -= new EventHandler(this.checkBox1_CheckedChanged);

            if (!RecentFiles())
                if (folderBrowserDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    this.folder = folderBrowserDialog1.SelectedPath;

            checkBox1.CheckedChanged += new EventHandler(this.checkBox1_CheckedChanged);

            if (this.folder != "")
                if (ListPictures())
                {
                    current = files.ToList().IndexOf(Lastfile);
                    ChangePicture(Direction.Actual);
                    return true;
                }
            return false;
        }

        private bool RecentFiles()
        {
            try
            {
                if (ReadSaves())
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

        private bool ReadSaves()
        {
            try
            {
                if (!File.Exists(SavePath)) return false;
                using (FileStream file = File.OpenRead(SavePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
                    SaveData localdata = (SaveData)serializer.Deserialize(file);
                    if (localdata.Entries != null)
                        localdata.ReadEntries();
                    savedata = localdata;
                    return (savedata.Entries.Count > 0);
                    //return true;
                }
            }
            catch { return false; }
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

            // check if we're aleready in the last picture
            if (current == (int)files.Length - 1 && direction == Direction.Next)
            {
                LastPictureMessage = true;
                pictureBox1.Invalidate();
                timer1.Start();
                return;
            }
            current += (int)direction;
            if (current < 0) current = 0;
            if (current >= files.Length) current = (int)files.Length - 1;

            string currentfile = files[current];
            Text = string.Format("{0} - index {1} / {2}", VersionTitle, current + 1, currentfile);

            Bitmap bmp = (Bitmap)Bitmap.FromFile(currentfile);
            if (savedata.Dark) bmp = InvertColor(bmp);

            pictureBox1.Tag = bmp;
            //UpdatePictureBoxSize(false);

            comboBox1.SelectedIndex = current;
            textBox1.Text = (current + 1).ToString();

            Y = 0;
            currentY = 0;
            pictureBox1.Invalidate();
            panel2.Refresh();
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

        //private void UpdatePictureBoxSize(bool changelocation = true)
        //{

        //    Image img = pictureBox1.Image;
        //    if (img == null) return;
        //    pictureBox1.Width = ClientSize.Width;// panel1.Width - 22;

        //    Ratio = (double)pictureBox1.Width / (double)img.Width;
        //    pictureBox1.Height = (int)(Ratio * (double)img.Height);

        //    int maxY = ClientSize.Height - pictureBox1.Height;
        //    if (ClientSize.Height > pictureBox1.Height) maxY = -maxY;

        //    if (changelocation && pictureBox1.Location.Y < maxY)
        //        pictureBox1.Location = new Point(0, maxY);
        //}

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
            pictureBox1.Width = ClientSize.Width;
            pictureBox1.Height = ClientSize.Height - 5;
            //UpdatePictureBoxSize();
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
            ScrollThread.Abort();
            Save();
        }

        private void Save()
        {
            if (folder != "")
            {
                savedata.EntriesDict[folder] = files[current]; // save current file

                //SaveData FinalSaveData = ReadSaves(); // read actual saves (we need only entries)
                //if (FinalSaveData != null)
                //{
                //    foreach (var pair in savedata.EntriesDict) // merge actual and new EntryDict (add/replace)
                //        FinalSaveData.EntriesDict[pair.Key] = pair.Value;
                //    FinalSaveData.Dark = savedata.Dark; // copy actual nightmode setting
                //}
                //else
                //{
                //    FinalSaveData = savedata;
                //}
                SaveData FinalSaveData = savedata;
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

        private void ToggleFullScreen(bool SaveState = true)
        {
            //panel1.AutoScroll = false; // to avoid horizontal scroll bar (I don't know why it appears, but this fix it)
            if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                this.TopMost = false;
                if (SaveState)
                    checkBox2.Checked = false;
                this.isFullScreen = false;
                if (!showMagnifier)
                    CursorShown = true;
            }
            else
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.TopMost = true;
                this.WindowState = FormWindowState.Normal;
                this.WindowState = FormWindowState.Maximized;
                if (SaveState)
                    checkBox2.Checked = true;
                this.isFullScreen = true;
                CursorShown = false;
            }
            button6.Visible = checkBox2.Checked && groupBox1.Visible;
            //panel1.AutoScroll = true;
        }

        private void ShowHideGroupBox()
        {
            groupBox1.Top = 0;
            groupBox1.Visible = !groupBox1.Visible;
            button6.Visible = checkBox2.Checked && groupBox1.Visible;
            if
                (groupBox1.Visible) CursorShown = true;
            else
                if (isFullScreen || showMagnifier)
                    CursorShown = false;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    ScrollPicture(120);
                    break;
                case Keys.Up:
                    ScrollPicture(-120);
                    break;
                case Keys.Home:
                    Gain = 5;
                    Y = 0;
                    break;
                case Keys.End:
                    Gain = 5;
                    Y = maxY;
                    break;
                case Keys.ControlKey:
                    showMagnifier = !showMagnifier;
                    if (showMagnifier)
                        CursorShown = false;
                    else
                        if (groupBox1.Visible || !isFullScreen)
                            CursorShown = true;
                    pictureBox1.Invalidate();
                    break;
            }
        }

        public void ScrollPicture(int step)
        {
            Gain = 1.5f;
            Y += step;
        }

        private void SmoothScroll()
        {
            while (true)
            {
                if (Y < 0) Y = 0;
                int err = Y - currentY;

                step = (int)(Gain * (int)Math.Pow(err > 0 ? err : -err, 1.0 / 3.0));
                if (err < 0 && step > 0) step = -step;

                if (step != 0)
                {
                    currentY += step;
                    pictureBox1.Invalidate();
                    panel2.Invalidate();
                    Thread.Sleep(5);
                }
            }
        }

        private void groupBox1_LocationChanged(object sender, EventArgs e)
        {
            groupBox1.Top = 0;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            //panel1.Size = ClientSize;
            pictureBox1.Width = ClientSize.Width;
            pictureBox1.Height = ClientSize.Height - 5;
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(DarkBrush, panel2.ClientRectangle);
            /*ActualProgress =-(double)Y(pictureBox1.Location.Y-5) / (double)(pictureBox1.Height - ClientSize.Height+5);*/
            // panel1.ClientSize.Height);
            int ProgressBarWidth = (int)(ActualProgress * panel2.Width);
            e.Graphics.FillRectangle(Brushes.DodgerBlue, new Rectangle(0, 0, ProgressBarWidth, 5));
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
                if (value > 0 && value <= files.Length)
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

        private void MainForm_Activated(object sender, EventArgs e)
        {
            pictureBox1.Enabled = true;
            if (checkBox2.Checked && !isFullScreen)
                ToggleFullScreen(false);
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            pictureBox1.Enabled = false;
            if (isFullScreen)
                ToggleFullScreen(false);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox1.Tag == null || !(pictureBox1.Tag is Image)) return;

            Image img = (Image)pictureBox1.Tag;

            double ratio = (double)img.Width / (double)pictureBox1.Width;
            int width = img.Width;
            int height = (int)((double)pictureBox1.Height * ratio);

            maxY = img.Height - height;
            if (maxY < 0) maxY = 0;
            if (Y > maxY) Y = maxY;
			
            if (currentY < 0) currentY = 0;
            else if (currentY > maxY) currentY = maxY;

            ActualProgress = (double)currentY / (double)maxY;

            Rectangle rect = new Rectangle(0, currentY, width, height);

            if (step == 0)
            {
                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            }
            else
            {
                e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
                e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
            }

            e.Graphics.CompositingQuality = (step != 0) ? CompositingQuality.HighSpeed : CompositingQuality.HighQuality;
            e.Graphics.DrawImage(img, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height), rect, GraphicsUnit.Pixel);

            if (showMagnifier)
            {
                float magnifierHeight = pictureBox1.Width / 4f;
                float magnifierWidth = pictureBox1.Width / 3f;
                float zoom = 2f;

                RectangleF source = new RectangleF((float)((MouseLocation.X - magnifierWidth / (2 * zoom)) * ratio), (float)((MouseLocation.Y - magnifierHeight / (2 * zoom)) * ratio + currentY), (float)(magnifierWidth / zoom * ratio), (float)(magnifierHeight / zoom * ratio));
                RectangleF dest = new RectangleF(MouseLocation.X - magnifierWidth / 2, MouseLocation.Y - magnifierHeight / 2, magnifierWidth, magnifierHeight);
                
                #region glow
                // most code from http://stackoverflow.com/a/11691081/5822322
                // hints about focusScales from : https://www.gittprogram.com/question/315860_how-to-create-halo-glow-light-effect-i-share-solution.html
                float distance = 16f;
                RectangleF glowrect = dest;
                glowrect.Inflate(distance / 2, distance / 2);
                glowrect.Offset(-1, 1);

                GraphicsPath p = new GraphicsPath();
                float diameter = 10f;
                p.AddArc(new RectangleF(glowrect.X, glowrect.Y, diameter, diameter), 180f, 90f);
                p.AddArc(new RectangleF(glowrect.X + glowrect.Width - diameter, glowrect.Y, diameter, diameter), 270f, 90f);
                p.AddArc(new RectangleF(glowrect.X + glowrect.Width - diameter, glowrect.Y + glowrect.Height - diameter, diameter, diameter), 0f, 90f);
                p.AddArc(new RectangleF(glowrect.X, glowrect.Y + glowrect.Height - diameter, diameter, diameter), 90f, 90f);
                //p.AddRectangle(glowrect);
                p.CloseFigure();

                PathGradientBrush pthGrBrush = new PathGradientBrush(p);

                pthGrBrush.CenterColor = savedata.Dark ? Color.White : Color.Black;//Color.FromArgb(255, 0, 0, 255);     
                Color[] colors = { Color.Transparent };
                pthGrBrush.SurroundColors = colors;
                //float[] blendFactors = { 0.0f, 0.1f, 0.3f, 1.0f };
                //float[] blendPos = { 0.0f, 0.4f, 0.6f, 1.0f };
                //pthGrBrush.Blend.Factors = blendFactors;
                //pthGrBrush.Blend.Positions = blendPos;
                pthGrBrush.FocusScales =  new PointF((glowrect.Width - distance) / glowrect.Width, (glowrect.Height - distance) / glowrect.Height);

                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;
                e.Graphics.FillPath(pthGrBrush, p);
                #endregion

                e.Graphics.DrawImage(img, dest, source, GraphicsUnit.Pixel);
                e.Graphics.DrawRectangle(Pens.Black, dest.X, dest.Y, dest.Width, dest.Height);
            }

            if (LastPictureMessage)
            {
                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                SizeF messageSize = e.Graphics.MeasureString(message, font);
                PointF location = new PointF((pictureBox1.Width - messageSize.Width) / 2, pictureBox1.Height - messageSize.Height - 10);

                GraphicsPath path = new GraphicsPath();
                RectangleF arcRectangle = new RectangleF(location.X - messageSize.Height / 2, location.Y, (messageSize.Height), messageSize.Height);
                path.AddArc(arcRectangle, 90, 180);
                arcRectangle.X += messageSize.Width;
                path.AddArc(arcRectangle, -90, 180);
                path.CloseFigure();

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                PathGradientBrush grad = new PathGradientBrush(path);
                grad.CenterColor = Color.FromArgb(220, Color.Black);
                grad.SurroundColors = new Color[]{Color.Transparent};

                float pathWidth = arcRectangle.Width + 2f * arcRectangle.Height;
                float distance = 10f;
                grad.FocusScales = new PointF((pathWidth - distance) / pathWidth, (arcRectangle.Height - distance * arcRectangle.Width / arcRectangle.Height) / arcRectangle.Height);
                e.Graphics.FillPath(grad, path);

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.DrawString(message, font, Brushes.Black, location);
                location.X -= 2;
                location.Y -= 2;
                e.Graphics.DrawString(message, font, Brushes.White, location);
            }
        }

        public bool LastPictureMessage { get; set; }

        private void timer1_Tick(object sender, EventArgs e)
        {
            LastPictureMessage = false;
            pictureBox1.Invalidate();
            timer1.Stop();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            MouseLocation = e.Location;
            if (showMagnifier)
            {
                pictureBox1.Refresh();
                pictureBox1.Update();
            }
        }

        // http://stackoverflow.com/questions/7639502/why-do-cursor-show-and-cursor-hide-not-immediately-hide-or-show-the-cursor
        private bool _CursorShown = true;
        public bool CursorShown
        {
            get
            {
                return _CursorShown;
            }
            set
            {
                if (value == _CursorShown)
                    return;
                if (value)
                    Cursor.Show();
                else
                    Cursor.Hide();
                _CursorShown = value;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Save();
            LoadPictures();
        }
    }
}
