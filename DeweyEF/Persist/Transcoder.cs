using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Dewey.Objects;

namespace DeweyEF.Persist
{
    public abstract class Transcoder
    {
        #region Nested classes

        public class PocosAndAssocs
        {
            #region Constructors

            public PocosAndAssocs()
            {
                Pocos = new Dictionary<Type, ISet<BasePoco>>();
                Associations = new Dictionary<Type, ISet<object>>();
            }

            #endregion

            #region Properties

            public Dictionary<Type, ISet<BasePoco>> Pocos { get; private set; }
            public Dictionary<Type, ISet<object>> Associations { get; private set; }

            #endregion
        }

        #endregion

        #region Properties

        public abstract IEnumerable<Type> PocoTypes { get; }

        public abstract IEnumerable<Type> AssocationTypes { get; }

        #endregion

        #region Methods

        public void LoadPocosAndAssociations(DbContext db, PocosAndAssocs pocosAndAssocs)
        {
            foreach (var pocoType in PocoTypes)
            {
                var dbset = db.Set(pocoType);
                pocosAndAssocs.Pocos[pocoType] = new HashSet<BasePoco>(Enumerable.Cast<BasePoco>(dbset));
            }

            foreach (var assocType in AssocationTypes)
            {
                var dbset = db.Set(assocType);
                pocosAndAssocs.Associations[assocType] = new HashSet<object>(Enumerable.Cast<object>(dbset));
            }

            // TODO add in the future 
            // NOTE most derived class reference keys not supported due to EF bugs
        }

        public void SavePocosAndAssociations(DbContext db, PocosAndAssocs pocosAndAssocs)
        {
            // TODO clear database first
            // ...

            SaveObjects(db, pocosAndAssocs.Pocos);
            SaveObjects(db, pocosAndAssocs.Associations);
        }

        protected void SaveObjects<TObjectBase>(DbContext db, Dictionary<Type, ISet<TObjectBase>> objects)
        {
            foreach (var kvp in objects)
            {
                var assocType = kvp.Key;
                var assocs = kvp.Value;
                var dbset = db.Set(assocType);
                foreach (var assoc in assocs)
                {
                    dbset.Add(assoc);
                }
            }
        }

        #endregion
    }
}
