using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.CodeGeneration
{

    public class ScrollableListCodeGenerator : ElementComponentCodeGenerator
    {
        // When the user makes an
        // Entity a ScrollableList,
        // then the Entity gets a PositionedObjectList
        // of Elements of the type specified by the user.
        // This list needs to have its activity be performed.
        // We're going to use the NamedObjectSaveCodeGenerator
        // and a NamedObjectSave to have the NamedObjectSaveCodeGenerator
        // handle that code generation for us.
        static NamedObjectSaveCodeGenerator mNamedObjectCodeGenerator = new NamedObjectSaveCodeGenerator();
        static NamedObjectSave mListNamedObjectSave = new NamedObjectSave();

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            // no longer do this for new projects
            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.RemoveIsScrollableEntityList)
            {
                return codeBlock;
            }


            if (element is EntitySave)
            {
                EntitySave entitySave = (EntitySave)element;

                if (entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
                {
                    string itemTypeWithoutPath = FileManager.RemovePath(entitySave.ItemType);

                    codeBlock.Line(string.Format("public System.Action<{0}> ScrollItemModified;", itemTypeWithoutPath));
                    codeBlock.Line(string.Format(
                            "FlatRedBall.Math.PositionedObjectList<{0}> mScrollableItems = new FlatRedBall.Math.PositionedObjectList<{0}>();",
                            itemTypeWithoutPath));

                    codeBlock.Line("FlatRedBall.PositionedObject mScrollableHandle;");
                    codeBlock.Line(string.Format("float mScrollableSpacing = {0};", entitySave.SpacingBetweenItems));
                    codeBlock.Line(string.Format("float mScrollableTopBoundary = {0};", entitySave.ListTopBound));
                    codeBlock.Line(string.Format("float mScrollableBottomBoundary = {0};", entitySave.ListBottomBound));

                    codeBlock.Line("int mScrollableFirstIndex = 0;");
                    codeBlock.Line(string.Format("{0} mLastCreatedScrollableItem;", itemTypeWithoutPath));

                    codeBlock
                        .Property("public int", "ScrollableFirstIndex")
                            .Set()
                                .Line("mScrollableFirstIndex = System.Math.Max(0, value);")

                                .If("mListShowingButUsePropertyPlease == null")
                                    .While("mScrollableItems.Count > 0")
                                        .Line("mScrollableItems.Last.Destroy();")
                                    .End()
                                .End()

                                .Else()
                                    // We add 1 because let's say the spacing is 10.  Even if the top to bottom was 1 unit, we'd see 1 item.
                                    .Line("int maxShown = 1 + (int)((mScrollableTopBoundary - mScrollableBottomBoundary) / mScrollableSpacing);")
                                    .Line("int maximumFirst = System.Math.Max(0, mListShowingButUsePropertyPlease.Count - maxShown);")
                                    .Line("mScrollableFirstIndex = System.Math.Min(maximumFirst, mScrollableFirstIndex);")
                                    .Line("mScrollableHandle.RelativeY = mScrollableFirstIndex * mScrollableSpacing + mScrollableTopBoundary;")
                                    .Line("FixScrollableHandleRelativeY();")
                                    .Line("int numberOfEntities = System.Math.Min(maxShown, ListShowing.Count);")

                                    .While("mScrollableItems.Count > ListShowing.Count || mScrollableItems.Count > numberOfEntities")
                                        .Line("mScrollableItems.Last.Destroy();")
                                    .End()

                                    .For("int i = 0; i < mScrollableItems.Count; i++")
                                        .Line("mScrollableItems[i].RelativeY = -( i + mScrollableFirstIndex ) * mScrollableSpacing;")
                                        .Line("mScrollableItems[i].ForceUpdateDependencies();")
                                        .If("ScrollItemModified != null")
                                            .Line("ScrollItemModified(mScrollableItems[i]);")
                                        .End()
                                    .End()

                                    .Line("PerformScrollableItemAdditionLogic();");


                    codeBlock.Line("private System.Collections.IList mListShowingButUsePropertyPlease;");

                    codeBlock
                        .Property("public System.Collections.IList", "ListShowing")
                            .Get()
                                .Line("return mListShowingButUsePropertyPlease;")
                            .End()

                            .Set()
                                .If("value != mListShowingButUsePropertyPlease")
                                    .Line("mListShowingButUsePropertyPlease = value;")
                                    .Line("ScrollableFirstIndex = 0;")
                                    .Line("RefreshAllScrollableListItems();");
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave)
            {
                EntitySave entitySave = (EntitySave)element;
                if (entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
                {
                    codeBlock.Line("mScrollableHandle = new FlatRedBall.PositionedObject();");

                    // We don't want to initialize the factory in the Initialize method otherwise
                    // it gets done async.  This means that this could get called while another Screen
                    // that used this factory is still around - then a Destroy will get called and bad things
                    // happen.
                    //codeBlock.Line(itemTypeWithoutPath + "Factory.Initialize(null, ContentManagerName);");
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave)
            {
                EntitySave entitySave = (EntitySave)element;
                if (entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
                {

                    string itemTypeWithoutPath = FileManager.RemovePath(entitySave.ItemType);
                    codeBlock.Line("Factories." + itemTypeWithoutPath + "Factory.Initialize(ContentManagerName);");

                    codeBlock.Line("mScrollableHandle.AttachTo(this, false);");
                    codeBlock.Line("mScrollableHandle.RelativeY = " + entitySave.ListTopBound + ";");
                    codeBlock.Line("FlatRedBall.SpriteManager.AddPositionedObject(mScrollableHandle);");
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave)
            {
                EntitySave entitySave = (EntitySave)element;
                if (entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
                {
                    string itemTypeWithoutPath = FileManager.RemovePath(entitySave.ItemType);

                    codeBlock
                        .For("int i = mScrollableItems.Count - 1; i > -1; i--")
                            .Line("mScrollableItems[i].Destroy();")
                        .End()

                        .Line("FlatRedBall.SpriteManager.RemovePositionedObject(mScrollableHandle);")
                        .Line("Factories." + itemTypeWithoutPath + "Factory.Destroy();");
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave)
            {
                EntitySave entitySave = (EntitySave)element;
                if (entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
                {
                    codeBlock.Line("ScrollableListActivity();");

                    mListNamedObjectSave.SourceType = SourceType.FlatRedBallType;
                    mListNamedObjectSave.SourceClassType = "PositionedObjectList<>";
                    mListNamedObjectSave.SourceClassGenericType = entitySave.ItemType;

                    mListNamedObjectSave.InstanceName = "mScrollableItems";

                    NamedObjectSaveCodeGenerator.GetActivityForNamedObject(mListNamedObjectSave, codeBlock);
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave)
            {
                EntitySave entitySave = (EntitySave)element;
                if (entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
                {
                    string itemTypeWithoutPath = FileManager.RemovePath(entitySave.ItemType);

                    string visibleSettingLine = "";

                    if (entitySave.ImplementsIVisible)
                    {
                        visibleSettingLine = "newItem.Visible = this.Visible;";
                    }
                    #region AddItemToTop

                    codeBlock
                        .Function("void", "AddItemToTop", "")
                            .Line(string.Format("{0} newItem = Factories.{0}Factory.CreateNew(LayerProvidedByContainer);", itemTypeWithoutPath))
                            .Line(visibleSettingLine)
                            .Line("mLastCreatedScrollableItem = newItem;")
                            .Line("newItem.AttachTo(mScrollableHandle, false);")
                            .If("mScrollableItems.Count > 0")
                                .Line("newItem.RelativeY = mScrollableItems[0].RelativeY + mScrollableSpacing;")
                            .End()
                            .Line("newItem.ForceUpdateDependencies();")

                            .Line("mScrollableFirstIndex--;")
                            .Line("mScrollableItems.Insert(0, newItem);")

                            .If("ScrollItemModified != null")
                                .Line("ScrollItemModified(newItem);")
                            .End()
                            .Line("mLastCreatedScrollableItem = null;")
                        .End()
                    #endregion

                    #region AddItemToBottom

                    .Function("void", "AddItemToBottom", "")
                            .Line(string.Format("{0} newItem = Factories.{0}Factory.CreateNew(LayerProvidedByContainer);", itemTypeWithoutPath))
                            .Line("mLastCreatedScrollableItem = newItem;")
                            .Line("newItem.AttachTo(mScrollableHandle, false);")

                            .If("mScrollableItems.Count > 0")
                                .Line("newItem.RelativeY = mScrollableItems.Last.RelativeY - mScrollableSpacing;")
                            .End()
                            .Else()
                                .Line("newItem.RelativeY = 0;")
                            .End()
                            .Line("newItem.ForceUpdateDependencies();")

                            .Line("mScrollableItems.Add(newItem);")

                            .If("ScrollItemModified != null")
                                .Line("ScrollItemModified(newItem);")
                            .End()
                            .Line("mLastCreatedScrollableItem = null;")
                        .End();
                    #endregion

                    codeBlock = codeBlock.Function("void", "ScrollableListActivity", "");
                        
                    var curBlock = codeBlock
                        .If("ListShowing != null")
                            .Line("FlatRedBall.Gui.Cursor cursor = FlatRedBall.Gui.GuiManager.Cursor;");

                    if (entitySave.ImplementsIClickable || entitySave.ImplementsIWindow || entitySave.GetInheritsFromIWindowOrIClickable())
                    {
                        curBlock = curBlock.If("cursor.PrimaryDown && HasCursorOver(GuiManager.Cursor)");
                    }
                    else
                    {
                        curBlock = curBlock.If("cursor.PrimaryDown");
                    }

                    curBlock = curBlock
                        .Line("mScrollableHandle.RelativeY += FlatRedBall.Gui.GuiManager.Cursor.WorldYChangeAt(this.Z, LayerProvidedByContainer);")
                        .Line("FixScrollableHandleRelativeY();")
                        .Line("mScrollableHandle.ForceUpdateDependenciesDeep();")
                        .End();

                    curBlock
                        .Line("PerformScrollableItemAdditionLogic();")

                        .Line("// remove from top")
                        .While("mScrollableItems.Count > 2 && mScrollableItems[1].Y > mScrollableTopBoundary")
                            .Line("RemoveItemFromTop();")
                        .End()

                        .Line("// remove from bottom")
                        .While("mScrollableItems.Count > 2 && mScrollableItems[mScrollableItems.Count - 2].Y < mScrollableBottomBoundary")
                            .Line("RemoveItemFromBottom();")
                        .End()

                        .Line("bool shouldRefreshAll = false;")
                        .While("ListShowing.Count < mScrollableItems.Count")
                            .Line("RemoveItemFromBottom();")
                            .Line("shouldRefreshAll = true;")
                        .End()

                        .If("shouldRefreshAll")
                            .Line("RefreshAllScrollableListItems();")
                        .End();

                    codeBlock
                        .Else()
                            .While("mScrollableItems.Count > 0")
                                .Line("mScrollableItems.Last.Destroy();");

                    codeBlock = codeBlock.End();
                     
                    codeBlock.Function("public bool", "IsScrollableItemNew", string.Format("{0} itemInQuestion", itemTypeWithoutPath))
                            .Line("return itemInQuestion == mLastCreatedScrollableItem;")
                        .End()

                        .Function("void", "RemoveItemFromTop", "")
                            .Line("mScrollableItems[0].Destroy();")
                            .Line("mScrollableFirstIndex++;")
                        .End()

                        .Function("void", "RemoveItemFromBottom", "")
                            .Line("mScrollableItems.Last.Destroy();")
                        .End()

                        .Function("public int", "GetAbsoluteIndexForItem", itemTypeWithoutPath + " item")
                            .Line("int indexInList = mScrollableItems.IndexOf(item);")
                            .Line("return mScrollableFirstIndex + indexInList;")
                        .End()

                        .Property("public bool", "IsAtTop")
                            .Get()
                                .Line("return mScrollableFirstIndex == 0;")
                            .End()
                        .End()

                        .Function("RefreshAllScrollableListItems", "", Type:"void")
                            .If("mListShowingButUsePropertyPlease == null")
                                .While("mScrollableItems.Count > 0")
                                    .Line("mScrollableItems.Last.Destroy();")
                                .End()
                            .End()

                            .Else()
                                .For("int i = 0; i < mScrollableItems.Count; i++")

                                    .If("ScrollItemModified != null")
                                        .Line("ScrollItemModified(mScrollableItems[i]);")
                                    .End()

                                .End()
                            .End()
                        .End()

                        .Function("RefreshToList", "", Type:"void", Public:true)
                            .Line("PerformScrollableItemAdditionLogic();")
                            .While("mScrollableItems.Count > 2 && mScrollableItems[1].Y > mScrollableTopBoundary")
                                .Line("RemoveItemFromTop();")
                            .End()

                            .While("mScrollableItems.Count > 2 && mScrollableItems[mScrollableItems.Count - 2].Y < mScrollableBottomBoundary || ListShowing.Count < mScrollableItems.Count")
                                .Line("RemoveItemFromBottom();")
                            .End()

                            .Line("RefreshAllScrollableListItems();")
                        .End()

                        .Function("void", "FixScrollableHandleRelativeY", "")
                            .Line("mScrollableHandle.RelativeY = System.Math.Min(mScrollableHandle.RelativeY,(ListShowing.Count - 1) * mScrollableSpacing + mScrollableBottomBoundary);")
                            .Line("mScrollableHandle.RelativeY = System.Math.Max(mScrollableHandle.RelativeY, " + entitySave.ListTopBound + ");")
                        .End()

                        .Function("void", "PerformScrollableItemAdditionLogic", "")
                            .Line("// add to bottom")
                            .While("mScrollableFirstIndex + mScrollableItems.Count < ListShowing.Count && (mScrollableItems.Count == 0 || mScrollableItems.Last.Y > mScrollableBottomBoundary)")
                                .Line("AddItemToBottom();")
                            .End()

                            .Line("// add to top")
                            .While("mScrollableFirstIndex > 0 && mScrollableItems.Count < ListShowing.Count && (mScrollableItems.Count == 0 || mScrollableItems[0].Y < mScrollableTopBoundary)")
                                .Line("AddItemToTop();")
                            .End()
                        .End();
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, IElement element)
        {
            return codeBlock;
        }
    }
}
