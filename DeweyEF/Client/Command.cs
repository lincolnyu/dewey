using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Dewey.Client;
using Dewey.Objects;
using DeweyEF.Persist;

namespace DeweyEF.Client
{
    public class Command : ICommand
    {
        #region Fields

        private bool _toMergeOnDisposal;

        #endregion

        #region Constructors

        public Command(Session session, string description)
        {
            Session = session;
            Description = description;

            var currentCommand = (Command)session.CurrentCommand;
            Db = currentCommand != null ? currentCommand.Db : session.CreateDbContext();
            
            Persister = new ObjectPersister(Db, session);

            AddedObjects = new HashSet<DeweyObject>();
            RemovedObjects = new HashSet<DeweyObject>();
            UpdatedObjects = new HashSet<DeweyObject>();
        }

        #endregion

        #region Properties

        public string Description { get; private set; }

        public ISession Session { get; private set; }

        public DbContext Db { get; private set; }

        public ObjectPersister Persister { get; private set; }

        public ISet<DeweyObject> AddedObjects { get; private set; }

        public ISet<DeweyObject> RemovedObjects { get; private set; }

        public ISet<DeweyObject> UpdatedObjects { get; private set; }

        public bool ForcedSave { get; set; }

        #endregion

        #region Methods

        #region ICommand members

        public virtual void Dispose()
        {
            var session = (Session) Session;
            if (session.CurrentCommand == this)
            {
                session.StackedCommands.Pop();

                if (session.StackedCommands.Count == 0)
                {
                    // outmost command which is in charge of the persistence
                    if (Db != null)
                    {
                        Db.Dispose();
                        Db = null;
                    }
                }
                else if (_toMergeOnDisposal) // only does it when it's been requested
                {
                    MergeCommands();
                }
            }
        }

        public virtual void Commit()
        {
            var session = (Session) Session;
            var commandCount = session.StackedCommands.Count;
            if (commandCount == 1)
            {
                PersistEntityChanges();
                if (AddedObjects.Any() || UpdatedObjects.Any() || RemovedObjects.Any() || ForcedSave)
                {
                    Db.SaveChanges();
                    GetIdsFromDb();
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(commandCount > 1);
                _toMergeOnDisposal = true;
            }
        }

        public virtual async Task CommitAsync()
        {
            var session = (Session)Session;
            var commandCount = session.StackedCommands.Count;
            if (commandCount == 1)
            {
                PersistEntityChanges();
                if (AddedObjects.Any() || UpdatedObjects.Any() || RemovedObjects.Any() || ForcedSave)
                {
                    await Db.SaveChangesAsync();
                    GetIdsFromDb();
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(commandCount > 1);
                _toMergeOnDisposal = true;
            }
        }

        private void MergeCommands()
        {
            var session = (Session)Session;
            var parent = (Command) session.StackedCommands.Peek();
            //merges with parent
            foreach (var addedObject in AddedObjects)
            {
                if (parent.RemovedObjects.Contains(addedObject))
                {
                    parent.RemovedObjects.Remove(addedObject);
                }
                parent.AddedObjects.Add(addedObject);
            }
            foreach (var updatedObject in UpdatedObjects)
            {
                if (parent.RemovedObjects.Contains(updatedObject))
                {
                    parent.RemovedObjects.Remove(updatedObject);
                }
                else if (!parent.AddedObjects.Contains(updatedObject) && !parent.UpdatedObjects.Contains(updatedObject))
                {
                    parent.UpdatedObjects.Add(updatedObject);
                }
            }
            foreach (var removedObject in RemovedObjects)
            {
                if (parent.AddedObjects.Contains(removedObject))
                {
                    parent.AddedObjects.Remove(removedObject);
                }
                else if (parent.UpdatedObjects.Contains(removedObject))
                {
                    parent.UpdatedObjects.Remove(removedObject);
                }
                else
                {
                    parent.RemovedObjects.Add(removedObject);
                }
            }
        }

        private void GetIdsFromDb()
        {
            foreach (var obj in AddedObjects)
            {
                if (obj.Id > 0)
                {
                    continue;
                }
                obj.Id = obj.Poco.Id;
            }
        }

        public virtual void AddDirtyObject(DeweyObject objectToAdd)
        {
            if (Session.SuppressUpdate)
            {
                objectToAdd.DeweyState = ObjectStates.Synced;
                return;
            }

            if (RemovedObjects.Contains(objectToAdd))
            {
                // TODO if the user follows standard way of using command we probably won't need this
                RemovedObjects.Remove(objectToAdd);
            }
            else if (objectToAdd.Id == 0)
            {
                if (((Session) Session).IdGenerators != null)
                {
                    var idType = objectToAdd.GetDeweyIdType();
                    // currently we generate id from UI and therefore can use id being zero to indicate that it's new
                    objectToAdd.Id = ((Session)Session).GetOrCreateIdGeneratorForType(idType).Generate();    
                }
                else
                {
                    objectToAdd.Id = ((Session) Session).GetTemporaryId();
                }
                
                AddedObjects.Add(objectToAdd);
            }
            else if (!AddedObjects.Contains(objectToAdd))
            {
                UpdatedObjects.Add(objectToAdd);
            }
        }

        public virtual void RemoveObject(DeweyObject objectToRemove)
        {
            if (Session.SuppressUpdate)
            {
                objectToRemove.DeweyState = ObjectStates.Synced;
                return;
            }

            if (AddedObjects.Remove(objectToRemove))
            {
                return;
            }
            UpdatedObjects.Remove(objectToRemove);
            RemovedObjects.Add(objectToRemove);
        }

        public virtual void CancelObjectUpdate(DeweyObject objectToCancel)
        {
            if (Session.SuppressUpdate)
            {
                objectToCancel.DeweyState = ObjectStates.Synced; // NOTE it shoiuld already be Synced though
                return;
            }

            if (AddedObjects.Remove(objectToCancel))
            {
                return;
            }
            if (UpdatedObjects.Remove(objectToCancel))
            {
                return;
            }
            RemovedObjects.Remove(objectToCancel);
        }

        #endregion

        private void PersistEntityChanges()
        {
            // NOTE The persister will set the states

            Persister.Save(AddedObjects);
            
            Persister.Save(UpdatedObjects);
            
            Persister.Delete(RemovedObjects);
        }

        #endregion
    }
}
