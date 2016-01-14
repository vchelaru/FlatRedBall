using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Content.AI.Pathfinding
{
    #region XML Docs
    /// <summary>
    /// An XML-Serializable object representing the state of a Link.
    /// </summary>
    #endregion
    public class LinkSave
    {

        public float Cost;

        public string NodeLinkingTo;


        public static LinkSave FromLink(FlatRedBall.AI.Pathfinding.Link link)
        {
            LinkSave linkSave = new LinkSave();

            linkSave.Cost = link.Cost;
            linkSave.NodeLinkingTo = link.NodeLinkingTo.Name;

            return linkSave;

        }

    }
}
