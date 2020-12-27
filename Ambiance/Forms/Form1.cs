using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HaloMap;

namespace Ambiance
{
    public partial class Form1 : Form
    {
        Map map;
        Sound sound;

        public Form1()
        {
            InitializeComponent();
        }

        #region Basic Form Code
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Halo 2 Xbox Map (*.map)|*.map";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                map = new Map(ofd.FileName);
                map.Read(treeView1);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Chunks.Items.Clear();
            Choices.Items.Clear();
            treeView1.Nodes.Clear();
            lblCompression.Text = "";
            lblFormat.Text = "";
            lblLocation.Text = "";
            lblOffset.Text = "";
            lblSize.Text = "";
            map = null;
            sound = null;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings s = new Settings();
            s.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(".:Ambiance - Perfect Sound Injector:.\n                 By Grimdoomer\n                 Updated By Kantanomo");
        }
        #endregion

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                //Load
                map.SelectedTag = map.TagIndex.FindTag(map, "snd!", treeView1.SelectedNode.Text);
                sound = new Sound();
                sound.Parse(map);

                //Update
                Choices.Items.Clear();
                Chunks.Items.Clear();

                for (int i = 0; i < sound.Permutation.Choices.Count; i++)
                {
                    Choices.Items.Add(map.SIDs[Coconuts.Names[sound.Permutation.Choices[i].NameIndex].Index]);
                }
                for (int i = 0; i < sound.Permutation.Choices[0].SoundChunks.Count; i++)
                {
                    Chunks.Items.Add("Chunk #" + i.ToString());
                }
                Choices.SelectedIndex = 0;
                Chunks.SelectedIndex = 0;

                lblFormat.Text = sound.Format.ToString();
                lblCompression.Text = sound.Compression.ToString();
                lblLocation.Text = sound.Permutation.Choices[0].SoundChunks[0].RawLocation.ToString();
                lblOffset.Text = sound.Permutation.Choices[0].SoundChunks[0].Offset.ToString();
                lblSize.Text = sound.Permutation.Choices[0].SoundChunks[0].Size.ToString();
            }
            catch { }
        }

        private void Choices_SelectedIndexChanged(object sender, EventArgs e)
        {
            Chunks.Items.Clear();
            for (int i = 0; i < sound.Permutation.Choices[Choices.SelectedIndex].SoundChunks.Count; i++)
            {
                Chunks.Items.Add("Chunk #" + i.ToString());
            }
            Chunks.SelectedIndex = 0;
        }

        private void Chunks_SelectedIndexChanged(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.URL = "";
            lblLocation.Text = sound.Permutation.Choices[Choices.SelectedIndex].SoundChunks[Chunks.SelectedIndex].RawLocation.ToString();
            lblOffset.Text = sound.Permutation.Choices[Choices.SelectedIndex].SoundChunks[Chunks.SelectedIndex].Offset.ToString();
            lblSize.Text = sound.Permutation.Choices[Choices.SelectedIndex].SoundChunks[Chunks.SelectedIndex].Size.ToString();
            axWindowsMediaPlayer1.URL = sound.LoadPreview(map, sound.Permutation.Choices[Choices.SelectedIndex].SoundChunks[Chunks.SelectedIndex]);
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                sound.Extract(sfd.FileName, map, sound.Permutation.Choices[Choices.SelectedIndex].SoundChunks.ToArray());
                MessageBox.Show("Done!");
            }
        }

        private void btnExtractAll_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Select a Root Folder for extraction.");
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string path = Path.Combine(fbd.SelectedPath, treeView1.SelectedNode.Text);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                for (var i = 0; i < sound.Permutation.Choices.Count; i++)
                {
                    string spath = Path.Combine(path,
                        map.SIDs[Coconuts.Names[sound.Permutation.Choices[i].NameIndex].Index]);
                    sound.Extract(spath, map, sound.Permutation.Choices[i].SoundChunks.ToArray());
                }
            }
           
        }


        private void extractEverySoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Select a Root Folder for extraction.");
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                foreach (Tag snd in map.Tags.Where(x => x.Class == "snd!"))
                {
                    map.SelectedTag = map.TagIndex.FindTag(map, "snd!", snd.Path);
                    sound = new Sound();
                    sound.Parse(map);

                    string path = Path.Combine(fbd.SelectedPath, snd.Path);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    for (var i = 0; i < sound.Permutation.Choices.Count; i++)
                    {
                        try
                        {
                            string spath = Path.Combine(path,
                                map.SIDs[Coconuts.Names[sound.Permutation.Choices[i].NameIndex].Index]);
                            sound.Extract(spath, map, sound.Permutation.Choices[i].SoundChunks.ToArray());
                        }
                        catch (Exception)
                        {

                        }
                    }
                }



                //for (var i = 0; i < sound.Permutation.Choices.Count; i++)
                //{
                //    string spath = Path.Combine(path,
                //        map.SIDs[Coconuts.Names[sound.Permutation.Choices[i].NameIndex].Index]);
                //    sound.Extract(spath, map, sound.Permutation.Choices[i].SoundChunks.ToArray());
                //}
            }
        }

        private void btnInternalize_Click(object sender, EventArgs e)
        {
            //So we dont mess things up
            this.Enabled = false;
            int Index = treeView1.SelectedNode.Index;
            treeView1.Nodes.Clear();

            //Internalize
            sound.Internalize(map, sound.Permutation);

            //Reload
            map.Reload(treeView1);

            //Finsih
            this.Enabled = true;
            treeView1.SelectedNode = treeView1.Nodes[Index];
            MessageBox.Show("Done!");
        }

        private void btnInject_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "WMA or Wav Files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                //So we dont mess things up
                this.Enabled = false;
                int Index = treeView1.SelectedNode.Index;
                treeView1.Nodes.Clear();

                //Internalize
                sound.Inject(map, ofd.FileName);

                //Reload
                map.Reload(treeView1);

                //Finsih
                this.Enabled = true;
                treeView1.SelectedNode = treeView1.Nodes[Index];
                MessageBox.Show("Done!");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.URL = "";
            axWindowsMediaPlayer1.URL = sound.PlayAll(map, sound.Permutation.Choices.ToArray());
        }

    }
}
