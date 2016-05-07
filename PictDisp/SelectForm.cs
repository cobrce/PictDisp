using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictDisp
{
    public partial class SelectForm : Form
    {
        private string[] files;
        private string folder;
        public string selectedFile ="";
        private string currentfile;
        private TreeNode selectedNode;

        public SelectForm(String[] files,string folder, int current)
        {
            this.currentfile = files[current];
            this.folder = folder;
            this.files = files;
            InitializeComponent();
        }

        private void SelectForm_Load(object sender, EventArgs e)
        {
            splitContainer1.Dock = treeView1.Dock = pictureBox1.Dock = DockStyle.Fill;
            treeView1.AfterSelect += new TreeViewEventHandler((o, a) =>
            {
                string path =treeView1.SelectedNode.Name;
                if (File.Exists(path))
                    pictureBox1.Image = Image.FromFile(path);
            });
            treeView1.Nodes.Add(PopulateTreeNode2(files, "\\"));
            if (selectedNode != null)
            {
                treeView1.SelectedNode = selectedNode;
                treeView1.SelectedNode.Expand();
            }
        }

        // orginal function by kaytes from http://stackoverflow.com/questions/1155977/populate-treeview-from-a-list-of-path?answertab=active#tab-top
        // modified
        private TreeNode PopulateTreeNode2(string[] paths, string pathSeparator)
        {
            if (paths == null)
                return null;

            string root = Path.GetFileName(folder);
            TreeNode thisnode = new TreeNode(root);
            TreeNode currentnode;
            char[] cachedpathseparator = pathSeparator.ToCharArray();
            foreach (string path in paths)
            {
                currentnode = thisnode;
                string[] subpath = path.ToLower().Replace(folder.ToLower(),"").Split(cachedpathseparator).ToArray();
                int last = subpath.Length - 1;
                for (int i = 0; i < subpath.Length; i++)
                {                    
                    if (subpath[i] == "") continue;
                    if (currentnode.Nodes[subpath[i]] == null)
                    {
                        currentnode = currentnode.Nodes.Add((i == last) ? path : subpath[i], subpath[i]);
                        if (i == last && path == currentfile)
                            selectedNode = currentnode;
                    }
                    else
                        currentnode = currentnode.Nodes[subpath[i]];
                }
            }

            return thisnode;
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            
            this.selectedFile = treeView1.SelectedNode.Name;
            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
