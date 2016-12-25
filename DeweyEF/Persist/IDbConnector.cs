using System;
using System.Data.Common;

namespace DeweyEF.Persist
{
    public interface IDbConnector : IEquatable<IDbConnector>
    {
        #region Properties

        /// <summary>
        ///  The connection string to the database pre-connected to by calling PreConnectToFile()
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        ///  The actual database file path if any revealed by the recent call to PreConnectToFile()
        /// </summary>
        string DbFilePath { get; }

        #endregion

        #region Methods

        /// <summary>
        ///  Creates and returns a connection for this db type
        /// </summary>
        /// <returns></returns>
        DbConnection GetConnection();

        /// <summary>
        ///  Initializes properties of the connector to prepare for the connection and returns the connection string
        ///  This doesn make any connection attempts at all
        /// </summary>
        /// <param name="dbFilePath">The file to connect with</param>
        void PreConnectToFile(string dbFilePath);

        /// <summary>
        ///  Prepares a new database file to write to; The file was specified in the last call to PreConnectToFile()
        /// </summary>
        void PrepareNewDatabase();

        #endregion
    }
}
