using System;
using System.Threading.Tasks;

namespace Dewey.Client
{
    public interface ICommand : IDisposable
    {
        #region Methods

        /// <summary>
        ///  Commits the command, whether it's performed is up to implementation
        /// </summary>
        void Commit();

        /// <summary>
        ///  Async version of Commit()
        /// </summary>
        /// <returns></returns>
        Task CommitAsync();

        #endregion
    }
}
