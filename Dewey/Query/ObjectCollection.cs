using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Dewey.Client;
using Dewey.Objects;

namespace Dewey.Query
{
    /// <summary>
    ///  A dictionary class that keeps a collection of Dewey objects that allows lookup by id
    ///  and resolves update requirements on object loading
    /// </summary>
    public class ObjectCollection : INotifyCollectionChanged,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<long, DeweyObject>>
    {
        #region Delegates

        public delegate bool LoadOnDemandDelegate(Type deweyIdType, long id, out DeweyObject obj);

        #endregion

        #region Nested types

        /// <summary>
        /// Copy context implementation for the required by this collection to sync data
        /// </summary>
        public class CopyContext : ICopyFromPocoContext
        {
            #region Constructors

            /// <summary>
            ///  Instantiate a copy context
            /// </summary>
            /// <param name="owner">The owner object collection that organises the copy and provides the required data</param>
            public CopyContext(ObjectCollection owner)
            {
                Owner = owner;
            }

            #endregion

            #region Properties

            #region ICopyFromPocoContext members

            /// <summary>
            ///  The session it's working in
            /// </summary>
            public ISession Session { get; set; }

            #endregion

            /// <summary>
            ///  the object collection that owns this context and provides object map for ID matching
            /// </summary>
            private ObjectCollection Owner { get; set; }

            #endregion

            #region Methods

            #region ICopyFromPocoContext members

            public bool TryGetObject<T>(long id, out T obj) where T : DeweyObject
            {
                return Owner.TryGetObject(id, out obj);
            }

            public bool TryGetObject(Type type, long id, out DeweyObject obj)
            {
                return Owner.TryGetObject(type, id, out obj);
            }

            public bool LoadOnDemand<T>(long id, out T obj) where T : DeweyObject
            {
                DeweyObject o;
                var result = LoadOnDemand(typeof (T), id, out o);
                obj = (T)o;
                return result;
            }

            public bool LoadOnDemand(Type deweyIdType, long id, out DeweyObject obj)
            {
                return Owner.LoadOnDemand(deweyIdType, id, out obj);
            }

            #endregion

            #endregion
        }


        /// <summary>
        ///  A internally used structure that stores a pair of objects to copy between
        /// </summary>
        public struct ObjectPair
        {
            #region Constructors

            /// <summary>
            ///  Constructs an instance using specified info
            /// </summary>
            /// <param name="source">The value for the Source property</param>
            /// <param name="target">The value for the Target property</param>
            public ObjectPair(BasePoco source, DeweyObject target)
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
            public BasePoco Source { get; private set; }

            /// <summary>
            ///  The object to update (normally the in-memory object)
            /// </summary>
            public DeweyObject Target { get; private set; }

            #endregion
        }

        #endregion

        #region Fields

        /// <summary>
        ///  The internal collection that stores all objects this collection has provides look up for object with id
        /// </summary>
        private readonly Dictionary<Type, Dictionary<long, DeweyObject>> _objectMap;

        #endregion

        #region Constructors

        /// <summary>
        ///  Constructs an instance of this class
        /// </summary>
        public ObjectCollection(ISession session)
        {
            _objectMap = new Dictionary<Type, Dictionary<long, DeweyObject>>();
            Context = new CopyContext(this) { Session = session };
        }

        #endregion

        #region Properties

        #region IReadOnlyDictionary<long, IDeweyObject> members

        /// <summary>
        ///  The total number of items in the collection
        /// </summary>
        public int Count
        {
            get { return _objectMap.Count; }
        }

        /// <summary>
        ///  All keys (object types) of the collection
        /// </summary>
        public IEnumerable<Type> Keys
        {
            get { return _objectMap.Keys; }
        }

        /// <summary>
        ///  All values (objects) of the collection
        /// </summary>
        public IEnumerable<IReadOnlyDictionary<long, DeweyObject>> Values
        {
            get { return _objectMap.Values; }
        }

        /// <summary>
        ///  Returns a value (dictionary from id to object) with the specified key (type)
        /// </summary>
        /// <param name="type">The key (type of the dictionary to return)</param>
        /// <returns>The dictionary for the specified type if any or an exception is thrown</returns>
        /// <exception cref="KeyNotFoundException">If not found this exception is thrown from the internal dictionary</exception>
        public IReadOnlyDictionary<long, DeweyObject> this[Type type]
        {
            get { return _objectMap[type]; }
        }

        #endregion

        public CopyContext Context { get; private set; }

        public LoadOnDemandDelegate LoadOnDemand { get; set; }

        #endregion

        #region Events

        /// <summary>
        ///  Event fired whenever the collection is changed
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region Methods

        #region IReadOnlyDictionary<long, IDeweyObject> members

