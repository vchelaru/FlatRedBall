using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Moq;
using Shouldly;

namespace GlueUnitTests.Managers;

public class WildcardReferencedFileSaveLogicTests
{
    WildcardReferencedFileSaveLogic _sut;

    Mock<IGlueCommands> _glueCommands;
    Mock<IFileCommands> _fileCommands;

    public WildcardReferencedFileSaveLogicTests()
    {
        _glueCommands = new Mock<IGlueCommands>();
        _fileCommands = new Mock<IFileCommands>();

        _sut = new WildcardReferencedFileSaveLogic(
            _glueCommands.Object,
            _fileCommands.Object);
    }


    [Fact]
    public void IsMatch_ShouldReturnTrue_OnValidMatch()
    {

        _sut.IsMatch("c:/Content/Images/*.png", "c:/Content/Images/hero.png").ShouldBeTrue();
        _sut.IsMatch("d:/Content/Images/*.png", "d:/Content/Images/hero.png").ShouldBeTrue();
        _sut.IsMatch(
            "C:/Users/vchel/Downloads/Temp/SourceLink1/SourceLink1/Content/GlobalContent/*.png",
            "C:/Users/vchel/Downloads/Temp/SourceLink1/SourceLink1/Content/GlobalContent/TextureFile.png").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/*.png", "c:/Content/Images/hero.achx").ShouldBeFalse();

        _sut.IsMatch("c:/Content/Images/*.*", "c:/Content/Images/hero.png").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/*.*", "c:/Content/Images/hero.achx").ShouldBeTrue();

        _sut.IsMatch("c:/Content/Images/*", "c:/Content/Images/hero.png").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/*", "c:/Content/Images/hero.achx").ShouldBeTrue();

        _sut.IsMatch("c:/Content/Images/**/*.png", "c:/Content/Images/hero.png").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/**/*.png", "c:/Content/Images/Subfolder/hero.png").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/**/*.png", "c:/Content/Images/hero.achx").ShouldBeFalse();

        _sut.IsMatch("c:/Content/Images/**/*.*", "c:/Content/Images/hero.png").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/**/*.*", "c:/Content/Images/Subfolder/hero.png").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/**/*.*", "c:/Content/Images/hero.achx").ShouldBeTrue();
        _sut.IsMatch("c:/Content/Images/**/*.*", "c:/Content/Images/Subfolder/hero.achx").ShouldBeTrue();
    }

    /// <summary>
    /// GitHub issue: LoadWildcardReferencedFiles used to report a missing wildcard folder by calling
    /// GlueCommands.Self.PrintError from inside the Parallel.ForEach body. PrintError marshals onto the
    /// UI thread, and this method itself runs synchronously on the UI thread during project load - so a
    /// worker thread hitting that catch block deadlocked waiting for a UI thread that was blocked waiting
    /// on the very same Parallel.ForEach to finish. This pins that every error is now reported from the
    /// calling thread only after the loop has fully completed, never from a worker thread mid-loop.
    /// </summary>
    [Fact]
    public void LoadWildcardReferencedFiles_ShouldReportErrors_OnlyFromCallingThread_AfterLoopCompletes()
    {
        var callingThreadId = Environment.CurrentManagedThreadId;
        var reportingThreadIds = new ConcurrentBag<int>();

        _glueCommands
            .Setup(gc => gc.PrintError(It.IsAny<string>()))
            .Callback<string>(_ => reportingThreadIds.Add(Environment.CurrentManagedThreadId));

        var tempDirectory = Path.Combine(Path.GetTempPath(), "WildcardDeadlockTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var glujPath = new FilePath(Path.Combine(tempDirectory, "Project.gluj"));

            var glueProjectSave = new GlueProjectSave();
            // Enough wildcard entries, each pointing at a folder that does not exist under Content/, that
            // Parallel.ForEach genuinely spreads the DirectoryNotFoundException-throwing work across
            // multiple thread-pool workers rather than running it all inline on the calling thread.
            for (var i = 0; i < 20; i++)
            {
                glueProjectSave.GlobalFiles.Add(new ReferencedFileSave
                {
                    Name = $"MissingFolder{i}/**/*.png"
                });
            }

            _sut.LoadWildcardReferencedFiles(glujPath, glueProjectSave);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }

        reportingThreadIds.ShouldNotBeEmpty();
        reportingThreadIds.ShouldAllBe(id => id == callingThreadId);
    }
}
