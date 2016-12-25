using Dewey.Objects;
using Herodotus;

namespace Dewey.Herodotus.Client
{
    public class Pcm : PropertyChangeMarker
    {
        #region Constructors

        public Pcm(DeweyObject owner, string propertyName, object targetValue)
            : base(
            owner.Session != null && !((IEditorSession)owner.Session).VirtualChange ? ((IEditorSession)owner.Session).TrackingManager : null,
            owner, propertyName, targetValue)
        {
            Owner = owner;
        }

        #endregion

        #region Properties

        public DeweyObject Owner { get; private set; }

        #endregion

        #region Methods

        public override void Dispose()
        {
            base.Dispose();
            
            if (Owner.Session != null && !Owner.Session.SuppressUpdate)
            {
                Owner.Save();
            }
        }

        #endregion
    }
}
