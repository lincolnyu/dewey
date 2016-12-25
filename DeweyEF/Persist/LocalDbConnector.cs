using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using DeweyEF.Persist.Helpers;

namespace DeweyEF.Persist
{
    public class LocalDbConnector : IDbConnector
    {
        #region Nested classes

        public class Factory : IDbConnectorFactory
        {
            #region Properties

            #region IDbConnectorFactory members

            public string FileDescription
            {
                get { return "Microsoft LocalDB Database File"; }
            }

            public IEnumerable<string> FileExtensions
            {
                get
                {
                    return new[]
                    {
                        ".mdf"
                    };
                }
            }

            #endregion

            #endregion

            #region Methods

            #region IDbConnectorFactory members

            public IDbConnector GetConnector()
            {
                return new LocalDbConnector();
            }

            #endregion

            #endregion
        }

        #endregion

        #region Properties

        #region IDbConnector members

        public string ConnectionString { get; private set; }

        public string DbFilePath { get; private set; }

        #endregion

        public string Folder { get; private set; }

        public string DbName { get; private set; }

        #endregion

        #region Methods

        #region IDbConnector members

        public bool Equals(IDbConnector other)
        {
            if (other == null)
            {
                // TODO figure out if the current is a default LocalDB connection?
            }
            var thatLocalDb = other as LocalDbConnector;
            if (thatLocalDb == null)
            {
                return false;
            }
            return DbFilePath.Equals(thatLocalDb.DbFilePath, StringComparison.OrdinalIgnoreCase);
        }

        public DbConnection GetConnection()
        {
            // TODO may need to specify it later
            return null;    // leave it to the default for now
        }

        public void PreConnectToFile(string filePath)
        {
            Folder = Path.GetDirectoryName(filePath);
            DbName = Path.GetFileNameWithoutExtension(filePath);
            
            if (Folder == null || DbName == null)
            {
                throw new Exception("Invalid file path");
            }

            DbName = DbName.TrimEnd('.'); // don't know why it may include the trailing dot

            string connectionString;
            string dbFilePath;

            LocalDbHelper.GetLocalDbConnectionStringAndFileName(DbName, Folder, out connectionString, out dbFilePath);
            
            ConnectionString = connectionString;
            DbFilePath = dbFilePath;
        }

        public void PrepareNewDatabase()
        {
            LocalDbHelper.PrepareLocalDb(DbName, Folder, DbFilePath, true);
        }

        #endregion

        #endregion
    }
}
