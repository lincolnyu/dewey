using System.Collections.Generic;

namespace DeweyEF.Persist
{
    public interface IDbConnectorFactory
    {
        #region Properties

        /// <summary>
        ///  Description of the database files for this database type
        /// </summary>
        string FileDescription { get; }

        /// <summary>
        ///  Supported file extension
        /// </summary>
        IEnumerable<string> FileExtensions { get; }

        #endregion

        #region Methods

        /// <summary>
        ///  Instantiates a connector of the associated type and return sit
        /// </summary>
        /// <returns></returns>
        IDbConnector GetConnector();

        #endregion
    }
}
