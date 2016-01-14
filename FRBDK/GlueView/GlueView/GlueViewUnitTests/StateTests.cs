using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Content.Instructions;
using GlueView.SaveClasses;
using FlatRedBall.Math;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue;
using FlatRedBall;

namespace GlueViewUnitTests
{
    [TestFixture]
    public class StateTests
    {
        NamedObjectSave mNamedObjectWithSetVariable;
        EntitySave mEntitySave;
        //EntitySave mDerivedEntitySave;

        EntitySave mContainer;

        ElementRuntime mElementRuntime;
        ElementRuntime mContainerElementRuntime;

        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            CreateEntitySaves();


            CreateElementRuntime();
        }

        private void CreateElementRuntime()
        {
            mElementRuntime = new ElementRuntime(mEntitySave, null, null, null, null);

            mContainerElementRuntime = new ElementRuntime(mContainer, null, null, null, null);
        }

        private void CreateEntitySaves()
        {
            mEntitySave = new EntitySave();
            mEntitySave.Name = "StateTestEntity";

            ObjectFinder.Self.GlueProject.Entities.Add(mEntitySave);

            CreateNamedObjectWithSetVariable();

            CreateEntityVariables();

            CreateEntitySaveState();

            mContainer = new EntitySave();
            mContainer.Name = "StateTestContainerEntity";


            NamedObjectSave nos = new NamedObjectSave();
            nos.InstanceName = mEntitySave.Name + "Instance";
            nos.SourceType = SourceType.Entity;
            nos.SourceClassType = mEntitySave.Name;
            mContainer.NamedObjects.Add(nos);


            CustomVariable stateTunnel = new CustomVariable();
            stateTunnel.SourceObject = nos.InstanceName;
            stateTunnel.SourceObjectProperty = "CurrentState";
            stateTunnel.Type = "VariableState";
            stateTunnel.Name = "StateTunnelVariable";
            mContainer.CustomVariables.Add(stateTunnel);

            CreateContainerEntityState();

        }

        private void CreateEntityVariables()
        {
            CustomVariable customVariable = new CustomVariable();
            customVariable.Name = "X";
            customVariable.Type = "float";
            mEntitySave.CustomVariables.Add(customVariable);

            customVariable = new CustomVariable();
            customVariable.Name = "CurrentState";
            customVariable.Type = "VariableState";
            customVariable.DefaultValue = "FirstState";
            mEntitySave.CustomVariables.Add(customVariable);

            customVariable = new CustomVariable();
            customVariable.Name = "SpriteScaleX";
            customVariable.Type = "float";
            customVariable.SourceObject = "SpriteObject";
            customVariable.SourceObjectProperty = "ScaleX";
            mEntitySave.CustomVariables.Add(customVariable);
        }

        private void CreateContainerEntityState()
        {
            StateSave stateSave = new StateSave();
            stateSave.Name = "SetContainedFirstState";

            InstructionSave instructionSave = new InstructionSave();
            instructionSave.Type = "VariableState";
            instructionSave.Value = "FirstState";
            instructionSave.Member = "StateTunnelVariable";

            stateSave.InstructionSaves.Add(instructionSave);

            mContainer.States.Add(stateSave);
        }

        private void CreateEntitySaveState()
        {
            StateSave stateSave = new StateSave();
            stateSave.Name = "FirstState";

            InstructionSave instructionSave = new InstructionSave();
            instructionSave.Member = "X";
            instructionSave.Value = 10.0f;
            stateSave.InstructionSaves.Add(instructionSave);

            instructionSave = new InstructionSave();
            instructionSave.Member = "SpriteScaleX";
            instructionSave.Value = 4.0f;
            stateSave.InstructionSaves.Add(instructionSave);

            mEntitySave.States.Add(stateSave);

        }

        private void CreateNamedObjectWithSetVariable()
        {
            mNamedObjectWithSetVariable = new NamedObjectSave();
            mNamedObjectWithSetVariable.InstanceName = "SpriteObject";
            // This will be complicated because it requires FRB to be instantiated!
            //mNamedObjectWithSetVariable.SourceType = SourceType.File;
            //mNamedObjectWithSetVariable.SourceFile = "Entities/StateTestEntity/SceneFile.scnx";
            //mNamedObjectWithSetVariable.SourceName = "Untextured (Sprite)";

            mNamedObjectWithSetVariable.SourceType = SourceType.FlatRedBallType;
            mNamedObjectWithSetVariable.SourceClassType = "Sprite";

            mNamedObjectWithSetVariable.UpdateCustomProperties();
            mNamedObjectWithSetVariable.SetPropertyValue("Y", 10.0f);

            mEntitySave.NamedObjects.Add(mNamedObjectWithSetVariable);
            
        }

        [Test]
        public void Test()
        {
            StateSave firstState = new StateSave();
            InstructionSave instructionSave = new InstructionSave();
            instructionSave.Type = "float";
            instructionSave.Value = 0.0f;
            instructionSave.Member = "X";
            firstState.InstructionSaves.Add(instructionSave);

            StateSave secondState = new StateSave();
            instructionSave = instructionSave.Clone();
            instructionSave.Value = 10.0f;
            secondState.InstructionSaves.Add(instructionSave);


            StateSave combined = StateSaveExtensionMethodsGlueView.CreateCombinedState(firstState, secondState, .5f);

            if (MathFunctions.RoundToInt((float)combined.InstructionSaves[0].Value) != 5)
            {
                throw new Exception("CreateCombined is not properly combining States");
            }
        }

        [Test]
        public void TestStateSetting()
        {
            FlatRedBall.PositionedObject directObjectReference = 
                ((FlatRedBall.PositionedObject)mElementRuntime.ContainedElements[0].DirectObjectReference);
            directObjectReference.ForceUpdateDependencies();

            float yBefore = directObjectReference.Y;
            mElementRuntime.SetState("", false);

            directObjectReference.ForceUpdateDependencies();
            float yAfter = directObjectReference.Y;


            if (yBefore != yAfter)
            {
                throw new Exception("Setting an empty state modifies the position values when it shouldn't");
            }


            mContainerElementRuntime.SetState(mContainer.States[0].Name);

            mContainerElementRuntime.ContainedElements[0].ForceUpdateDependencies();
            if (mContainerElementRuntime.ContainedElements[0].X != 10.0f)
            {
                throw new Exception("Setting a state on a container which sets the state of a contained object seems to not be actually setting the state of the contained object.");
            }

            Sprite sprite = mContainerElementRuntime.ContainedElements[0].ContainedElements[0].DirectObjectReference as Sprite;
            if (sprite.ScaleX != 4.0f)
            {
                throw new Exception("Setting a state on a container which sets the state of a contained object, and that state sets a tunneled variable on a FRB type seems to not be working properly");
            }
        }

        [Test]
        public void TestTryGetCurrentCustomVariableValueForState()
        {
            object value;
            mElementRuntime.TryGetCurrentCustomVariableValue("CurrentState", out value);

            if (value == null || !(value is StateSave))
            {
                throw new Exception("This value needs to be non-null.");
            }

        }
    }
}
