using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Dewey.Objects;
using DeweyEF.Client;
using Herodotus;


namespace DeweyEF.Herodotus.Client
{
    public class EditorCommand : Command
    {
        #region Fields

        private readonly Changeset _committingChangeset;

        private readonly IList<DeweyObject> _addedObjects;

        private readonly IList<DeweyObject> _removedObjects;

        #endregion

        #region Constructors

        public EditorCommand(EditorSession session, string description, bool suppressTracking=false) 
            : base(session, description)
        {
            if (session.SuppressUpdate || suppressTracking)
            {
                return;
            }
            session.TrackingManager.StartChangeset(description);
            _committingChangeset = session.TrackingManager.CommittingChangeset;

            _addedObjects = new List<DeweyObject>();
            _removedObjects = new List<DeweyObject>();
        }

        #endregion

        #region Methods

        #region Command members

        public override void Dispose()
        {
            // always commit changes at the end, since the changes have been done and need to be tracked anyways
            // regardless if they are updated to the persistence
            var trackingManager = ((EditorSession)Session).TrackingManager;
            if (_committingChangeset != null)
            {
                if (_addedObjects.Count > 0)
                {
                    var addObjectsChange = new AddObjectsChange
                    {
                        AddedObjects = _addedObjects
                    };
                    _committingChangeset.Changes.Insert(0, addObjectsChange);
                }

                if (_removedObjects.Count > 0)
                {
                    var removeObjectsChange = new RemoveObjectsChange
                    {
                        RemovedObjects = _removedObjects
                    };
                    _committingChangeset.Changes.Add(removeObjectsChange);
                }

                trackingManager.Commit(); // don't use merge as it may not be supported
            }

            base.Dispose();
        }

        public override void Commit()
        {
            PreCommit();

            base.Commit();

            PostCommit();
        }

        public override async Task CommitAsync()
        {
            PreCommit();

            await base.CommitAsync();

            PostCommit();
        }

        #endregion

        private void PreCommit()
        {
            var session = (EditorSession)Session;
            if (session.StackedCommands.Count == 1)
            {
                // objects referenced by the changeset
                foreach (var obj in AddedObjects)
                {
                    obj.AddHardReference();
                }
                foreach (var obj in RemovedObjects)
                {
                    obj.AddHardReference();
                }
                foreach (var obj in UpdatedObjects)
                {
                    obj.AddHardReference();
                }
            }
        }

        private void PostCommit()
        {
            var session = (EditorSession)Session;
            if (session.StackedCommands.Count == 1)
            {
                // this is to make sure they are loaded
                session.LoadedObjects.Add(AddedObjects);

                // this is strategy that clear id upon removal
                foreach (var obj in RemovedObjects)
                {
                    // TODO other ID generation methods?
                    session.UnuseId(obj);
                }
                // if the objects are no longer valid, they should be removed from the loaded object list anyways
                var objectsToDrop = RemovedObjects.ToList();
                session.LoadedObjects.Drop(objectsToDrop, true);
            }
        }

        #endregion
    }
}
