namespace Dewey.Utilities
{
    public interface IIdGenerator
    {
        #region Methods

        long Generate();

        bool Use(long id);

        bool Unuse(long id);

        #endregion
    }
}
