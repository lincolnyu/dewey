using System.Collections.Generic;
using Dewey.Objects;
using Herodotus;

namespace DeweyEF.Herodotus.Client
{
    public class RemoveObjectsChange : ITrackedChange
    {
        #region Properties

        public IList<DeweyObject> RemovedObjects { get; set; }

        #endregion

        #region Methods

        public void Redo()
        {
            foreach (var obj in RemovedObjects)
            {
                obj.Delete();
            }
        }

        public void Undo()
        {
            foreach (var obj in RemovedObjects)
            {
                obj.Save();
            }
        }

        #endregion
    }
}
