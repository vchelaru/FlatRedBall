using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
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
}
