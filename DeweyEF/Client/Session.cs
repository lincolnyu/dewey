using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Dewey.Client;
using Dewey.Objects;
using Dewey.Utilities;

namespace DeweyEF.Client
{
    /// <summary>
    ///  A (persistence) session
    /// </summary>
    public abstract class Session : ISession, IObjectQuery
    {
        #region Fields

        private long _tempId = 1;

        #endregion

        #region Constructors

        /// <summary>
        ///  Instantiates a session
        /// </summary>
        protected Session()
        {
			StackedCommands = new Stack<ICommand>();
        }

        #endregion

        #region Properties

        #region ISession members

        /// <summary>
        ///  The current command if available or null
        /// </summary>
        public ICommand CurrentCommand 
		{
            get
            {
                return StackedCommands.FirstOrDefault();
            }
		}

        #endregion

        /// <summary>
        ///  A flag used to suppress entity update, mainly used during loading
        /// </summary>
        public bool SuppressUpdate { get; set; }

        // TODO review ID generation
        public IDictionary<Type, IIdGenerator> IdGenerators { get; protected set; }

		/// <summary>
		///  Nested commands stored on the stack
		/// </summary>
		public Stack<ICommand> StackedCommands { get; private set; }


        // TODO review ID generation
        protected abstract IEnumerable<DeweyObject> AllObjectsForIdGenerator { get; }

        #endregion

        #region Events

        #region IObjectQuery members

        /// <summary>
        ///  An event that's fired when object collection has changed
        /// </summary>
        /// <remarks>
        ///  Currently this is implemented as a command induced collection changed event
        ///  which should cover all collection changes initiated by this user session
        ///  In the future we may use this also to notify of collection changes from the server
        ///  or may use a separate event for that
        /// </remarks>
        public event NotifyCollectionChangedEventHandler ObjectCollectionChanged;

        #endregion

        #endregion

        #region Methods

        #region IObjectQuery members

        public virtual IEnumerable<T> GetAllObjectsOfType<T>() where T : DeweyObject
        {
            return GetAllObjectsOfType(typeof (T)).Cast<T>();
        }

        public abstract IEnumerable<DeweyObject> GetAllObjectsOfType(Type type);

        public abstract IEnumerable<Type> GetAllDeweyIdTypes();

        #endregion

        #region ISession members

        #region IDisposable members

        /// <summary>
        ///  Disposes of the object
        /// </summary>
        public virtual void Dispose()
        {
        }

        #endregion

        public long GetTemporaryId()
        {
            return _tempId++;
        }

        // TODO review ID generation
        public void InitializeIdGenerator()
        {
            IdGenerators = new Dictionary<Type, IIdGenerator>();
            // TODO find out if this is the right assumption
            foreach (var o in AllObjectsForIdGenerator)
            {
                var idType = o.GetDeweyIdType();
                var gen = GetOrCreateIdGeneratorForType(idType);
                gen.Use(o.Id);
            }
        }

        // TODO review ID generation
        public IIdGenerator GetOrCreateIdGeneratorForType(Type type)
        {
            IIdGenerator gen;
            if (!IdGenerators.TryGetValue(type, out gen))
            {
                gen = new IdGenerator();
                IdGenerators[type] = gen;
            }
            return gen;
        }

        /// <summary>
        ///  Adds the object to the 
        /// </summary>
        /// <param name="objectToAdd"></param>
        public virtual void AddDirtyObject(DeweyObject objectToAdd)
        {
            var command = (Command) CurrentCommand;
            if (command != null)
            {
                command.AddDirtyObject(objectToAdd);    
            }
        }

        /// <summary>
        ///  Adds the object to the removal list
        /// </summary>
        /// <param name="objectToRemove"></param>
        public virtual void RemoveObject(DeweyObject objectToRemove)
        {
            var command = (Command)CurrentCommand;
            if (command != null)
            {
                command.RemoveObject(objectToRemove);
            }
        }

        /// <summary>
        ///  removes the object from update/removal list
        /// </summary>
        /// <param name="objectToCancel">The object to cancel update</param>
        public void CancelObjectUpdate(DeweyObject objectToCancel)
        {
            var command = (Command)CurrentCommand;
            if (command != null)
            {
                command.CancelObjectUpdate(objectToCancel);
            }
        }

        /// <summary>
        ///  Creates and starts a new command
        /// </summary>
        /// <param name="description">The optional description of the command to start</param>
        /// <returns>The command created and started</returns>
        public virtual ICommand StartCommand(string description)
        {
            var command = new Command(this, description);
            StackNewCommand(command);
            return command;
        }

        /// <summary>
        ///  Adds newly started command to the stack; derived classes must always call this after a new command is created and started
        /// </summary>
        /// <param name="command"></param>
        protected void StackNewCommand(ICommand command)
        {
            StackedCommands.Push(command);
        }

        /// <summary>
        ///  Commits the specified the command (though it should always be the current and thereby checked for)
        /// </summary>
        public virtual void CommitCommand()
        {
            var command = StackedCommands.Peek();
            command.Commit();
        }

        /// <summary>
        ///  Async version of CommitCommand()
        /// </summary>
        public virtual async Task CommitCommandAsync()
        {
            var command = StackedCommands.Peek();
            await command.CommitAsync();
        }

        public void UnuseId(DeweyObject obj)
        {
            var type = obj.GetDeweyIdType();
            var id = obj.Id;
            var gen = IdGenerators[type];
            gen.Unuse(id);
            obj.Id = 0;
        }

        #endregion

        /// <summary>
        ///  Creates a db context of the type the session knows to create
        /// </summary>
        /// <returns>The db context</returns>
        public abstract DbContext CreateDbContext();

        /// <summary>
        ///  Raises the ObjectCollectionChanged event
        /// </summary>
        /// <param name="e">The event arguments</param>
        /// <remarks>
        ///  As the session instanc ealmost has no knowledge of the object changes
        ///  It most likely is the command who knows about this and this is the method
        ///  for it to call to fire the event
        /// </remarks>
        public void RaiseObjectCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (ObjectCollectionChanged != null)
            {
                ObjectCollectionChanged(this, e);
            }
        }

        #endregion
    }
}
