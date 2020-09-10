using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsInput;

namespace DefinitelyNotAfk
{
    public partial class MainForm : Form
    {
        #region "Properties"

        private DateTime AskedStartupDate
        {
            get
            {
                return DateTime.Parse(ConfigurationManager.AppSettings["askedStartupDate"]);
            }
            set
            {
                Configuration.AppSettings.Settings["askedStartupDate"].Value = value.ToString();
                SaveConfiguration();
            }
        }

        private bool AddedToStartup
        {
            get
            {
                return bool.Parse(ConfigurationManager.AppSettings["addedToStartup"]);
            }
            set
            {
                Configuration.AppSettings.Settings["addedToStartup"].Value = value.ToString();
                SaveConfiguration();
            }
        }

        private Configuration Configuration
        {
            get
            {
                if (configuration is null) configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                return configuration;
            }
        }
        private Configuration configuration = null;

        #endregion

        public MainForm() => InitializeComponent();

        #region "Events"

        private void MainForm_Load(object sender, EventArgs e)
        {
            AskForStartupPath();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Hide();
        }

        private void AfkTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                InputSimulator input = new InputSimulator();
                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.NUMLOCK).Sleep(10);
                input.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.NUMLOCK);
            }
            catch (Exception)
            {
                // Trust me. I am an engineer.
            }
        }
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) return;
            AfkNotifyIcon.Visible = true;
            ShowInTaskbar = false;

            Hide();
        }

        private void AfkNotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            AfkNotifyIcon.Visible = false;
            this.ShowInTaskbar = true;
            Show();

            WindowState = FormWindowState.Normal;

            RepositionWindow();
            this.BringToFront();
        }

        #endregion

        #region "Functions"

        /// <summary>
        /// https://stackoverflow.com/questions/1264406/how-do-i-get-the-taskbars-position-and-size
        /// 
        /// This code brings back all of the task bars as a list of rectanges:
        ///
        /// 0 rectangles means the taskbar is hidden;
        /// 1 rectangle is the position of the taskbar;
        /// 2+ is very rare, it means that we have multiple monitors, and we are not using Extend these displays to create a single virtual desktop.
        /// </summary>
        private List<Rectangle> FindDockedTaskBars()
        {
            List<Rectangle> dockedRects = new List<Rectangle>();
            foreach (Screen tmpScrn in Screen.AllScreens)
            {
                if (!tmpScrn.Bounds.Equals(tmpScrn.WorkingArea))
                {
                    Rectangle rect = new Rectangle();

                    int leftDockedWidth = Math.Abs(Math.Abs(tmpScrn.Bounds.Left) - Math.Abs(tmpScrn.WorkingArea.Left));
                    int topDockedHeight = Math.Abs(Math.Abs(tmpScrn.Bounds.Top) - Math.Abs(tmpScrn.WorkingArea.Top));
                    int rightDockedWidth = (tmpScrn.Bounds.Width - leftDockedWidth) - tmpScrn.WorkingArea.Width;
                    int bottomDockedHeight = (tmpScrn.Bounds.Height - topDockedHeight) - tmpScrn.WorkingArea.Height;

                    if (leftDockedWidth > 0)
                    {
                        rect.X = tmpScrn.Bounds.Left;
                        rect.Y = tmpScrn.Bounds.Top;
                        rect.Width = leftDockedWidth;
                        rect.Height = tmpScrn.Bounds.Height;
                    }
                    else if (rightDockedWidth > 0)
                    {
                        rect.X = tmpScrn.WorkingArea.Right;
                        rect.Y = tmpScrn.Bounds.Top;
                        rect.Width = rightDockedWidth;
                        rect.Height = tmpScrn.Bounds.Height;
                    }
                    else if (topDockedHeight > 0)
                    {
                        rect.X = tmpScrn.WorkingArea.Left;
                        rect.Y = tmpScrn.Bounds.Top;
                        rect.Width = tmpScrn.WorkingArea.Width;
                        rect.Height = topDockedHeight;
                    }
                    else if (bottomDockedHeight > 0)
                    {
                        rect.X = tmpScrn.WorkingArea.Left;
                        rect.Y = tmpScrn.WorkingArea.Bottom;
                        rect.Width = tmpScrn.WorkingArea.Width;
                        rect.Height = bottomDockedHeight;
                    }
                    else
                    {
                        // Nothing found!
                    }

                    dockedRects.Add(rect);
                }
            }

            if (dockedRects.Count == 0)
            {
                // Taskbar is set to "Auto-Hide".
            }

            return dockedRects;
        }

        private void RepositionWindow()
        {
            List<Rectangle> taskbars = FindDockedTaskBars();
            Rectangle bounds = Screen.FromControl(this).Bounds;

            switch (taskbars.Count())
            {
                case 0:
                    this.Location = new Point(bounds.Width - Width, 0);
                    break;
                case 1:
                    this.Location = new Point(bounds.Width - Width, bounds.Height - taskbars.First().Height - Height);
                    break;
                default:
                    this.Location = new Point(bounds.Width - Width, bounds.Height - taskbars.First().Height - Height);
                    break;
            }
        }

        private void CreateStartupShortcut()
        {
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            dynamic link = shell.CreateShortcut(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Definitely not AFK.lnk"));

            link.TargetPath = Application.ExecutablePath;
            link.WindowStyle = 1;
            link.Save();
        }

        private void AskForStartupPath()
        {
            if (AskedStartupDate.AddMonths(1) < DateTime.Now && !AddedToStartup &&
                MessageBox.Show("Start at startup?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CreateStartupShortcut();
                AddedToStartup = true;
            }
            AskedStartupDate = DateTime.Now;
        }

        private void SaveConfiguration()
        {
            Configuration.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        #endregion
    }
}
