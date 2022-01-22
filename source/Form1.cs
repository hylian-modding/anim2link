using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace anim2link
{
    public partial class Form1 : Form
    {
        public bool FloorPlane { get; set; }
        public static Processing Proc = new Processing();
        public static List<Processing.Animation> Animations = new List<Processing.Animation>();
        public static int Anime_Size = 0;
        public static bool Do_Not_Alert = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog OpenFile = new OpenFileDialog())
            {
                if (OpenFile.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = OpenFile.FileName;
                    ListAnimations(textBox1.Text, listView1);
                    btnRefresh.Enabled = true;
                }
            }
        }

        private void ListAnimations(string _src, ListView _lb)
        {
            Animations.Clear();
            string[] _l = File.ReadAllLines(_src);

            // Parse All Animations
            for (int i = 0; i < _l.Length; i++)
            {

                string[] _s = _l[i].Split(' ');
                if (_s.Length < 4)
                {
                    if (_s[0] == "frames")
                    {
                        // Create New Animation Instance
                        Processing.Animation NewAnim = new Processing.Animation(_src, _s[2]);
                        Animations.Add(NewAnim);
                    }
                }
            }

            // Populate ListView
            _lb.Items.Clear();
            for (int i = 0; i < Animations.Count; i++)
            {
                string[] ItemData = new string[2];

                ItemData[0] = Animations[i].Name.Trim('"');
                ItemData[1] = Animations[i].FrameCount.ToString();

                ListViewItem NewItem = new ListViewItem(ItemData);

                _lb.Items.Add(NewItem);
            }

        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox3.Enabled = !groupBox3.Enabled;
        }

        private void processStuff()
        {
            if (listView1.SelectedItems.Count < 1)
            {
                MessageBox.Show("Please select an animation!", "Uh-oh!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] AnimationRaw = Proc.GetRaw(Animations[listView1.SelectedIndices[0]]);
            string fileOutPath = String.Format("{0}.bin", listView1.SelectedItems[0].Text);
            #region Write To ROM
            if (checkBox1.Checked)
            {
                // Initialize Variables
                byte[] _rom = File.ReadAllBytes(textBox2.Text);
                BinaryWriter rombw = new BinaryWriter(new FileStream(textBox2.Text, FileMode.Open));
                int link_animetion = Convert.ToInt32(textBox4.Text, 16);
                byte[] _anime = Proc.GetByteArray(_rom, link_animetion, Anime_Size);

                rombw.Seek(link_animetion, 0);
                rombw.Write(_anime);

                int gameplay_keep = Convert.ToInt32(textBox5.Text, 16);
                int header = gameplay_keep + Convert.ToInt32(textBox3.Text, 16);
                byte[] _header = Proc.GetByteArray(_rom, header, 8);
                short _fc_old = (short)((_header[0] << 8) | (_header[1]));
                int old_size = _fc_old * 0x86;
                int anim_offset = (int)((_header[5] << 16) | (_header[6] << 8) | (_header[7]));
                Console.WriteLine("_fc_old " + _fc_old.ToString());
                Console.WriteLine("_fc_new " + (AnimationRaw.Length / 0x86).ToString());
                if (AnimationRaw.Length > old_size)
                {
                    MessageBox.Show("The new animation is larger than the one being replaced. Old: " + _fc_old.ToString() + ", New: " + (AnimationRaw.Length / 0x86).ToString(), "Write Aborted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    rombw.Close();
                    rombw.Dispose();
                    return;
                }
                else
                {
                    // Update Frame Count
                    rombw.Seek(header, 0);
                    byte[] _fc_new = new byte[2] {
                        (byte)(((AnimationRaw.Length / 0x86) >> 8) & 0xFF),
                        (byte)(((AnimationRaw.Length / 0x86)) & 0xFF),
                    };
                    rombw.Write(_fc_new);

                    rombw.Seek(anim_offset + link_animetion, 0);
                    rombw.Write(AnimationRaw);
                    if (!Do_Not_Alert)
                    {
                        MessageBox.Show("Done!", fileOutPath + " Injected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Console.WriteLine("Done! " + fileOutPath + " Injected");
                    }

                    _anime = Proc.GetByteArray(_rom, link_animetion, Anime_Size);
                    File.WriteAllBytes(Directory.GetParent(textBox2.Text) + "/link_animetion.zdata", _anime);

                    rombw.Close();
                    rombw.Dispose();
                }
            }
            else
            {
                BinaryWriter bw = new BinaryWriter(new FileStream(fileOutPath, FileMode.Create));
                for (int i = 0; i < AnimationRaw.Length; i++)
                {
                    bw.Write(AnimationRaw[i]);
                }
                if (!Do_Not_Alert)
                {
                    MessageBox.Show("Done!", fileOutPath + " Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Console.WriteLine("Done! " +  fileOutPath + " Exported");
                }
                bw.Close();
                bw.Dispose();
            }
            #endregion
        }

        private void button2_Click(object sender, EventArgs e)
        {
            processStuff();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog OpenFile = new OpenFileDialog())
            {
                if (OpenFile.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = OpenFile.FileName;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                FloorPlane = true;
            else
                FloorPlane = false;

            Proc.host.FloorPlane = FloorPlane;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
            listBox1.SelectedIndex = 0;
            Console.WriteLine(Anime_Size);
            btnRefresh.Enabled = false;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            if (listView1.SelectedItems[0].SubItems[0].Text.StartsWith("0x"))
            {
                textBox3.Text = listView1.SelectedItems[0].SubItems[0].Text.Split('_')[0];
            }
        }

        private void setLinkAnime(string text)
        {
            textBox4.Text = text;
        }

        private void setGK(string text)
        {
            textBox5.Text = text;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // a
            string debug_f1 = "0x004E5C00";
            // g
            string debug_f2 = "0x00F5E000";
            string n0_f1 = "0x00557000";
            string n0_f2 = "0x00F03000";
            int n0_f1_size = 0x265C30;

            string mm_f1 = "0x0065D000";
            string mm_f2 = "0x0108B000";
            int mm_f1_size = 0x000925E0;

            if ((listBox1.SelectedItem as string) == "1.0 (Oot)")
            {
                setLinkAnime(n0_f1);
                setGK(n0_f2);
                Anime_Size = n0_f1_size;
            }else if ((listBox1.SelectedItem as string) == "Debug (Oot)")
            {
                setLinkAnime(debug_f1);
                setGK(debug_f2);
                Anime_Size = n0_f1_size;
            }
            else if ((listBox1.SelectedItem as string) == "1.0 (MM)")
            {
                setLinkAnime(mm_f1);
                setGK(mm_f2);
                Anime_Size = mm_f1_size;
            }

        }

        private void btnExportAll_Click(object sender, EventArgs e)
        {
            Do_Not_Alert = true;
            for (int i = 0; i < listView1.SelectedIndices.Count; i++)
            {
                int index = listView1.SelectedIndices[i];
                listView1.Items[index].Selected = false;
            }
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                listView1.Items[i].Selected = true;
                processStuff();
            }
            Do_Not_Alert = false;
            MessageBox.Show("Done!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            ListAnimations(textBox1.Text, listView1);
        }
    }
}
