using System;

namespace Dewey.Objects
{
    public abstract class BasePoco
    {
        #region Properties

        /// <summary>
        ///  Unique ID at least within this poco type
        /// </summary>
        public long Id { get; set; }

        #endregion

        #region Methods

        public virtual DeweyObject InstantiateDeweyObject()
        {
            return (DeweyObject)Activator.CreateInstance(GetDeweyType());
        }

        public virtual Type GetPocoType()
        {
            return GetType();
        }

        public abstract Type GetDeweyType();

        public abstract Type GetDeweyIdType();

        #endregion
    }
}
