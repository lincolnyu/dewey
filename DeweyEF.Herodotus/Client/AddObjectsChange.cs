using System.Collections.Generic;
using Dewey.Objects;
using Herodotus;

namespace DeweyEF.Herodotus.Client
{
    public class AddObjectsChange : ITrackedChange
    {
        #region Properties

        public IList<DeweyObject> AddedObjects { get; set; }

        #endregion

        public void Redo()
        {
            foreach (var obj in AddedObjects)
            {
                obj.Save();
            }
        }

        public void Undo()
        {
            foreach (var obj in AddedObjects)
            {
                obj.Delete();
            }
        }
    }
}
