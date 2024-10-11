using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace GlueFormsCore.ViewModels;


public enum AddScreenType
{
    LevelScreen,
    BaseLevelScreen,
    EmptyScreen
}

public enum TmxOptions
{
    NewStandardTmx,
    CopiedTmx,
    NoTmx
}

public class AddScreenViewModel : ViewModel
{
    public bool HasChangedScreenTextBox
    {
        get => Get<bool>();
        set => Set(value);
    }

    [DependsOn(nameof(ScreenName))]
    public string NameValidationMessage
    {
        get
        {
            if(!NameVerifier.IsScreenNameValid(ScreenName, null, out string whyItIsntValid))
            {
                return whyItIsntValid;
            }
            return null;
        }
    }

    [DependsOn(nameof(NameValidationMessage))]
    public Visibility ValidationVisibility => (!string.IsNullOrEmpty(NameValidationMessage)).ToVisibility();

    #region General Values

    public string ScreenName
    {
        get => Get<string>();
        set => Set(value);
    }

    /// <summary>
    /// The containing Directory relative to the project. If blank, the Screen is added to the Screens folder. If not blank,
    /// this should contain the Screens\\ prefix. For example, a proper value might be: "Screens\\Level1\\".
    /// </summary>
    public string Directory
    {
        get => Get<string>();
        set => Set(value);
    }

    public AddScreenType AddScreenType
    {
        get => Get<AddScreenType>();
        set
        {
            if (Set(value))
            {
                TryUpdateScreenName();
            }
        }
    }

    public bool HasGameScreen
    {
        get => Get<bool>();
        set => Set(value);
    }

    #endregion

    #region Level Screen

    #region Level Screen Radio

    [DependsOn(nameof(HasGameScreen))]
    public Visibility LevelScreenOptionUiVisibility => HasGameScreen.ToVisibility();

    [DependsOn(nameof(AddScreenType))]
    public bool IsLevelScreen
    {
        get => AddScreenType == AddScreenType.LevelScreen;
        set
        {
            if (value)
            {
                AddScreenType = AddScreenType.LevelScreen;
            }
        }
    }

    [DependsOn(nameof(AddScreenType))]
    [DependsOn(nameof(HasGameScreen))]
    public Visibility LevelScreenUiVisibility => (HasGameScreen && IsLevelScreen).ToVisibility();

    #endregion

    #region Screen Properties

    public bool InheritFromGameScreen
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsSetAsStartupChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    #endregion

    #region Entities

    [DependsOn(nameof(InheritFromGameScreen))]
    [DependsOn(nameof(AvailableLevels))]
    public Visibility LevelScreenEntitiesGroupVisibility => (InheritFromGameScreen && AvailableLevels.Count > 0).ToVisibility();

    public bool IsCopyEntitiesFromOtherLevelChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    [DependsOn(nameof(IsCopyEntitiesFromOtherLevelChecked))]
    public Visibility CopyEntitiesVisibility => IsCopyEntitiesFromOtherLevelChecked.ToVisibility();

    public ObservableCollection<string> AvailableLevels
    {
        get => Get<ObservableCollection<string>>();
        private set => Set(value);
    }

    public string SelectedCopyEntitiesFromLevel
    {
        get => Get<string>();
        set => Set(value);
    }

    #endregion

    #region Tiled


    public TmxOptions TmxOptions
    {
        get => Get<TmxOptions>();
        set => Set(value);
    }

    [DependsOn(nameof(TmxOptions))]
    public bool IsAddStandardTmxChecked
    {
        get => TmxOptions == TmxOptions.NewStandardTmx;
        set
        {
            if (value) TmxOptions = TmxOptions.NewStandardTmx;
        }
    }

    [DependsOn(nameof(TmxOptions))]
    public bool IsCopyTmxFromOtherLevelChecked
    {
        get => TmxOptions == TmxOptions.CopiedTmx;
        set
        {
            if (value) TmxOptions = TmxOptions.CopiedTmx;
        }
    }
    [DependsOn(nameof(TmxOptions))]
    public bool IsNoTmxFileChecked
    {
        get => TmxOptions == TmxOptions.NoTmx;
        set
        {
            if (value) TmxOptions = TmxOptions.NoTmx;
        }
    }

