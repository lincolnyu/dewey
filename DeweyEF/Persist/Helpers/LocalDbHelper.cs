using System;
using System.Data.SqlClient;
using System.IO;

namespace DeweyEF.Persist.Helpers
{
    /// <summary>
    ///  Provides Local DB related utility functions
    /// </summary>
    /// <remarks>
    ///  https://social.technet.microsoft.com/Forums/sqlserver/en-US/268c3411-102a-4272-b305-b14e29604313/localdb-create-connect-to-database-programmatically-?forum=sqlsetupandupgrade
    /// </remarks>
    public static class LocalDbHelper
    {
        #region Fields

        public static readonly string[] FileExtensions =
        {
            ".mdf"
        };

        public const string FileDescription = "Microsoft LocalDB database file";

        // NOTE this general identifier seems working better
        private const string LocalDbVer = "mssqllocaldb";   // "v11.0"?

        #endregion

        #region Methods

        /// <summary>
        ///  returns the connection string and database file path as per the request, it doesn't change or create the database
        /// </summary>
        /// <param name="dbName">The name of the database which will be used as the file name</param>
        /// <param name="dbFileFolder">The folder to create the file in</param>
        /// <param name="connectionString">The connection string</param>
        /// <param name="dbFilePath">The path to the db file</param>
        public static void GetLocalDbConnectionStringAndFileName(string dbName, string dbFileFolder, 
            out string connectionString, out string dbFilePath)
        {
            var mdfFilename = dbName + ".mdf";
            dbFilePath = Path.Combine(dbFileFolder, mdfFilename);
            connectionString = string.Format(@"Data Source=(LocalDB)\{0};AttachDBFileName={1};Initial Catalog={2};Integrated Security=True;", LocalDbVer, dbFilePath, dbName);
        }

        /// <summary>
        ///  Gets or creates a local db file and returns its connection string
        /// </summary>
        /// <param name="dbName">The name of the database which will be used as the file name</param>
        /// <param name="dbFileFolder">The folder to create the file in</param>
        /// <param name="dbFilePath">The complete path to the file</param>
        /// <param name="deleteIfExists">Delete the file(s) first if it exists</param>
        /// <returns>The connection string</returns>
        public static void PrepareLocalDb(string dbName, string dbFileFolder, string dbFilePath, bool deleteIfExists = false)
        {
            var logFileName = Path.Combine(dbFileFolder, String.Format("{0}_log.ldf", dbName));
            // Create Data Directory If It Doesn't Already Exist.
            if (!Directory.Exists(dbFileFolder))
            {
                Directory.CreateDirectory(dbFileFolder);
            }

            // If the file exists, and we want to delete old data, remove it here and create a new database.
            if (File.Exists(dbFilePath) && deleteIfExists)
            {
                if (File.Exists(logFileName))
                {
                    File.Delete(logFileName);
                }
                File.Delete(dbFilePath);
                CreateDatabase(dbName, dbFilePath);
            }
            // If the database does not already exist, create it.
            else if (!File.Exists(dbFilePath))
            {
                CreateDatabase(dbName, dbFilePath);
            }
        }

        /// <summary>
        ///  Gets or creates a local db file as per the specified parameters and returns a connection to it
        /// </summary>
        /// <param name="dbName">The name of the database which will be used as the file name</param>
        /// <param name="dbFileFolder">The folder to create the file in</param>
        /// <param name="deleteIfExists">Delete the file(s) first if it exists</param>
        /// <returns>A connection to the specified database</returns>
        public static SqlConnection ConnectToLocalDb(string dbName, string dbFileFolder, bool deleteIfExists = false)
        {
            string connectionString;
            string dbFilePath;
            GetLocalDbConnectionStringAndFileName(dbName, dbFileFolder, out connectionString, out dbFilePath);
            PrepareLocalDb(dbName, dbFileFolder, dbFilePath, deleteIfExists);

            // Open newly created, or old database.
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }


        /// <summary>
        ///  Creates a Local DB file based database
        /// </summary>
        /// <param name="dbName">The database name</param>
        /// <param name="dbFilePath">The path to the database file to create</param>
        /// <returns>True if the file successfully is created</returns>
        public static bool CreateDatabase(string dbName, string dbFilePath)
        {
            var connectionString = string.Format(@"Data Source=(LocalDB)\{0};Initial Catalog=master;Integrated Security=True", LocalDbVer);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();

                DetachDatabase(dbName); // We don't care if it can't be detached

                cmd.CommandText = string.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", dbName, dbFilePath);
                cmd.ExecuteNonQuery();
            }

            return File.Exists(dbFilePath);
        }

        public static bool DetachDatabase(string dbName)
        {
            try
            {
                var connectionString = string.Format(@"Data Source=(LocalDB)\{0};Initial Catalog=master;Integrated Security=True", LocalDbVer);
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = string.Format("exec sp_detach_db '{0}'", dbName);
                    cmd.ExecuteNonQuery();

                    return true;
                }
            }
            catch(Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
