// *
// * Copyright (C) 2005 Roger Alsing : http://www.puzzleframework.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;

namespace Puzzle.Collections
{

    #region EventArgs and Delegate

    public class CollectionEventArgs : EventArgs
    {
        public readonly int Index;
        public readonly object Item;

        public CollectionEventArgs()
        {
            try {}
                #region ERROR HANDLER
#if DEBUG_COMPONA
			catch (Exception x)
			{
				System.Diagnostics.Debug.Write(x, "Puzzle");
				throw;
			}
#else
            catch
            {
                throw;
            }
#endif

            #endregion
        }

        public CollectionEventArgs(object item, int index)
        {
            #region PARAMETER VALIDATIONS

            if (item == null)
                throw new ArgumentNullException("item"); // Throw error if validation failed for "item"

            #endregion

            //IMPLEMENTATION 
            try
            {
                Index = index;
                Item = item;
            }
                #region ERROR HANDLER
#if DEBUG_COMPONA
			catch (Exception x)
			{
				System.Diagnostics.Debug.Write(x, "Puzzle");
				throw;
			}
#else
            catch
            {
                throw;
            }
#endif

            #endregion
        }
    }

    public delegate void CollectionEventHandler(object sender, CollectionEventArgs e);

    #endregion

    public abstract class CollectionBase : System.Collections.CollectionBase
    {
        #region Events

        public event CollectionEventHandler ItemAdded = null;

        protected virtual void OnItemAdded(int index, object item)
        {
            if (ItemAdded != null)
            {
                var e = new CollectionEventArgs(item, index);

                ItemAdded(this, e);
            }
        }

        public event CollectionEventHandler ItemRemoved = null;

        protected virtual void OnItemRemoved(int index, object item)
        {
            if (ItemRemoved != null)
            {
                var e = new CollectionEventArgs(item, index);

                ItemRemoved(this, e);
            }
        }

        public event EventHandler ItemsCleared = null;

        protected virtual void OnItemsCleared()
        {
            if (ItemsCleared != null)
                ItemsCleared(this, EventArgs.Empty);
        }

        #endregion

        #region Overrides

        protected override void OnClearComplete()
        {
            base.OnClearComplete();
            OnItemsCleared();
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            base.OnRemoveComplete(index, value);
            OnItemRemoved(index, value);
        }

        protected override void OnInsertComplete(int index, object value)
        {
            base.OnInsertComplete(index, value);
            OnItemAdded(index, value);
        }

        #endregion
    }
}