using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using FlatRedBall.Instructions;
using FlatRedBall.Math.Geometry;
using WpfDataUi;
using WpfDataUi.DataTypes;
using Arrow.Controls.DataUi;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Arrow.Data;
using WpfDataUi.EventArguments;
using FlatRedBall.Arrow.Verification;
using System.Windows;
using FlatRedBall.Arrow.DataTypes;

namespace FlatRedBall.Arrow.Managers
{
    public class PropertyGridManager : Singleton<PropertyGridManager>
    {
        #region Fields

        DataUiGrid mPropertyGrid;

        Dictionary<string, TypeMemberDisplayProperties> mTypeMemberDisplayProperties = new Dictionary<string, TypeMemberDisplayProperties>();

        #endregion

        public void Initialize(DataUiGrid propertyGrid)
        {
            mPropertyGrid = propertyGrid;

            mPropertyGrid.PropertyChange += HandlePropertyChange;
            mPropertyGrid.BeforePropertyChange += HandleBeforePropertyChange;
            InitializeDisplayProperties();

            InitializeDisplayerTypeAssociations();
        }

        private void HandleBeforePropertyChange(string propertyName, BeforePropertyChangedArgs changeArgs)
        {
            if (propertyName == "Name")
            {
                // make sure this is a valid name, and if not, let's change it
                string whyIsntValid;

                var instance = ArrowState.Self.CurrentInstance;
                var elementSave = ArrowState.Self.CurrentArrowElementSave;
                NameVerifier.Self.IsInstanceNameValid(changeArgs.NewValue as string, out whyIsntValid, instance, elementSave);

                if (!string.IsNullOrEmpty(whyIsntValid))
                {
                    MessageBox.Show("Invalid name:\n" + whyIsntValid);
                    changeArgs.OverridingValue = changeArgs.OldValue;
                }
            }
        }

        private void HandlePropertyChange(string propertyName, PropertyChangedArgs arg2)
        {
            if (propertyName == "Name")
            {
                string newName = arg2.NewValue as string;

                // The runtime direct object reference changed its name.  Let's change the selected instance VM's name
                ArrowState.Self.CurrentInstanceVm.Name = newName;
                ArrowCommands.Self.File.GenerateGlux();
                ArrowCommands.Self.UpdateToSelectedElement();
            }
            else
            {
                ArrowCommands.Self.UpdateInstanceValuesFromRuntime(ArrowState.Self.CurrentContainedElementRuntime);
                NamedObjectSave currentNos = ArrowState.Self.CurrentNamedObjectSave;

                ArrowCommands.Self.UpdateNosFromArrowInstance(ArrowState.Self.CurrentInstance, ArrowState.Self.CurrentNamedObjectSave);


            }


            ArrowCommands.Self.File.SaveProject();
            ArrowCommands.Self.File.SaveGlux();
        }

        private void InitializeDisplayerTypeAssociations()
        {
            Func<Type, bool> func = (type) => type == typeof(Microsoft.Xna.Framework.Color);
            Type controlType = typeof(ColorDisplay);

            var kvp = new KeyValuePair<Func<Type, bool>, Type>(func, controlType);
            SingleDataUiContainer.TypeDisplayerAssociation.Add(kvp);

            //func = (type) => type == typeof(float);
            //controlType = typeof(WpfDataUi.Controls.AngleSelectorDisplay);
            //kvp = new KeyValuePair<Func<Type, bool>, Type>(func, controlType);
            //SingleDataUiContainer.TypeDisplayerAssociation.Add(kvp);

        }

