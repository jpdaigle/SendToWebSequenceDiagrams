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

        object _nextTask = null;
        object _nextTaskLock = new Object();

        public Form1()
        {
            InitializeComponent();
        }

        private void Refresh(string txt)
        {
            this.BeginInvoke(new Action(() => 
            {
                LogMessage(string.Format("Got MSC text ({0} chars, starting refresh.", txt.Length));
                SetWait(true);
                WsdRequest req = new WsdRequest() { MSC = txt };
                submitBgTask(req);
            }));
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
                centerOn(pbWait, panel1);
                pbWait.Visible = true;
            }
            else
            {
                pbWait.Visible = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = true;
            pictureBox1.Location = new Point(0, 0);

            string[] args = Environment.GetCommandLineArgs();
            try
            {
                string path = null;
                if (args.Length > 1)
                {
                    path = args[1];
                    _fsReloader = new FileReloader(args[1], new Action<string>((x) => Refresh(x)));
                }
                this.Text = "WebSequenceDiagrams: " + path;
            }
            catch (Exception err)
            {
                PopError("Initialization error", err);
            }
        }

        public void LogMessage(string s)
        {
            s = (s.EndsWith("\r\n")) ? s : s + "\r\n";
            textBox1.Text += s;
        }

        private void submitBgTask(WsdRequest req)
        {
            if (backgroundWorker1.IsBusy)
            {
                lock (_nextTaskLock)
                {
                    _nextTask = req;
                }
            }
            else
            {
                backgroundWorker1.RunWorkerAsync(req);
            }
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
                if (ms.Length > 0)
                {
                    try
                    {
                        Image imgLoaded = Image.FromStream(ms);
                        UpdateImage(imgLoaded);
                    }
                    catch (Exception err)
                    {
                        PopError("Error loading image", err);
                    }
                }
                SetWait(false);
            }

            WsdRequest next = null;
            lock (_nextTaskLock)
            {
                if (_nextTask != null)
                {
                    next = _nextTask as WsdRequest;
                    _nextTask = null;
                }
            }
            if (next != null)
                submitBgTask(next);

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

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            // Save the currently displayed image to a file.
        }
    }
}
