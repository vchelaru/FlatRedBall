namespace GlueControl.Editing
{
    public partial class EditorVisuals
    {
        public static void DrawRepositionDirections(FlatRedBall.TileCollisions.TileShapeCollection tileShapeCollection)
        {
            foreach (var rectangle in tileShapeCollection.Rectangles)
            {
                DrawRepositionDirections(rectangle);
            }
        }
    }
}