        private void InitializeDisplayProperties()
        {

            mPropertyGrid.TypesToIgnore.Add(typeof(Microsoft.Xna.Framework.Matrix));
            mPropertyGrid.TypesToIgnore.Add(typeof(Microsoft.Xna.Framework.Vector2));
            mPropertyGrid.TypesToIgnore.Add(typeof(Microsoft.Xna.Framework.Vector3));
            mPropertyGrid.TypesToIgnore.Add(typeof(Microsoft.Xna.Framework.Vector4));

            Ignore("XVelocity");
            Ignore("YVelocity");
            Ignore("ZVelocity");

            Ignore("AnimationChains");
            Ignore("CurrentChain");
            Ignore("CurrentChainIndex");
            Ignore("CustomBehavior");
            Ignore("JustChangedFrame");
            Ignore("JustCycled");
            Ignore("ScaleX");
            Ignore("ScaleY");
            Ignore("ScaleXVelocity");
            Ignore("ScaleYVelocity");
            Ignore("TimeIntoAnimation");

            // We can't just ignore all relative values - we need to selectively
            // show them depending on if the object has a parent or not.
            //foreach (var relationship in InstructionManager.AbsoluteRelativeValueRelationships)
            //{
            //    Ignore(relationship.RelativeValue);
            //}

            foreach (var relationship in InstructionManager.VelocityValueRelationships)
            {
                Ignore(relationship.Acceleration);
                Ignore(relationship.Velocity);
            }


            var properties = TypeMemberDisplayPropertiesManager.Self.GetCircleDisplayProperties();
            mTypeMemberDisplayProperties.Add(properties.Type, properties);


            properties = TypeMemberDisplayPropertiesManager.Self.GetAARectDisplayProperties();
            mTypeMemberDisplayProperties.Add(properties.Type, properties);

            properties = TypeMemberDisplayPropertiesManager.Self.GetGlueElementRuntimeProperties();
            mTypeMemberDisplayProperties.Add(properties.Type, properties);


        }

        private void Ignore(string member)
        {
            if (!string.IsNullOrEmpty(member) && !mPropertyGrid.MembersToIgnore.Contains(member))
            {
                mPropertyGrid.MembersToIgnore.Add(member);
            }
        }

        internal void UpdateToSelectedInstance()
        {
            if (ArrowState.Self.CurrentContainedElementRuntime != null)
            {
                if (ArrowState.Self.CurrentContainedElementRuntime.DirectObjectReference != null)
                {

                    mPropertyGrid.Instance = ArrowState.Self.CurrentContainedElementRuntime.DirectObjectReference;

                    string fullName = mPropertyGrid.Instance.GetType().FullName;

                    TypeMemberDisplayProperties foundTmdp;

                    if (mTypeMemberDisplayProperties.ContainsKey(fullName))
                    {
                        foundTmdp = mTypeMemberDisplayProperties[fullName];
                        mPropertyGrid.Apply(mTypeMemberDisplayProperties[fullName]);
                    }
                    else
                    {
                        foundTmdp = new TypeMemberDisplayProperties();
                    }

                    TypeMemberDisplayPropertiesManager.Self.EliminateAbsoluteOrRelativeValuesIfNecessary(
                        mPropertyGrid, foundTmdp);

                    mPropertyGrid.Refresh();
                }
                else if (ArrowState.Self.CurrentInstance is ArrowElementInstance)
                {
                    mPropertyGrid.Instance = ArrowState.Self.CurrentContainedElementRuntime;

                    string fullName = mPropertyGrid.Instance.GetType().FullName;

                    TypeMemberDisplayProperties foundTmdp;

                    if (mTypeMemberDisplayProperties.ContainsKey(fullName))
                    {
                        foundTmdp = mTypeMemberDisplayProperties[fullName];
                        mPropertyGrid.Apply(mTypeMemberDisplayProperties[fullName]);
                    }
                    else
                    {
                        foundTmdp = new TypeMemberDisplayProperties();
                    }

                    TypeMemberDisplayPropertiesManager.Self.EliminateAbsoluteOrRelativeValuesIfNecessary(
                        mPropertyGrid, foundTmdp);

                    mPropertyGrid.Refresh();
                }
            }
            else if(mPropertyGrid != null)
            {
                mPropertyGrid.Instance = null;
            }
        }



    }
}
