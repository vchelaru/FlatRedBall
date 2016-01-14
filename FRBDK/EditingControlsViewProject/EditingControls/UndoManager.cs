using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EditingControls
{

    public class UndoManager
    {
        UndoState mCurrentSnapshot;

        List<UndoState> mUndos = new List<UndoState>();

        public void Clear()
        {
            mUndos = new List<UndoState>();
            mCurrentSnapshot = null;
        }


        public void SaveSnapshot(object selectedObject)
        {
            mCurrentSnapshot = UndoState.FromObject(selectedObject);

        }

        public bool SaveUndo(object selectedObject)
        {
            var possibleUndo = mCurrentSnapshot.GetDiff(selectedObject);

            if (possibleUndo != null)
            {
                mUndos.Add(possibleUndo);
            }

            return possibleUndo != null;
        }

        public void PerformUndo()
        {
            if (mUndos.Count != 0)
            {
                var last = mUndos.Last();
                last.ApplyUndo();
                mUndos.RemoveAt(mUndos.Count - 1);
                SaveSnapshot(last.Owner);
            }

        }
    }
}
