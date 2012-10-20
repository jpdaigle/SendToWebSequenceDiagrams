using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace SendToWebSequenceDiagrams
{
    public partial class Form1 : Form
    {
        FileReloader _fsReloader;
        public Form1()
        {
            InitializeComponent();
        }

        private void Refresh(string txt)
        {
            LogMessage("Got MSC text, starting refresh.");
            this.BeginInvoke(new Action(() => SetWait(true)));
            WsdRequest req = new WsdRequest() { MSC = txt };
            backgroundWorker1.RunWorkerAsync(req);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
        }

        private void UpdateImage(Image img)
        {
            pictureBox1.Image = img;
        }

        private void SetWait(bool state)
        {
            Action<Control, Control> centerOn = (a, b) =>
            {
                a.Left = b.Left + b.Width / 2 - a.Width / 2;
                a.Top = b.Top + b.Height / 2 - a.Height / 2;
            };

            if (state)
            {
                pbWait.SizeMode = PictureBoxSizeMode.AutoSize;
                centerOn(pbWait, pictureBox1);
                pbWait.Visible = true;
            }
            else
            {
                pbWait.Visible = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LogMessage("Loaded...");
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                _fsReloader = new FileReloader(args[1], new Action<string>((x) => Refresh(x)));
            }
            splitContainer1.Panel2Collapsed = true;
        }

        public void LogMessage(string s)
        {
            s = (s.EndsWith("\r\n")) ? s : s + "\r\n";
            textBox1.Text += s;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Load the request object we want to run.
            WsdRequest req = e.Argument as WsdRequest;
            req.PerformRequest();
            e.Result = req;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LogMessage(e.ToString());
            }
            else
            {
                WsdRequest req = e.Result as WsdRequest;
                Stream ms = req.Result;
                LogMessage("Got... " + ms.Length + " bytes");
                Image imgLoaded = Image.FromStream(ms);
                UpdateImage(imgLoaded);
                SetWait(false);
            }

        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            ToolStripButton btn = sender as ToolStripButton;
            splitContainer1.Panel2Collapsed = (!btn.Checked);
        }

        public void PopError(string msg, Exception e = null)
        {
            if (e != null)
            {
                msg += "\r\n\r\n" + e.ToString();
            }
            this.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }));
        }
    }
}
