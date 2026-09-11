using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;
using System.Linq;

namespace FlatRedBall.Glue.Elements
{
    /// <summary>
    /// Decides which object gets selected after one is deleted, so the tree view's selection lands next to
    /// where the user was.
    /// </summary>
    public static class RemovalSelection
    {
        /// <summary>
        /// Picks the neighbour to select after <paramref name="removed"/> has been taken out of
        /// <paramref name="remainingSiblings"/>. <paramref name="indexOfRemoved"/> is where it used to be.
        /// Returns null when nothing suitable is left, so the caller can fall back to the container.
        ///
        /// Only siblings shown in the same tree folder count. The list is flat, but the tree shows layers
        /// and collision relationships under their own folder nodes, so a neighbour picked purely by index
        /// can sit in a different (collapsed) folder and selecting it pops that folder open. See GitHub
        /// issue #2265.
        /// </summary>
        public static NamedObjectSave PickSuccessor(IList<NamedObjectSave> remainingSiblings, NamedObjectSave removed, int indexOfRemoved)
        {
            bool IsInSameFolder(NamedObjectSave candidate) =>
                candidate.IsLayer == removed.IsLayer &&
                candidate.IsCollisionRelationship() == removed.IsCollisionRelationship();

            var after = remainingSiblings.Skip(indexOfRemoved).FirstOrDefault(IsInSameFolder);
            if (after != null)
            {
                return after;
            }

            return remainingSiblings.Take(indexOfRemoved).LastOrDefault(IsInSameFolder);
        }
    }
}
