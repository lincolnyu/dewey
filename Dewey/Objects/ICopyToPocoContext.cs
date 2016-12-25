using System;
using Dewey.Client;

namespace Dewey.Objects
{
    public interface ICopyToPocoContext
    {
        #region Properties

        ISession Session { get; set; }

        #endregion

        #region Methods

        bool TryGetObject<T>(long id, out T obj) where T : BasePoco;

        bool TryGetObject(Type deweyIdType, long id, out BasePoco obj);

        #endregion
    }
}
