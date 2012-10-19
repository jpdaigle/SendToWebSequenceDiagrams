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
        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                string msc = "A->B: foobar";
                LogMessage("going to grab image...");
                WsdRequest req = new WsdRequest() { MSC = msc };
                SetWait(true);
                backgroundWorker1.RunWorkerAsync(req);
            }
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
    }
}
