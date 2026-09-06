using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;

using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.Managers;
using GlueUnitTests.TestSupport;
using Microsoft.Xna.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin
{
    /// <summary>
    /// Where a newly added object ends up. Creating the object and positioning it used to be separate
    /// messages to the game, so the position could be computed and then dropped - leaving the object at
    /// the origin both on screen and in the saved project. These pin that the position travels with the
    /// object's creation instead.
    /// </summary>
    public class NewObjectPositionTests : IDisposable
    {
        #region Test doubles

        /// <summary>
        /// Answers as a running game would, without one. <see cref="SentDtos"/> records what the
        /// RefreshManager decided to send, which is what the game would have acted on.
        /// </summary>
        class FakeCommandSender : CommandSender
        {
            public List<object> SentDtos { get; } = new List<object>();
            public Vector3? CameraPosition { get; set; } = Vector3.Zero;
            public bool AddObjectSucceeds { get; set; } = true;

            internal override Task<Vector3?> GetCameraPosition() => Task.FromResult(CameraPosition);

            public override Task<GeneralResponse<T>> Send<T>(object dto, SendImportance importance = SendImportance.Normal)
            {
                SentDtos.Add(dto);

                if (typeof(T) == typeof(AddObjectDtoListResponse))
                {
                    var list = dto as AddObjectDtoList;

                    if (!AddObjectSucceeds)
                    {
                        return Task.FromResult(GeneralResponse<T>.UnsuccessfulWith(
                            "The game is not ready to handle commands yet"));
                    }

                    var responseData = new AddObjectDtoListResponse
                    {
                        Data = list.Data.Select(_ => new AddObjectDtoResponse
                        {
                            CreationResponse = OptionallyAttemptedGeneralResponse.SuccessfulAttempt
                        }).ToList()
                    };

                    return Task.FromResult(new GeneralResponse<T>
                    {
                        Succeeded = true,
                        Data = (T)(object)responseData
                    });
                }

                return Task.FromResult(new GeneralResponse<T> { Succeeded = true });
            }
        }

        #endregion

        readonly CommandSender originalCommandSender;
        readonly bool originalIsRunning;
        readonly bool originalIsEditChecked;
        readonly VisualStudioProject originalMainProject;
        readonly GlueProjectSave originalGlueProject;
        readonly string originalRelativeDirectory;
        readonly bool originalSynchronousMode;
        readonly string tempProjectDirectory;
        readonly FakeCommandSender fakeCommandSender = new FakeCommandSender();
        readonly ScreenSave screen;

        public NewObjectPositionTests()
        {
            GlueTestBootstrap.EnsureInitialized();

            originalCommandSender = CommandSender.Self;
            CommandSender.Self = fakeCommandSender;

            originalIsRunning = CompilerViewModel.Self.IsRunning;
            originalIsEditChecked = CompilerViewModel.Self.IsEditChecked;
            originalMainProject = GlueState.Self.CurrentMainProject;
            originalGlueProject = ObjectFinder.Self.GlueProject;
            originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
            originalSynchronousMode = TaskManager.SynchronousMode;

            // Positioning regenerates the element it changed, which needs a real code project to write to.
            var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out tempProjectDirectory);
            GlueState.Self.CurrentMainProject = vsProject;
            FlatRedBall.IO.FileManager.RelativeDirectory = tempProjectDirectory + "\\";
            TaskManager.SynchronousMode = true;

            screen = new ScreenSave { Name = "Screens\\GameScreen" };

            ObjectFinder.Self.GlueProject = new GlueProjectSave();
            ObjectFinder.Self.GlueProject.Screens.Add(screen);
        }

        public void Dispose()
        {
            CommandSender.Self = originalCommandSender;
            CompilerViewModel.Self.IsRunning = originalIsRunning;
            CompilerViewModel.Self.IsEditChecked = originalIsEditChecked;
            GlueState.Self.CurrentMainProject = originalMainProject;
            ObjectFinder.Self.GlueProject = originalGlueProject;
            FlatRedBall.IO.FileManager.RelativeDirectory = originalRelativeDirectory;
            TaskManager.SynchronousMode = originalSynchronousMode;

            try
            {
                Directory.Delete(tempProjectDirectory, recursive: true);
            }
            catch
            {
                // best-effort cleanup; a stray temp dir isn't worth failing the test over
            }
        }

        #region Helpers

        RefreshManager CreateRefreshManagerInEditMode()
        {
            // CompilerViewModel's constructor is private and it is a singleton, so the test drives the
            // real one and restores it in Dispose rather than building its own.
            CompilerViewModel.Self.IsRunning = true;
            CompilerViewModel.Self.IsEditChecked = true;

            var refreshManager = new RefreshManager((_, __) => Task.FromResult((string)null), (_, __) => { })
            {
                ViewModel = CompilerViewModel.Self
            };

            // Its constructor assigns itself to RefreshManager.VariableSendingManager, which
            // CreateAddObjectDtoFor uses to qualify each variable's type.
            new VariableSendingManager(refreshManager);

            return refreshManager;
        }

        NamedObjectSave AddEntityInstanceToScreen(string instanceName)
        {
            var namedObject = new NamedObjectSave
            {
                InstanceName = instanceName,
                SourceType = SourceType.Entity,
                SourceClassType = "Entities\\Player"
            };

            screen.NamedObjects.Add(namedObject);

            return namedObject;
        }

        static float? GetVariable(NamedObjectSave namedObject, string name) =>
            namedObject.GetCustomVariable(name)?.Value as float?;

        /// <summary>
        /// The same variable read off an object that has been through the DTO's JSON round trip, which
        /// widens a float to a double. What matters here is the value the game receives, not its boxed type.
        /// </summary>
        static float? GetRoundTrippedVariable(NamedObjectSave namedObject, string name)
        {
            var value = namedObject.GetCustomVariable(name)?.Value;

            return value == null ? (float?)null : Convert.ToSingle(value);
        }

        static NamedObjectSave GetSentNamedObject(FakeCommandSender sender, string instanceName) =>
            sender.SentDtos.OfType<AddObjectDtoList>()
                .SelectMany(item => item.Data)
                .Select(item => item.NamedObjectSave)
                .FirstOrDefault(item => item.InstanceName == instanceName);

        #endregion

        /// <summary>
        /// Dropping an entity onto the game window computes a world position in Glue and hands it to the
        /// RefreshManager. That position is the whole point of dropping at a spot rather than adding from
        /// the tree, so it has to reach the object.
        /// </summary>
        [Fact]
        public async Task NewObject_WhenAPositionWasForced_GetsThatPosition()
        {
            var refreshManager = CreateRefreshManagerInEditMode();
            var namedObject = AddEntityInstanceToScreen("PlayerInstance");

            refreshManager.NextPositionValues = new RefreshManager.NewObjectListPositionValues
            {
                ForcedNextObjectPosition = new System.Numerics.Vector2(300, 200)
            };

            await refreshManager.HandleNewObjectList(new List<NamedObjectSave> { namedObject });

            GetVariable(namedObject, "X").ShouldBe(300f);
            GetVariable(namedObject, "Y").ShouldBe(200f);
        }

        /// <summary>
        /// The position has to be on the object before it is sent, or the game creates it at the origin and
        /// only moves it if a second message arrives.
        /// </summary>
        [Fact]
        public async Task NewObject_WhenAPositionWasForced_SendsItWithTheObjectsCreation()
        {
            var refreshManager = CreateRefreshManagerInEditMode();
            var namedObject = AddEntityInstanceToScreen("PlayerInstance");

            refreshManager.NextPositionValues = new RefreshManager.NewObjectListPositionValues
            {
                ForcedNextObjectPosition = new System.Numerics.Vector2(300, 200)
            };

            await refreshManager.HandleNewObjectList(new List<NamedObjectSave> { namedObject });

            var sent = GetSentNamedObject(fakeCommandSender, "PlayerInstance");

            sent.ShouldNotBeNull();
            GetRoundTrippedVariable(sent, "X").ShouldBe(300f);
            GetRoundTrippedVariable(sent, "Y").ShouldBe(200f);
        }

        /// <summary>
        /// A failed add falls back to a full stop/rebuild/relaunch. The restart rebuilds from the saved
        /// project, so a position that was only going to be applied afterwards is lost for good - the
        /// object is at the origin in the game and in the .glsj.
        /// </summary>
        [Fact]
        public async Task NewObject_WhenTheAddFailedAndTriggeredARestart_KeepsTheForcedPosition()
        {
            var refreshManager = CreateRefreshManagerInEditMode();
            var namedObject = AddEntityInstanceToScreen("PlayerInstance");

            fakeCommandSender.AddObjectSucceeds = false;

            refreshManager.NextPositionValues = new RefreshManager.NewObjectListPositionValues
            {
                ForcedNextObjectPosition = new System.Numerics.Vector2(300, 200)
            };

            await refreshManager.HandleNewObjectList(new List<NamedObjectSave> { namedObject });

            GetVariable(namedObject, "X").ShouldBe(300f);
            GetVariable(namedObject, "Y").ShouldBe(200f);
        }

        /// <summary>
        /// Dropping at the origin is a deliberate choice, so it has to be written like any other position.
        /// Skipping the write because the value happens to be zero leaves whatever was there before.
        /// </summary>
        [Fact]
        public async Task NewObject_WhenTheForcedPositionIsTheOrigin_StillWritesIt()
        {
            var refreshManager = CreateRefreshManagerInEditMode();
            var namedObject = AddEntityInstanceToScreen("PlayerInstance");

            refreshManager.NextPositionValues = new RefreshManager.NewObjectListPositionValues
            {
                ForcedNextObjectPosition = new System.Numerics.Vector2(0, 0)
            };

            await refreshManager.HandleNewObjectList(new List<NamedObjectSave> { namedObject });

            GetVariable(namedObject, "X").ShouldBe(0f);
            GetVariable(namedObject, "Y").ShouldBe(0f);
        }

        /// <summary>
        /// Adding from the tree has no drop point, so the object goes to the camera so the user can see it.
        /// </summary>
        [Fact]
        public async Task NewObject_WithNoForcedPosition_GoesToTheCamera()
        {
            var refreshManager = CreateRefreshManagerInEditMode();
            var namedObject = AddEntityInstanceToScreen("PlayerInstance");

            fakeCommandSender.CameraPosition = new Vector3(50, 75, 0);

            await refreshManager.HandleNewObjectList(new List<NamedObjectSave> { namedObject });

            GetVariable(namedObject, "X").ShouldBe(50f);
            GetVariable(namedObject, "Y").ShouldBe(75f);
        }

        /// <summary>
        /// A game that died mid-operation answers nothing, which is not the same as a camera at the origin.
        /// Writing 0,0 in that case puts a real variable assignment on the object that the user never asked
        /// for, so the position is left alone instead.
        /// </summary>
        [Fact]
        public async Task NewObject_WhenTheGameDidNotAnswer_LeavesThePositionAlone()
        {
            var refreshManager = CreateRefreshManagerInEditMode();
            var namedObject = AddEntityInstanceToScreen("PlayerInstance");

            fakeCommandSender.CameraPosition = null;

            await refreshManager.HandleNewObjectList(new List<NamedObjectSave> { namedObject });

            namedObject.GetCustomVariable("X").ShouldBeNull();
            namedObject.GetCustomVariable("Y").ShouldBeNull();
        }
    }
}
