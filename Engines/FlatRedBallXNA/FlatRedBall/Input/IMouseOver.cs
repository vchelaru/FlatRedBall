using FlatRedBall.Graphics;
using FlatRedBall.Gui;

namespace FlatRedBall.Input
{
    ///<summary>
    /// Interface for checking mouse over's
    ///</summary>
    public interface IMouseOver
    {
        ///<summary>
        /// Check to see if object is under mouse.
        ///</summary>
        ///<param name="cursor">Cursor to check against.</param>
        ///<returns>True if under mouse.</returns>
        bool IsMouseOver(Cursor cursor);
        
        ///<summary>
        /// Check to see if object is under mouse.
        ///</summary>
        ///<param name="cursor">Cursor to check against.</param>
        ///<param name="layer">Layer object is on.</param>
        ///<returns>True if under mouse.</returns>
        bool IsMouseOver(Cursor cursor, Layer layer);
    }
}
