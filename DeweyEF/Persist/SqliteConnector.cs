//using System;
//using System.Collections.Generic;
//using System.Data.Common;
//using System.IO;
//using Devart.Data.SQLite;

//namespace DeweyEF.Persist
//{
//    public class SqliteConnector : IDbConnector
//    {
//        #region Nested classes

//        public class Factory : IDbConnectorFactory
//        {
//            #region Properties

//            #region IDbConnectorFactory members

//            public string FileDescription { get { return "SQLite database file"; } }

//            public IEnumerable<string> FileExtensions
//            {
//                get
//                {
//                    return new[]
//                    {
//                        ".sqlite",
//                        ".db",
//                        ".db3"
//                    };
//                }
//            }

//            #endregion

//            #endregion

//            #region Properties

//            #region IDbConnectorFactory members

//            public IDbConnector GetConnector()
//            {
//                return new SqliteConnector();
//            }

//            #endregion

//            #endregion
//        }

//        #endregion

//        #region Properties

//        #region IDbConnector members

//        public string ConnectionString { get; private set; }

//        public string DbFilePath { get; private set; }

//        #endregion

//        #endregion

//        #region Methods

//        #region IDbConnector members

//        public bool Equals(IDbConnector other)
//        {
//            var thatSqlite = other as SqliteConnector;
//            if (thatSqlite == null)
//            {
//                return false;
//            }
//            return DbFilePath.Equals(other.DbFilePath, StringComparison.OrdinalIgnoreCase);
//        }

//        public DbConnection GetConnection()
//        {
//            //var conn = new System.Data.SQLite.EF6.SQLiteProviderFactory().CreateConnection();
//            var conn = new SQLiteConnection();
//            return conn;
//        }

//        public void PreConnectToFile(string dbFilePath)
//        {
//            DbFilePath = dbFilePath;

//            var connectionString = String.Format(@"Data Source={0};Version=3", dbFilePath);
//            //var sb = new SQLiteConnectionStringBuilder(connectionString);
//            ConnectionString = connectionString; //sb.ConnectionString;
//        }

//        public void PrepareNewDatabase()
//        {
//            if (File.Exists(DbFilePath))
//            {
//                File.Delete(DbFilePath);
//            }

//            // TODO creating database file needed?
//            File.Create(DbFilePath).Close();
//        }

//        #endregion

//        #endregion
//    }
//}
