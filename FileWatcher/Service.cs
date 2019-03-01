/*
 * Author       -   Avinash Singh
 * Date         -   27 February 2019
 * Version       -   1.0.0
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
	    //Location can be changed to Any location (Local, Network, Remote etc..)
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

                // Watch .bak files in folder and subfolders.
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
        }

	// Call the Restore Method to restore the database.
        private void OnChangedRestore(object sender, FileSystemEventArgs e)
        {
	    bool fileLocked = false;
            //System.Diagnostics.Debugger.Launch(); FOR DEBUGGING 
            string[] info = new string[] { "Timestamp: " + DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss"),
                                           "File/Directory Name: " + e.Name,
                                           "File/Directory URL: " + e.FullPath,
                                           "File/Directory Change: " + e.ChangeType.ToString()};
            // Adding in a condition to resolve issue of FileTracker recursively tracking itself on WriteToFile().
            if (!e.FullPath.Contains("FileWatcher.txt"))
                WriteToFile(info);

	    // Verify if the File is locked or in Use.
	    FileInfo backupFile = new FileInfo(e.FullPath);
	    fileLocked = IsFileLocked(backupFile);

            // Restore the Database.
	    if(!fileLocked)
	    {
		RestoreDatabase("SANDBOX", e.FullPath, "LDM-AVINASHS", "filewatcher", "password1!");
	    }
	    else
	    {
		string[] log = new string[] { "Timestamp: " + DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss"),
					   "File/Directory Name: " + e.Name,
					   "File/Directory URL: " + e.FullPath,
					   "File is either Locked or in Use by other Process"
					    }; 
		if (!e.FullPath.Contains("FileWatcher.txt"))
		    WriteToFile(log);
	    }
		

        }

	// Restore Database and trigger the stored procedure.
	public void RestoreDatabase(String databaseName, String filePath, String serverName, String userName, String password)
        {
	    //System.Diagnostics.Debugger.Launch(); FOR DEBUGGING

	    String connectionString = "Server=" + serverName + "; DataBase=" + databaseName + ";Integrated Security=SSPI"
	    
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
            
            // Restoration in process. The Database is Restored at a Default location. Under the DATA Folder.
            sqlRestore.SqlRestore(sqlServer);
            db = sqlServer.Databases[databaseName];
            db.SetOnline();
            sqlServer.Refresh();

	    // Trigger Stored Procedure after restore.
	    triggerSP(connectionString);

        }

	// Trigger Stored Procedure after restore. MAKE ANOTHER METHOD.
	public void triggerSP(String connectionStr)
	{
	    
	    SqlConnection conn = new SqlConnection(connectionStr);

	    // conn.Open();
	    // 1.  create a command object identifying the stored procedure
	    SqlCommand cmd = new SqlCommand("sp_test_temp", conn);

	    // 2. set the command object so it knows to execute a stored procedure
	    cmd.CommandType = CommandType.StoredProcedure;

	    // Add a check here as well.
	    // execute the command
	    SqlDataReader rdr = cmd.ExecuteReader();

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

	// Check if File is Loked or is in Use.
	protected virtual bool IsFileLocked(FileInfo file)
	{
	    FileStream stream = null;

	    try
	    {
		stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
	    }
	    catch (IOException)
	    {
		//the file is unavailable because it is:
		//still being written to
		//or being processed by another thread
		//or does not exist (has already been processed)
		return true;
	    }
	    finally
	    {
		if (stream != null)
		    stream.Close();
	    }

	    //file is not locked
	    return false;
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

// Start up Notification.
// Create a config file, CSV/XML, List of emails.
// After we successfully restore the DB, send and email to user the restore has been started. 
// Read the list of user from Config File.
// Completion Email goes with the Start up Email.

// Any Error/Warning, Separete list of users.

// Data compare Notification
