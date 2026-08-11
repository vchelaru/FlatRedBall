using System;
using System.Collections.Generic;
using System.Threading;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.Managers;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.GlueControlTests;

// Issue #2047: adding a new instance to a list (e.g. a Player into a PlayerList) during "Run in Edit"
// occasionally throws InvalidOperationException ("Collection was modified; enumeration operation may
// not execute") out of Newtonsoft.Json, deep inside VariableSendingManager.PushVariableChangesToGame.
// That method builds a GlueVariableSetDataList whose NamedObjectsToUpdate holds live references to
// Glue's in-memory NamedObjectSave model objects (see NamedObjectWithElementName.NamedObjectSave), then
// JSON-serializes the whole graph - including each NamedObjectSave's own ContainedObjects list - to
// compute a dedup hash. If another part of Glue mutates that same ContainedObjects list (e.g. an "add to
// list" operation) concurrently, Json.NET's enumerator throws mid-walk. This only reproduces under a
// genuine data race, so the test keeps a background thread mutating the list while the foreground thread
// repeatedly calls the real production method.
public class VariableSendingManagerConcurrencyTests
{
    [Fact]
    public void PushVariableChangesToGame_DoesNotThrow_WhenTargetListIsMutatedConcurrently()
    {
        GlueTestBootstrap.EnsureInitialized();

        var originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        try
        {
            var glueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
            var screen = new ScreenSave { Name = "Screens\\Level1" };
            glueProject.Screens.Add(screen);

            var playerList = new NamedObjectSave
            {
                InstanceName = "PlayerList",
                SourceType = SourceType.FlatRedBallType,
                SourceClassType = "PositionedObjectList<T>",
            };
            for (int i = 0; i < 300; i++)
            {
                playerList.ContainedObjects.Add(new NamedObjectSave { InstanceName = $"Player{i}" });
            }
            screen.NamedObjects.Add(playerList);

            FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = glueProject;

            var refreshManager = new RefreshManager((_, _) => System.Threading.Tasks.Task.FromResult(""), (_, _) => { });
            var sendingManager = new VariableSendingManager(refreshManager);

            var stopMutating = false;
            Exception mutatorException = null;
            var mutatorThread = new Thread(() =>
            {
                try
                {
                    var newInstanceNumber = 1000;
                    while (!Volatile.Read(ref stopMutating))
                    {
                        playerList.ContainedObjects.Add(new NamedObjectSave { InstanceName = $"NewPlayer{newInstanceNumber++}" });
                        if (playerList.ContainedObjects.Count > 400)
                        {
                            playerList.ContainedObjects.RemoveAt(0);
                        }
                    }
                }
                catch (Exception e)
                {
                    mutatorException = e;
                }
            })
            {
                IsBackground = true
            };
            mutatorThread.Start();

            try
            {
                for (int i = 0; i < 300; i++)
                {
                    // Reproduces the reported call: adding an object to a list pushes the list's new
                    // contents to the running game as a variable change on the list's own NamedObjectSave.
                    sendingManager.PushVariableChangesToGame(new List<GlueVariableSetData>(),
                        new List<NamedObjectSave> { playerList });
                }
            }
            finally
            {
                Volatile.Write(ref stopMutating, true);
                mutatorThread.Join();
            }

            Assert.Null(mutatorException);
        }
        finally
        {
            FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = originalGlueProject;
        }
    }
}
