using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

using FlatRedBall.Utilities;
using System.Collections;

namespace FlatRedBall.Math
{
    #region XML Docs
    /// <summary>
    /// Interface for an object which can be attached to a parent.
    /// </summary>
    /// <remarks>
    /// IAttachables do not necessarily have to be positioned objects - they
    /// can also be objects which have un-positioned attachments.  For example,
    /// an event in a scripted sequence might have a parent event which it executes
    /// after.
    /// </remarks>
    #endregion
    public interface IAttachable : INameable
    {
        #region Properties

        int HierarchyDepth { get; }

        IAttachable ParentAsIAttachable { get; }

        IList ChildrenAsIAttachables { get; }

        #region XML Docs
        /// <summary>
        /// Gets all lists that the instance belongs to.
        /// </summary>
        /// <remarks>
        /// This property provides the two-way relationship between IAttachables and
        /// and common FlatRedBall Lists.
        /// </remarks>
        #endregion
        List<IAttachableRemovable> ListsBelongingTo { get; }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Clears all attachments to parents and removes all attached Children.
        /// </summary>
        #endregion
        void ClearRelationships();

        #region XML Docs
        /// <summary>
        /// Detaches the instance from its parent and removes it from its parent's Children List.
        /// </summary>
        #endregion
        void Detach();

        #region XML Docs
        /// <summary>
        /// Forces an update of the instance and calls ForceUpdateDependencies on its parent.
        /// </summary>
        /// <remarks>
        /// This method will recursively crawl up the Parent property until it reaches the TopParent.
        /// </remarks>
        #endregion
        void ForceUpdateDependencies();

        #endregion
    }


    //#region XML Docs
    ///// <summary>
    ///// Defines the interface for objects which can attach to PositionedObjects.
    ///// </summary>
    ///// <remarks>
    ///// The IAttachable interface is rarelyl explicitly implemented in game code; rather,
    ///// most game objects which require attachment inherit from the PositionedObject class.
    ///// <seealso cref="FlatRedBall.PositionedObject"/>
    ///// </remarks>
    //#endregion
    //public interface IAttachable<T> : IAttachable, IPositionable, IRotatable, INameable where T : IAttachable
    //{
    //    #region Properties


    //    // The IAttachable works with the PositionedObject class
    //    // to avoid the usage of properties in tight loops for Position.

    //    #region XML Docs
    //    /// <summary>
    //    /// Gets the instance's parent.
    //    /// </summary>
    //    #endregion
    //    T Parent { get; }

    //    #region XML Docs
    //    /// <summary>
    //    /// Gets the instance's Children (objects attached to this).
    //    /// </summary>
    //    #endregion
    //    AttachableList<T> Children { get;}

    //    #region XML Docs
    //    /// <summary>
    //    /// Gets the object at the top of the hierarchy.
    //    /// </summary>
    //    /// <remarks>
    //    /// If the instance does not have a parent this property will return
    //    /// the instance itself.
    //    /// </remarks>
    //    #endregion
    //    T TopParent { get;}


    //    #endregion

    //    #region Methods

    //    #region XML Docs
    //    /// <summary>
    //    /// Attaches the instance to the argument PositionedObject.
    //    /// </summary>
    //    /// <param name="newParent">The object to attach to.</param>
    //    /// <param name="changeRelative">Whether this method should
    //    /// change the relative values.
    //    /// If this property is true, then the absolute values will
    //    /// remain the same before and after the attachment but relative values
    //    /// will change.  If this value is false, then relative values will remain
    //    /// the same but absolute values will change.  Passing false as this value will
    //    /// likely cause objects to "pop" to new locations.
    //    /// </param>
    //    #endregion
    //    void AttachTo(PositionedObject newParent, bool changeRelative);



    //    #region XML Docs
    //    /// <summary>
    //    /// Determines if this is a parent, grandparent, etc of the argument IAttachable.
    //    /// </summary>
    //    /// <param name="attachableInQuestion">The IAttachable to test if this is a parent of.</param>
    //    /// <returns>If this is a parent of the argument IAttachable.</returns>
    //    #endregion
    //    bool IsParentOf(IAttachable attachableInQuestion);

    //    #region XML Docs
    //    /// <summary>
    //    /// Removes the instance from all Lists that it has a two-way relationship with.
    //    /// </summary>
    //    #endregion
    //    void RemoveSelfFromListsBelongingTo();

    //    #region XML Docs
    //    /// <summary>
    //    /// Attempts to update the dependencies if the argument currentTime does not match the last
    //    /// time UpdateDependencies has been called.
    //    /// </summary>
    //    /// <remarks>
    //    /// This method crawls up the Parent property until it reaches the TopParent or an instance
    //    /// that has had its last UpdateDependency called with the same argument currentTime.
    //    /// </remarks>
    //    /// <param name="currentTime">The value marking the current time of this frame.  This prevents
    //    /// the method from executing its logic multiple times per object per frame.  This is usually
    //    /// the TimeManager.CurrentTime.</param>
    //    #endregion
    //    void UpdateDependencies(double currentTime);
    //    #endregion
    //}
}
