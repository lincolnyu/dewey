using System;
using Dewey.Client;

namespace Dewey.Objects
{
    public interface ICopyFromPocoContext
    {
        #region Properties

        ISession Session { get; set; }

        #endregion

        #region Methods

        bool TryGetObject<T>(long id, out T obj) where T : DeweyObject;

        bool TryGetObject(Type deweyIdType, long id, out DeweyObject obj);

        bool LoadOnDemand<T>(long id, out T obj) where T : DeweyObject;

        bool LoadOnDemand(Type deweyIdType, long id, out DeweyObject obj);

        #endregion
    }
}
