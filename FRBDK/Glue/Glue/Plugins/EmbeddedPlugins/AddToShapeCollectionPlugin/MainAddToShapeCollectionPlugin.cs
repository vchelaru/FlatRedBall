using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.AddToShapeCollectionPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAddToShapeCollectionPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            this.ReactToTreeViewRightClickHandler += HandleTreeViewRightClick;
        }

        private void HandleTreeViewRightClick(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify)
        {
            if(rightClickedTreeNode.IsNamedObjectNode())
            {
                var nos = GlueState.Self.CurrentNamedObjectSave;

                if(nos?.SourceType == SourceType.FlatRedBallType && 
                    nos.SourceClassType == "ShapeCollection")
                {
                    AddAddShapeTreeNodes(menuToModify);
                }
            }
        }

        private void AddAddShapeTreeNodes(ContextMenuStrip menuToModify)
        {
            menuToModify.Items.Add("Add AxisAlignedRectangle", null, HandleAddAxisAlignedRectangle);
            menuToModify.Items.Add("Add Circle", null, HandleAddCircle);
            menuToModify.Items.Add("Add Polygon", null, HandleAddPolygon);

        }

        private void HandleAddAxisAlignedRectangle(object sender, EventArgs e)
        {
            string message = "Enter new AxisAlignedRectangle Name:";
            var sourceClassType = AvailableAssetTypes.CommonAtis.AxisAlignedRectangle;

            HandleAddShape(message, sourceClassType);
        }

        private void HandleAddCircle(object sender, EventArgs e)
        {
            string message = "Enter new Circle Name:";
            var sourceClassType = AvailableAssetTypes.CommonAtis.Circle;

            HandleAddShape(message, sourceClassType);
        }

        private void HandleAddPolygon(object sender, EventArgs e)
        {
            string message = "Enter new Polygon Name:";
            var sourceClassType = AvailableAssetTypes.CommonAtis.Polygon;

            var namedObjectSave = HandleAddShape(message, sourceClassType);

            var points = new List<Vector2>();
            points.Add(new Vector2(-16, 16));
            points.Add(new Vector2(16, 16));
            points.Add(new Vector2(16, -16));
            points.Add(new Vector2(-16, -16));
            points.Add(new Vector2(-16, 16));

            GlueCommands.Self.GluxCommands.SetVariableOn(namedObjectSave, "Points", typeof(List<Vector2>), points);

            //instructions.Value = points;

            GlueCommands.Self.ProjectCommands.SaveProjects();
            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
        }

        private static NamedObjectSave HandleAddShape(string message, AssetTypeInfo ati)
        {
            NamedObjectSave toReturn = null;

            var tiw = new TextInputWindow();
            tiw.Message = message;
            var dialogResult = tiw.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                string whyItIsntValid;
                NameVerifier.IsNamedObjectNameValid(tiw.Result, out whyItIsntValid);

                if (!string.IsNullOrEmpty(whyItIsntValid))
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(whyItIsntValid);
                }
                else
                {
                    var viewModel = new AddObjectViewModel();

                    viewModel.ObjectName = tiw.Result;
                    viewModel.SourceType = SaveClasses.SourceType.FlatRedBallType;
                    viewModel.SelectedAti = ati;

                    toReturn = GlueCommands.Self.GluxCommands.AddNewNamedObjectToSelectedElement(viewModel);

                    GlueState.Self.CurrentNamedObjectSave = toReturn;

                }
            }

            return toReturn;
        }
    }
}
