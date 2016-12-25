using Dewey.Client;
using Dewey.Objects;

namespace Dewey.Helpers
{
    /// <summary>
    ///  Helper for command related activities
    /// </summary>
    public static class CommandHelper
    {
        #region Delegates

        public delegate void CommandAction();

        public delegate bool CancellableCommandAction();

        #endregion

        #region Methods

        /// <summary>
        ///  Creates a command if no command created yet
        /// </summary>
        /// <param name="session">The session that creates commands</param>
        /// <param name="commandDescription">The description for command</param>
        /// <param name="action">The action to be performed within the command scope</param>
        public static void RequiresCommand(this ISession session, string commandDescription, 
            CommandAction action)
        {
            if (session.CurrentCommand == null)
            {
                using (var command = session.StartCommand(commandDescription))
                {
                    action();
                    command.Commit();
                }
            }
            else
            {
                action();
            }
        }

        /// <summary>
        ///  Creates a cancellable if no command created yet
        /// </summary>
        /// <param name="session">The session that creates commands</param>
        /// <param name="commandDescription">The description for command</param>
        /// <param name="action">The action to be performed within the command scope</param>
        /// <remarks>
        ///  NOTE depending on the implementation nested commands may not be cancellable
        /// </remarks>
        public static void RequiresCommand(this ISession session, string commandDescription,
            CancellableCommandAction action)
        {
            if (session.CurrentCommand == null)
            {
                using (var command = session.StartCommand(commandDescription))
                {
                    var successful = action();
                    if (successful)
                    {
                        command.Commit();
                    }
                }
            }
            else
            {
                action();
            }
        }

        /// <summary>
        ///  Creates a command if no command created yet
        /// </summary>
        /// <param name="obj">The object that provides the session</param>
        /// <param name="commandDescription">The description for command</param>
        /// <param name="action">The action to be performed within the command scope</param>
        public static void RequiresCommand(this DeweyObject obj, string commandDescription,
            CommandAction action)
        {
            obj.Session.RequiresCommand(commandDescription, action);
        }


        /// <summary>
        ///  Creates a cancellable command if no command created yet
        /// </summary>
        /// <param name="obj">The object that provides the session</param>
        /// <param name="commandDescription">The description for command</param>
        /// <param name="action">The action to be performed within the command scope</param>
        ///  NOTE depending on the implementation nested commands may not be cancellable
        public static void RequiresCommand(this DeweyObject obj, string commandDescription,
            CancellableCommandAction action)
        {
            obj.Session.RequiresCommand(commandDescription, action);
        }

        #endregion
    }
}
