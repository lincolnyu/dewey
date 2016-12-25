using System.Collections.Generic;
using System.Linq;
using Dewey.Objects;
using Dewey.Client;
using DeweyEF.Client;
using Dewey.Herodotus.Client;
using Herodotus;
using Dewey.Query;

namespace DeweyEF.Herodotus.Client
{
    public abstract class EditorSession : Session, IEditorSession
    {
        #region Fields 

        private readonly CompleteManager _completeManager;

        #endregion

        #region Constructors

        protected EditorSession()
        {
            _completeManager = new CompleteManager();
            _completeManager.Reinitialize();
            TrackingManager.IsTrackingEnabled = true;
            LoadedObjects = new ObjectCollection(this);
        }

        #endregion

        #region Properties

        #region IEditorSession Members

        public IChangesetManager ChangeManager
        {
            get
            {
                return _completeManager;
            }
        }

        public ITrackingManager TrackingManager
        {
            get
            {
                return _completeManager;
            }
        }

        public bool VirtualChange { get; set; }

        public bool SuppressTracking { get; set; }

        public ObjectCollection LoadedObjects { get; private set; }

        protected override IEnumerable<DeweyObject> AllObjectsForIdGenerator
        {
            get
            {
                return LoadedObjects.Values.SelectMany(dict => dict.Values);
            }
        }


        #endregion

        #endregion

        #region Methods

        #region Session members

        public override ICommand StartCommand(string description)
        {
            var command = new EditorCommand(this, description);
            StackNewCommand(command);
            return command;
        }

        #endregion

        public ICommand StartUntrackingCommand(string description)
        {
            var command = new EditorCommand(this, description, true);
            StackNewCommand(command);
            return command;
        }

        public bool CanRedo()
        {
            return ChangeManager.CanRedo();
        }

        public bool CanUndo()
        {
            return ChangeManager.CanUndo();
        }

        public void Redo()
        {
            // note just use normal command to bypass change tracking
            using (var cmd = StartUntrackingCommand("Redo"))
            {
                ChangeManager.Redo();
                cmd.Commit();
            }
        }

        public void Undo()
        {
            // note just use normal command to bypass change tracking
            using (var cmd = StartUntrackingCommand("Undo"))
            {
                ChangeManager.Undo();
                cmd.Commit();
            }
        }

        #endregion
    }
}
