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
        const int BALOON_TIP_DELAY = 5000; // ms

        string backupDriveLabel;
        string backupDir;

        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            iconMain = new Icon("Main.ico");
            iconDownloding = new Icon("Downloading.ico");

            appConfig = ConfigurationManager.AppSettings;

            backupDriveLabel = appConfig.Get("cctvVolumeLabel");
            backupDir = appConfig.Get("backupDir");

            notifyIcon.Icon = iconMain;
            notifyIcon.Visible = true;

            notifyIcon.DoubleClick += delegate (object sender, EventArgs e)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            textBox.Clear();
            textBox.AppendText("Starting app...");

            textBox.AppendText("Available removable drive labels:" + Environment.NewLine);
            bool backupDriveFound = false;
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    textBox.AppendText(" * " + drive.Name);
                    textBox.AppendText(" " + drive.VolumeLabel);
                    if (drive.VolumeLabel.ToString() == backupDriveLabel)
                    {
                        textBox.AppendText(" << This is the backup drive");
                        backupDriveFound = true;
                    }
                    textBox.AppendText(Environment.NewLine);
                }
            }

            if (backupDriveFound)
            {
                CopyFiles();
            }

            ManagementEventWatcher watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
            watcher.EventArrived += (s, e) =>
            {
                CopyFiles();
            };

            watcher.Query = query;
            watcher.Start();
        }

        private void CopyFiles()
        {
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if ((driveInfo.DriveType == DriveType.Removable) && (driveInfo.VolumeLabel == backupDriveLabel))
                {
                    textBox.Dispatcher.Invoke(new Action(() => textBox.AppendText("Connected backup drive volume: " + backupDriveLabel + Environment.NewLine)));

                    notifyIcon.Icon = iconDownloding;
                    notifyIcon.ShowBalloonTip(5000, "CCTV", "CCTV Flash Drive connected as " + driveInfo.RootDirectory + Environment.NewLine + "Copying files...", ToolTipIcon.Info);

                    textBox.Dispatcher.Invoke(new Action(() => textBox.AppendText("Copying files..." + Environment.NewLine)));

                    string sourceDir = driveInfo.RootDirectory.ToString();

                    List<string> filesToCopy = new List<string>();

                    AddFiles(sourceDir, filesToCopy);

                    foreach (string textFile in filesToCopy)
                    {
                        string fileName = textFile.Substring(sourceDir.Length);

                        try
                        {
                            string sourcePath = System.IO.Path.Combine(sourceDir, fileName);
                            string destinationPath = System.IO.Path.Combine(backupDir, fileName);

                            if (!File.Exists(destinationPath))
                            {
                                System.IO.FileInfo file = new System.IO.FileInfo(destinationPath);
                                file.Directory.Create();

                                textBox.Dispatcher.Invoke(new Action(() => textBox.AppendText(" * Copy file " + sourcePath + " to " + destinationPath + Environment.NewLine)));

                                File.Copy(sourcePath, destinationPath, false);
                            }
                            else
                            {
                                textBox.Dispatcher.Invoke(new Action(() => textBox.AppendText(" * File " + destinationPath + " already exists" + Environment.NewLine)));
                            }
                        }
                        catch (IOException ioe)
                        {
                            textBox.Dispatcher.Invoke(new Action(() => textBox.AppendText("IO Exception " + ioe + Environment.NewLine)));
                        }
                    }

                    notifyIcon.ShowBalloonTip(BALOON_TIP_DELAY, "CCTV", "Files copied success", ToolTipIcon.Info);
                    notifyIcon.Icon = iconMain;

                    textBox.Dispatcher.Invoke(new Action(() => textBox.AppendText("Copying finished" + Environment.NewLine)));

                    break;
                }
            }
        }

        private static void AddFiles(string path, IList<string> files)
        {
            try
            {
                Directory.GetFiles(path)
                    .ToList()
                    .ForEach(s => files.Add(s));

                Directory.GetDirectories(path)
                    .ToList()
                    .ForEach(s => AddFiles(s, files));
            }
            catch (UnauthorizedAccessException ex)
            {
                // ok, so we are not allowed to dig into that directory. Move on.
            }
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
