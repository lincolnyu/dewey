using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Dewey.Client;
using Dewey.Objects;

namespace DeweyEF.Persist
{
    public class ObjectPersister
    {
        #region Nested classes

        public class CopyContext : ICopyToPocoContext
        {
            #region Fields

            /// <summary>
            ///  Objects created this rount to be added but not yet in the database and to be referenced in the copying process
            /// </summary>
            private readonly Dictionary<Type, Dictionary<long, BasePoco>> _transientSet;

            #endregion

            #region Constructors

            public CopyContext(ObjectPersister owner)
            {
                Owner = owner;

                _transientSet = new Dictionary<Type, Dictionary<long, BasePoco>>();
            }

            #endregion

            #region Properties

            #region ICopyToPocoContext members

            public ISession Session { get; set; }

            #endregion

            private ObjectPersister Owner { get; set; }

            #endregion

            #region Methods

            #region ICopyToPocoContext members

            public bool TryGetObject<T>(long id, out T obj) where T : BasePoco
            {
                Dictionary<long, BasePoco> dict;
                if (_transientSet.TryGetValue(typeof(T), out dict))
                {
                    BasePoco retrieved;
                    if (dict.TryGetValue(id, out retrieved))
                    {
                        obj = (T)retrieved;
                        return true;
                    }
                }

                var dbset = Owner._db.Set<T>();
                obj = dbset.FirstOrDefault(x => x.Id == id);
                return obj != null;
            }

            public bool TryGetObject(Type type, long id, out BasePoco obj)
            {
                Dictionary<long, BasePoco> dict;
                if (_transientSet.TryGetValue(type, out dict))
                {
                    BasePoco retrieved;
                    if (dict.TryGetValue(id, out retrieved))
                    {
                        obj = retrieved;
                        return true;
                    }
                }

                var dbset = Owner._db.Set(type);
                var cast = Enumerable.Cast<BasePoco>(dbset);
                obj = cast.FirstOrDefault(x => x.Id == id);
                return obj != null;
            }


            #endregion

            public void AddTransientObject(BasePoco poco)
            {
                Dictionary<long, BasePoco> dict;
                if (!_transientSet.TryGetValue(poco.GetPocoType(), out dict))
                {
                    dict = new Dictionary<long, BasePoco>();
                }

                dict[poco.Id] = poco;
            }

            #endregion
        }


        /// <summary>
        ///  A internally used structure that stores a pair of objects to copy between
        /// </summary>
        private struct ObjectPair
        {
            #region Constructors

            /// <summary>
            ///  Constructs an instance using specified info
            /// </summary>
            /// <param name="source">The value for the source property</param>
            /// <param name="target">The value for the target property</param>
            public ObjectPair(DeweyObject source, BasePoco target)
                : this()
            {
                Source = source;
                Target = target;
            }

            #endregion

            #region Properties

            /// <summary>
            ///  The object to update from (normally the persistence)
            /// </summary>
            public DeweyObject Source { get; private set; }

            /// <summary>
            ///  The object to update (normally the in-memory object)
            /// </summary>
            public BasePoco Target { get; private set; }

            #endregion
        }

        private class FastDbSet
        {
            public DbSet DbSet { get; set; }
            public Dictionary<long, BasePoco> Pocos { get; set; }
        }


        #endregion

        #region Fields

        private readonly DbContext _db;

        private readonly ISession _session;

        #endregion

        #region Constructors

        public ObjectPersister(DbContext db, ISession session)
        {
            _db = db;
            _session = session;
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Deletes all the specified objects from the persistence
        /// </summary>
        /// <param name="objectsToDelete">The objects to delete</param>
        public void Delete(IEnumerable<DeweyObject> objectsToDelete)
        {
            foreach (var objToDel in objectsToDelete)
            {
                objToDel.DeweyState = ObjectStates.Deleted;

                var id = objToDel.Id;
                var pocoType = objToDel.GetPocoType();
                if (pocoType == null)
                {
                    continue;   // the object doesn't have a coresponding poco and is not mean to be persisted
                }
                var dbset = _db.Set(pocoType);
                var list = Enumerable.Cast<BasePoco>(dbset).ToList();
                var objInDb = list.FirstOrDefault(x => x.Id == id);
                if (objInDb != null)
                {
                    dbset.Remove(objInDb);
                }
            }
        }

        public CopyContext CreateContext()
        {
            return new CopyContext(this) { Session = _session };
        }

        /// <summary>
        ///  Saves all the specified objects (new or existing) to the persistence
        /// </summary>
        /// <param name="objectsToSave"></param>
        public void Save(IEnumerable<DeweyObject> objectsToSave)
        {
            var objPairs = new List<ObjectPair>();

            var copyContext = CreateContext();

            var cachedSets = new Dictionary<Type, FastDbSet>();

            foreach (var deweyToSave in objectsToSave)
            {
                deweyToSave.DeweyState = ObjectStates.Synced;

                var id = deweyToSave.Id;
                var pocoType = deweyToSave.GetPocoType();
                if (pocoType == null)
                {
                    continue;   // the object doesn't have a coresponding poco and is not mean to be persisted
                }

                FastDbSet fastset;
                if (!cachedSets.TryGetValue(pocoType, out fastset))
                {
                    var dbset = _db.Set(pocoType);
                    var pocos = new Dictionary<long, BasePoco>();
                    foreach (var p in Enumerable.Cast<BasePoco>(dbset))
                    {
                        pocos[p.Id] = p;
                    }
                    fastset = new FastDbSet
                    {
                        DbSet = dbset,
                        Pocos = pocos
                    };
                    cachedSets[pocoType] = fastset;
                }
                
                // TODO DB generate IDs?
                // NOTE if we use DB to generate IDs, then 'id' being zero is enough to determine that a new poco
                //      is to be created

                BasePoco poco;
                if (!fastset.Pocos.TryGetValue(id, out poco))
                {
                    poco = deweyToSave.InstantiatePoco();
                    poco.Id = id;
                    fastset.DbSet.Add(poco);
                    copyContext.AddTransientObject(poco);
                }
                objPairs.Add(new ObjectPair(deweyToSave, poco));
            }

            foreach (var p in objPairs)
            {
                p.Source.CopyToPoco(p.Target, copyContext);
            }
        }

        #endregion
    }
}
