using System;
using System.Threading.Tasks;
using Dewey.Objects;

namespace Dewey.Client
{
    /// <summary>
    ///  Interface for classes that represent sessions to implement
    /// </summary>
    public interface ISession : IDisposable
    {
        #region Properties

        // TODO how to make it thread-safe?
        /// <summary>
        ///  Whether object update should be suppressed (for instance in a loading process)
        /// </summary>
        bool SuppressUpdate { get; set; }

        /// <summary>
        ///  The current command in the session; if nested commands are supported it's normally the topmost
        /// </summary>
        ICommand CurrentCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        ///  Marks the specified object as dirty object in the session
        /// </summary>
        /// <param name="obj">The dirty object to mark</param>
        void AddDirtyObject(DeweyObject obj);

        /// <summary>
        ///  Marks the specified object as to be deleted in the session (normally this action cannot be undone within the command (transaction))
        /// </summary>
        /// <param name="obj">The object to delete</param>
        void RemoveObject(DeweyObject obj);

        /// <summary>
        ///  Removes the specified object from updating (update/delete)
        /// </summary>
        /// <param name="deweyObject">The object to remove from updating</param>
        void CancelObjectUpdate(DeweyObject deweyObject);

        /// <summary>
        ///  Creates and starts a new command
        /// </summary>
        /// <param name="description">The optional description of the command to start</param>
        /// <returns>The command created and started</returns>
        ICommand StartCommand(string description);

        /// <summary>
        ///  Commits the top level command
        /// </summary>
        void CommitCommand();

        /// <summary>
        ///  Async version of CommitCommand()
        /// </summary>
        Task CommitCommandAsync();

        #endregion
    }
}
