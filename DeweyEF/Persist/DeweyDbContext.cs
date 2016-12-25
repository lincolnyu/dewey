using System.Data.Common;
using System.Data.Entity;
// ReSharper disable RedundantUsingDirective
using System.ComponentModel.DataAnnotations.Schema;
using Dewey.Objects;
// ReSharper restore RedundantUsingDirective

namespace DeweyEF.Persist
{
    public abstract class DeweyDbContext : DbContext
    {
        #region Constructors

        protected DeweyDbContext()
        {
        }

        protected DeweyDbContext(DbConnection connection, bool contextOwnsConnetion=true)
            : base(connection, contextOwnsConnetion)
        {
            
        }

        #endregion

        #region Methods

        #region DbContext members

        // ReSharper disable once RedundantOverridenMember
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // NOTE may be we can't configure anything for base class at all
            // http://stackoverflow.com/questions/13540976/multiple-inheritance-with-entity-framework-tpc
#if false            
            // configuration for the base abstract class
            modelBuilder.Entity<BasePoco>()
                .HasKey(x => x.Id)
                .Property(x => x.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
#endif
        }

        #endregion

        #endregion
    }
}
