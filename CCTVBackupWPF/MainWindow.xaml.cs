using System;
using System.Collections.Generic;
using System.Configuration;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;

namespace CCTVBackup
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NameValueCollection appConfig;
        Icon iconMain;
        Icon iconDownloding;

        public MainWindow()
        {
            InitializeComponent();

            iconMain = new Icon("Main.ico");
            iconDownloding = new Icon("Downloading.ico");

            appConfig = ConfigurationManager.AppSettings;

            backupPath.Content = appConfig.Get("Key2");

            System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

            notifyIcon.Icon = iconMain;
            notifyIcon.Visible = true;

            notifyIcon.DoubleClick += delegate (object sender, EventArgs e)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            textBox.Clear();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    textBox.AppendText(drive.Name);
                    textBox.AppendText(" " + drive.VolumeLabel);
                    textBox.AppendText("\r\n");
                }
            }

            ManagementEventWatcher watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
            watcher.EventArrived += (s, e) =>
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Removable && drive.VolumeLabel == appConfig.Get("cctvVolumeLabel"))
                    {
                        textBox.Dispatcher.Invoke(new Action(() => textBox.AppendText("Connected\r\n")));
                        notifyIcon.Icon = iconDownloding;
                        notifyIcon.ShowBalloonTip(5000, "CCTV", "CCTV Flash Drive connected as " + drive.RootDirectory + "\r\nCopying files...", ToolTipIcon.Info);

                        string sourceDir = @drive.RootDirectory.ToString();
                        string backupDir = appConfig.Get("backupDir");

                        string[] filesToCopy = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                        string[] directoriesToCopy = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);

                        System.IO.Directory.CreateDirectory(backupDir);

                        foreach (string dirPath in directoriesToCopy)
                        {
                            string dirPathStripped = dirPath.Substring(sourceDir.Length);
                            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(backupDir, dirPathStripped));
                        }

                        foreach (string textFile in filesToCopy)
                        {
                            string fileName = textFile.Substring(sourceDir.Length);

                            try
                            {
                                File.Copy(System.IO.Path.Combine(sourceDir, fileName),
                                    System.IO.Path.Combine(backupDir, fileName));
                            }
                            catch (IOException)
                            {
                            }
                            
                        }

                        notifyIcon.ShowBalloonTip(5000, "CCTV", "Files copied success", ToolTipIcon.Info);
                        notifyIcon.Icon = iconMain;
                    }
                }
            };

            watcher.Query = query;
            watcher.Start();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }
    }
}