        public IEnumerator<KeyValuePair<Type, IReadOnlyDictionary<long, DeweyObject>>> GetEnumerator()
        {
            return _objectMap.Select(kvp => new KeyValuePair<Type,
                IReadOnlyDictionary<long, DeweyObject>>(kvp.Key, kvp.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(Type key)
        {
            return _objectMap.ContainsKey(key);
        }

        public bool TryGetValue(Type key, out IReadOnlyDictionary<long, DeweyObject> value)
        {
            Dictionary<long, DeweyObject> result;
            var ret = _objectMap.TryGetValue(key, out result);
            value = result;
            return ret;
        }

        #endregion

        /// <summary>
        ///  Loads the specified objects that've just been loaded from the DB to the internal collection
        /// </summary>
        /// <param name="pocos">The objects loaded from DB to be load</param>
        /// <returns>The pairs of updated dewey objects and corresponding pocos</returns>
        public IList<ObjectPair> Load(IEnumerable<BasePoco> pocos)
        {
            var objectsToUpdate = new List<ObjectPair>();
            var objectsAdded = new List<DeweyObject>();
            foreach (var poco in pocos)
            {
                Dictionary<long, DeweyObject> dict;
                var idType = poco.GetDeweyIdType();
                if (!_objectMap.TryGetValue(idType, out dict))
                {
                    dict = new Dictionary<long, DeweyObject>();
                    _objectMap[idType] = dict;
                }

                DeweyObject objInM;
                if (!dict.TryGetValue(poco.Id, out objInM))
                {
                    objInM = poco.InstantiateDeweyObject();
                    objInM.Id = poco.Id;
                    objInM.Session = Context.Session;
                    dict[objInM.Id] = objInM;
                    objectsAdded.Add(objInM);
                }
                objectsToUpdate.Add(new ObjectPair(poco, objInM));

                objInM.CopyFromPocoFields(poco);
            }

            foreach (var p in objectsToUpdate)
            {
                p.Target.CopyFromPocoRelations(p.Source, Context);
            }

            if (CollectionChanged != null)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, objectsAdded);
                CollectionChanged(this, args);
            }

            return objectsToUpdate;
        }

        /// <summary>
        ///  Remove the specified objects from the collection
        ///  Caller's responsibility to make sure that've been properly saved as per their dewey states
        /// </summary>
        /// <param name="objectsToDrop">The objects to remove</param>
        /// <param name="forceDrop">Drop the objects regardless of their ID validity</param>
        public void Drop(IList<DeweyObject> objectsToDrop, bool forceDrop=false)
        {
            foreach (var obj in objectsToDrop)
            {
                if (obj.HardReferenceCount > 0 && !forceDrop)
                {
                    // the object is held by change tracking system and therefore shouldn't be dropped
                    continue;
                }

                var idType = obj.GetDeweyIdType();
                var dict = _objectMap[idType];
                dict.Remove(obj.Id);
                if (dict.Count == 0)
                {
                    _objectMap.Remove(idType);
                }
            }

            if (CollectionChanged != null)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)objectsToDrop);
                CollectionChanged(this, args);
            }
        }

        /// <summary>
        ///  UI requests new objects to be added; caller's responsibility to make sure that they 
        ///  are assigned valid IDs and properly saved
        /// </summary>
        /// <param name="objectsToAdd">The objects to add</param>
        public void Add(IEnumerable<DeweyObject> objectsToAdd)
        {
            // TODO review if this is not to be used
            //var newlyLoaded = new List<DeweyObject>();

            foreach (var obj in objectsToAdd)
            {
                var idType = obj.GetDeweyIdType();
                Dictionary<long, DeweyObject> dict;
                if (!_objectMap.TryGetValue(idType, out dict))
                {
                    dict = new Dictionary<long, DeweyObject>();
                    _objectMap.Add(idType, dict);
                }
                // TODO DB generated IDs?
                // NOTE currently we assume ids are generated by code and therefore are available upon object creation
                //      However if it's not the case, this might be the right place to check for zero IDs and update
                //      them from the pocos (persistence) this is reserved for this potential future change
                // NOTE this method should be called after the changes have been updated to DB and therefore IDs are generated
                //      ...

                // NOTE chances are that the objects were not removed at all 
                // TODO is object existing in the dictionary actually an erroneous condition?
                if (!dict.ContainsKey(obj.Id))
                {
                    dict.Add(obj.Id, obj);
                    //newlyLoaded.Add(obj);
                }
            }

            if (CollectionChanged != null)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)objectsToAdd);
                CollectionChanged(this, args);
            }
        }

        public bool TryGetObject<T>(long id, out T obj) where T : DeweyObject
        {
            DeweyObject o;
            var ret = TryGetObject(typeof(T), id, out o);
            obj = (T)o;
            return ret;
        }

        public bool TryGetObject(Type type, long id, out DeweyObject obj)
        {
            Dictionary<long, DeweyObject> dict;
            if (!_objectMap.TryGetValue(type, out dict))
            {
                obj = null;
                return false;
            }
            DeweyObject retrieved;
            var ret = dict.TryGetValue(id, out retrieved);
            obj = retrieved;
            return ret;
        }

        #endregion
    }
}