    [DependsOn(nameof(AvailableTmxFiles))]
    public Visibility CopyTmxFromOtherLevelVisibility => (AvailableTmxFiles.Count > 0).ToVisibility();

    public ObservableCollection<string> AvailableTmxFiles { get; private set; }

    public string SelectedTmxFile
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(TmxOptions))]
    public Visibility TmxComboBoxVisibility => (TmxOptions == TmxOptions.CopiedTmx).ToVisibility();

    [DependsOn(nameof(TmxOptions))]
    [DependsOn(nameof(SelectedTmxFile))]
    public Visibility TsxReferenceWarningVisibility
    {
        get
        {
            GlueElement container = null;
            if (TmxOptions == TmxOptions.CopiedTmx && !string.IsNullOrEmpty(SelectedTmxFile))
            {
                var rfs = ObjectFinder.Self.GetAllReferencedFiles().FirstOrDefault(item => item.Name == SelectedTmxFile);

                if(rfs != null)
                {
                    container = ObjectFinder.Self.GetElementContaining(rfs);
                }
            }

            if(container != null && container is ScreenSave screenSave)
            {
                var name = screenSave.Name;
                var otherScreenDirectory = FileManager.GetDirectory(name, RelativeType.Relative)
                    .Replace('/', '\\');

                var areDirectoriesEqual = otherScreenDirectory == this.Directory ||
                    // special case - if we're putting an entity in the Screens, then directory can
                    // be blank
                    (string.IsNullOrEmpty(Directory) && otherScreenDirectory == "Screens\\");

                if (!areDirectoriesEqual)
                {
                    return Visibility.Visible;
                }
            }


            return Visibility.Collapsed;
        }
    }


    #endregion


    #endregion

    #region Game Screen (Base Level Screen)

    [DependsOn(nameof(HasGameScreen))]
    public Visibility GameScreenOptionUiVisibility => (!HasGameScreen).ToVisibility();

    [DependsOn(nameof(HasGameScreen))]
    public bool CanAddBaseLevelScreen => !HasGameScreen;

    [DependsOn(nameof(AddScreenType))]
    public bool IsBaseLevelScreen
    {
        get => AddScreenType == AddScreenType.BaseLevelScreen;
        set
        {
            if (value)
            {
                AddScreenType = AddScreenType.BaseLevelScreen;
            }
        }
    }

    [DependsOn(nameof(AddScreenType))]
    public Visibility BaseLevelScreenUiVisibility => IsBaseLevelScreen.ToVisibility();

    public bool IsAddMapLayeredTileMapChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsAddSolidCollisionShapeCollectionChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsAddCloudCollisionShapeCollectionChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsAddListsForEntitiesChecked
    {
        get => Get<bool>();
        set => Set(value);
    }

    #endregion

    #region Empty Screen

    [DependsOn(nameof(AddScreenType))]
    public bool IsEmptyScreen
    {
        get => AddScreenType == AddScreenType.EmptyScreen;
        set
        {
            if (value)
            {
                AddScreenType = AddScreenType.EmptyScreen;
            }
        }
    }


    #endregion

    public AddScreenViewModel()
    {
        IsAddMapLayeredTileMapChecked = true;
        IsAddListsForEntitiesChecked = true;
        AvailableTmxFiles = new ObservableCollection<string>();
        AvailableLevels = new ObservableCollection<string>();
    }

    public void TryUpdateScreenName()
    {
        if (!HasChangedScreenTextBox)
        {
            string newName = null;
            switch (AddScreenType)
            {
                case AddScreenType.BaseLevelScreen:
                    newName = "GameScreen";
                    break;
                case AddScreenType.LevelScreen:
                    newName = "Level1";
                    break;

                case AddScreenType.EmptyScreen:
                    newName = "NewScreen";
                    // do we even do anything?
                    break;
            }

            var screens = GlueState.Self.CurrentGlueProject.Screens;

            while (screens.Any(item => item.GetStrippedName() == newName))
            {
                newName = StringFunctions.IncrementNumberAtEnd(newName);
            }
            ScreenName = newName;
        }
    }


}
