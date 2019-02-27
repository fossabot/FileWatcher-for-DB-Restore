/*
 * Author       -   Avinash Singh
 * Date         -   27 February 2019
 * Manager      -   Muthu Ramakrishnan
 * Verion       -   1.0.0
 * Description  -   A Demo FileWatcher used for Watching a specific folder for .bak files - 
 *                  Pick it up and Restore the Database.
 *                  After Restoration is complete it also triggers a Stored procedure in the Database.
 *                  
 *                  To Install and Uninstall use the folowing commands using Command-Line for Developer in VS.
 *                  
 *                  To Insatall: installutil FileWatcher.exe
 *                  
 *                  To Uninstall: installutil /u FileWatcher.exe
*/

/*
* using System.Reflection;
* using MySql.Data.MySqlClient;
* using MySql.Data;
* using System.Diagnostics;
* using System.Linq;
* using System.ComponentModel;
* using System.Text;
* using System.Threading.Tasks;
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceProcess;
using System.IO;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;

namespace FileWatcher
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

	// What Happens when the Service starts.
        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Launch(); FOR DEBUGGING
            // Creating a list of FileSystemWatchers and dynamically populating - one for each of my drives.
            List<FileSystemWatcher> fileSystemWatchers = new List<FileSystemWatcher>();
            fileSystemWatchers.Add(new FileSystemWatcher() { Path = @"C:\Users\avinashs\Documents\GitHub\Electron-Desktop-App-Sandbox\" });
            //fileSystemWatchers.Add(new FileSystemWatcher() { Path = @"D:\" });
            //fileSystemWatchers.Add(new FileSystemWatcher() { Path = @"E:\" });

            foreach (FileSystemWatcher fsw in fileSystemWatchers)
            {
                // I will be watching changes if any files/directories are read or written to.
                fsw.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                fsw.Changed += new FileSystemEventHandler(OnChanged);
                fsw.Created += new FileSystemEventHandler(OnChangedRestore);
                fsw.Deleted += new FileSystemEventHandler(OnChanged);
                fsw.Renamed += new RenamedEventHandler(OnRenamed);

                // Watch all files in folder and subfolders.
                fsw.Filter = "*.bak";
                fsw.IncludeSubdirectories = false;

                // Start watching for the above events.
                fsw.EnableRaisingEvents = true;
            }
        }

	// What happens when the service finds a Change in the specified Folder.
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            //System.Diagnostics.Debugger.Launch(); FOR DEBUGGING
            string[] info = new string[] { "Timestamp: " + DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss"),
                                           "File/Directory Name: " + e.Name,
                                           "File/Directory URL: " + e.FullPath,
                                           "File/Directory Change: " + e.ChangeType.ToString()};
            // Adding in a condition to resolve issue of FileTracker recursively tracking itself on WriteToFile().
            if (!e.FullPath.Contains("FileWatcher.txt"))
                WriteToFile(info);

            // Restore the Database.
            //RestoreDatabase("SANDBOX", e.FullPath, "LDM-AVINASHS", "filewatcher", "password1!");


        }

	// Call the Restore Method to restore the database.
        private void OnChangedRestore(object sender, FileSystemEventArgs e)
        {
            //System.Diagnostics.Debugger.Launch(); FOR DEBUGGING
            string[] info = new string[] { "Timestamp: " + DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss"),
                                           "File/Directory Name: " + e.Name,
                                           "File/Directory URL: " + e.FullPath,
                                           "File/Directory Change: " + e.ChangeType.ToString()};
            // Adding in a condition to resolve issue of FileTracker recursively tracking itself on WriteToFile().
            if (!e.FullPath.Contains("FileWatcher.txt"))
                WriteToFile(info);

            // Restore the Database.
            RestoreDatabase("SANDBOX", e.FullPath, "LDM-AVINASHS", "filewatcher", "password1!");

        }

	// Restore Database and trigger the stored procedure.
	public void RestoreDatabase(String databaseName, String filePath, String serverName, String userName, String password)
        {
            //System.Diagnostics.Debugger.Launch(); FOR DEBUGGING
            Restore sqlRestore = new Restore();
            BackupDeviceItem deviceItem = new BackupDeviceItem(filePath, DeviceType.File);
            sqlRestore.Devices.Add(deviceItem);
            sqlRestore.Database = databaseName;

            ServerConnection connection = new ServerConnection(serverName, userName, password);
            Server sqlServer = new Server(connection);

            Database db = sqlServer.Databases[databaseName];
            sqlRestore.Action = RestoreActionType.Database;
            db = sqlServer.Databases[databaseName];

            sqlRestore.ReplaceDatabase = true;
            
            // Restoration in process.
            sqlRestore.SqlRestore(sqlServer);
            db = sqlServer.Databases[databaseName];
            db.SetOnline();
            sqlServer.Refresh();


            // Trigger Stored Procedure after restore.
            using (SqlConnection conn = new SqlConnection("Server=(local);DataBase=SANDBOX;Integrated Security=SSPI"))
            {
                conn.Open();

                // 1.  create a command object identifying the stored procedure
                SqlCommand cmd = new SqlCommand("sp_test_temp", conn);

                // 2. set the command object so it knows to execute a stored procedure
                cmd.CommandType = CommandType.StoredProcedure;

                // execute the command
                using (SqlDataReader rdr = cmd.ExecuteReader()){}
            }

        }

	// What happens when the specified files are Renamed.
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            //System.Diagnostics.Debugger.Launch();
            string[] info = new string[] { "Timestamp: " + DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss"),
                                           "File/Directory Original Name: " + e.OldName,
                                           "File/Directory New Name: " + e.Name,
                                           "File/Directory Original URL: " + e.OldFullPath,
                                           "File/Directory New URL: " + e.FullPath,
                                           "File/Directory Change: " + e.ChangeType.ToString()};
            // Adding in a condition to resolve issue of FileTracker recursively tracking itself on WriteToFile().
            if (!e.FullPath.Contains("FileWatcher.txt"))
                WriteToFile(info);
        }

	// Logs entry in a file. The file must exist before running the service (somehow).
        private void WriteToFile(string[] info)
        {
            //System.Diagnostics.Debugger.Launch();
            string path = @"C:\Users\avinashs\Desktop\FileWatcher.txt";
            try
            {
                if (!File.Exists(path))
                {
                    // Creates a hidden, read-only text file.
                    File.SetAttributes(path, FileAttributes.ReadOnly);
                    File.CreateText(path);
                }

                File.AppendAllLines(path, info);
             
            }
            catch (Exception ex)
            {
                Console.Write(ex.InnerException);
            }
        }
    }
}
