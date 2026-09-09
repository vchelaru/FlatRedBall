using System;
using System.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ExportedImplementations;

// Reproduces #1754: a corrupt settings.xml used to pop a modal error dialog and abort the rest of
// Glue startup. TryLoadSettingsFromFile is the extracted, file-path-parameterized seam that makes
// this pinnable without touching the real AppData settings file GlueSettingsSave.SettingsFileName
// points at.
public class GlueCommandsLoadSettingsTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-settings.xml");

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void TryLoadSettingsFromFile_ShouldReturnError_WhenFileIsNotValidXml()
    {
        File.WriteAllText(_tempFile, "this is not xml");

        var settingsSave = GlueCommands.TryLoadSettingsFromFile(new FilePath(_tempFile), out var error);

        settingsSave.ShouldBeNull();
        error.ShouldNotBeNull();
    }

    [Fact]
    public void TryLoadSettingsFromFile_ShouldReturnSettings_WhenFileIsValid()
    {
        var toSave = new FlatRedBall.Glue.SaveClasses.GlueSettingsSave
        {
            LastProjectFile = "SomeProject.gluj"
        };
        FileManager.XmlSerialize(toSave, _tempFile);

        var settingsSave = GlueCommands.TryLoadSettingsFromFile(new FilePath(_tempFile), out var error);

        error.ShouldBeNull();
        settingsSave.ShouldNotBeNull();
        settingsSave.LastProjectFile.ShouldBe("SomeProject.gluj");
    }
}
