using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Dewey.Objects;

namespace DeweyEF.Client
{
    /// <summary>
    ///  Interface that specifies complete/partial object retrieval
    /// </summary>
    public interface IObjectQuery
    {
        #region Events

        /// <summary>
        ///  An event that's fired when object collection has changed
        /// </summary>
        /// <remarks>
        ///  Currently this is implemented as a command induced collection changed event
        ///  which should cover all collection changes initiated by this user session
        ///  In the future we may use this also to notify of collection changes from the server
        ///  or may use a separate event for that
        /// </remarks>
        event NotifyCollectionChangedEventHandler ObjectCollectionChanged;

        #endregion

        #region Methods

        /// <summary>
        ///  Get all objects of the specified type in the project the session is opened for
        /// </summary>
        /// <typeparam name="T">The type of the object, it doesn't have to be a dewey id object</typeparam>
        /// <returns>The enumeration of those objects</returns>
        /// <remarks>
        ///  The type can be 
        ///   1. dewey id type, then it should return all objects governed by this dewey id type
        ///      if if this dewey id type is a base class of other dewey id types (dewey id type overlap/fallback)
        ///      which is used as a fallback option, then it should only return objects fall back
        ///      on this dewey id type and not return objects of the derived dewey id types
        ///   2. a type that is more derived than the most derived dewey id type (if this is supported)
        ///      it should return all objects that are of the specified type (or more derived)
        /// </remarks>
        IEnumerable<T> GetAllObjectsOfType<T>() where T : DeweyObject;

        /// <summary>
        ///  Get all objects of the specified type in the project the session is opened for
        /// </summary>
        /// <param name="type">The type which doesn't have to be a dewey id object type</param>
        /// <returns>The enumeration of those objects</returns>
        IEnumerable<DeweyObject> GetAllObjectsOfType(Type type);

        /// <summary>
        ///  Gets all dewey Id types
        /// </summary>
        /// <returns></returns>
        IEnumerable<Type> GetAllDeweyIdTypes();

        #endregion
    }
}
