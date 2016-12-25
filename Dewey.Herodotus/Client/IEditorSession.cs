using Dewey.Client;
using Herodotus;

namespace Dewey.Herodotus.Client
{
    public interface IEditorSession : ISession
    {
        #region Properties

        IChangesetManager ChangeManager { get; }

        ITrackingManager TrackingManager { get; }

        bool VirtualChange { get; set; }

        #endregion
    }
}
