using FlatRedBall.Instructions;
using Newtonsoft.Json;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Views;
using OfficialPluginsCore.Wizard.ViewModels;
using OfficialPluginsCore.Wizard.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ToolsUtilities;

namespace OfficialPluginsCore.Wizard.Models
{
    class WizardFormsDefinition
    {
        #region Fields/Properties

        public List<WizardPage> FormsDataList { get; private set; } = new List<WizardPage>();
        public WizardViewModel ViewModel { get; private set; }

        Grid grid;

        #endregion

        #region Events

        public event Action DoneClicked;

        #endregion

        public void CreatePages()
        {
            ViewModel = new WizardViewModel();
            ViewModel.ApplyDefaults();

            {
                var formsData = new WizardPage(ViewModel);

                var page = new NewWizardWelcomePage();
                page.PlatformerClicked += () =>
                {
                    PlatformerSetupLogic.SetupForDefaultPlatformer(ViewModel);
                    DoneClicked();
                };
                page.TopDownClicked += () =>
                {
                    ViewModel.PlayerControlType = GameType.Topdown;
                    ViewModel.AddCloudCollision = false;
                    DoneClicked();
                };
                page.FormsClicked += () =>
                {
                    ViewModel.AddGameScreen = false;
                    ViewModel.AddPlayerEntity = false;
                    ViewModel.CreateLevels = false;
                    ViewModel.AddGum = true;
                    ViewModel.AddFlatRedBallForms = true;
                    ViewModel.AdditionalNonGameScreens.Add("MainMenu");
                    DoneClicked();
                };

                page.CustomClicked += () =>
                {
                    formsData.CallNext();
                };

                page.JsonConfigurationClicked += () =>
                {
                    formsData.CallNext();
                };

                var item = formsData.AddView(page);
                item.StackOrFill = StackOrFill.Fill;
                formsData.HasNextButton = false;



                FormsDataList.Add(formsData);
            }


            #region Welcome Page
            {
                var formsData = new WizardPage(ViewModel);
                var page = new WizardWelcomePage();
                var welcomePageViewModel = new WizardWelcomeViewModel();
                page.DataContext = welcomePageViewModel;
                page.StartFromScratch += () => formsData.CallNext();
                page.StartWithConfiguration += () =>
                {
                    ViewModel = welcomePageViewModel.DeserializedObject;

                    // just in case the user wants to go back:
                    foreach(var innerFormsData in FormsDataList)
                    {
                        innerFormsData.ViewModel = ViewModel;
                    }

                    Show(FormsDataList.Last());
                };

                var item = formsData.AddView(page);
                item.StackOrFill = StackOrFill.Fill;
                formsData.HasNextButton = false;

                FormsDataList.Add(formsData);
            }
            #endregion

            #region Game Screen
            {
                var formsData = new WizardPage(ViewModel);

                formsData.AddTitle("GameScreen");
                formsData.AddText("Most games have a GameScreen which is where the main gameplay happens. " +
                    "Adding a GameScreen enables lots of automated Glue behavior");



                formsData.AddBoolValue("Add GameScreen", nameof(ViewModel.AddGameScreen));
                formsData.AddText("We strongly recommend adding a GameScreen.", nameof(ViewModel.NoGameScreen));
                formsData.AddBoolValue("Add Tiled Map", nameof(ViewModel.AddTiledMap), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Add SolidCollision", nameof(ViewModel.AddSolidCollision), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Add CloudCollision", nameof(ViewModel.AddCloudCollision), nameof(ViewModel.AddGameScreen));

                formsData.AddBoolValue("Add HUD Layer", nameof(ViewModel.AddHudLayer), nameof(ViewModel.AddGameScreen));


                FormsDataList.Add(formsData);
            }
            #endregion

            #region Player Entity
            {
                var formsData = new WizardPage(ViewModel);
                formsData.AddTitle("Player Entity");

                formsData.AddText("A Player Entity is controlled by the player. Examples include a " +
                    "character in a platformer, a space ship in a shooter, or a car in a racing game.");

                formsData.AddBoolValue("Add Player Entity", nameof(ViewModel.AddPlayerEntity));

                formsData.AddOptions("How would you like to create the player?", nameof(ViewModel.PlayerCreationType), nameof(ViewModel.AddPlayerEntity))
                    .Add("Select Options", PlayerCreationType.SelectOptions)
                    .Add("Import Entity", PlayerCreationType.ImportEntity);


                var grid = new Grid();
                grid.SetBinding(Grid.VisibilityProperty, nameof(ViewModel.PlayerEntityImportUiVisibility));
                var elementImportItem = new ElementImportItem();
                var elementImportViewModel = new ElementImportItemViewModel();
                elementImportViewModel.HintText = "Enter Player URL or File Name";
                elementImportViewModel.SupportsLocalFile = true;
                elementImportItem.DataContext = elementImportViewModel;
                grid.Children.Add(elementImportItem);
                formsData.AddView(grid);

                formsData.AddOptions("What kind of control will the Player have?", nameof(ViewModel.PlayerControlType), nameof(ViewModel.IsPlayerCreationSelectingOptions))
                    .Add("Top-down", GameType.Topdown)
                    .Add("Platformer", GameType.Platformer)
                    .Add("None (controls be added later)", GameType.None);


                formsData.AddOptions("What kind of collision will the Player have?", nameof(ViewModel.PlayerCollisionType), nameof(ViewModel.IsPlayerCreationSelectingOptions))
                    .Add("Rectangle", CollisionType.Rectangle)
                    .Add("Circle", CollisionType.Circle)
                    .Add("None (will still be an ICollidable)", CollisionType.None);

                formsData.AddBoolValue("Add Sprite to Player Entity", nameof(ViewModel.AddPlayerSprite), nameof(ViewModel.IsPlayerCreationSelectingOptions));

                formsData.AddBoolValue("Make Player IDamageable", nameof(ViewModel.IsPlayerDamageableChecked), nameof(ViewModel.IsPlayerCreationSelectingOptions));

                formsData.AddBoolValue("Add Platformer Animations", nameof(ViewModel.AddPlayerSpritePlatformerAnimations),
                    nameof(ViewModel.ShowAddPlayerSpritePlatformerAnimations));

                formsData.AddBoolValue("Add Platformer Animation Controller", nameof(ViewModel.AddPlatformerAnimationController),
                    nameof(ViewModel.ShowAddPlatformAnimatorController));

                formsData.AddTitle("Player Instance in GameScreen", nameof(ViewModel.AddPlayerEntity));

                formsData.AddBoolValue("Add Player list to GameScreen", nameof(ViewModel.AddPlayerListToGameScreen), nameof(ViewModel.AddPlayerEntity));
                formsData.AddBoolValue("Add Player instance to list", nameof(ViewModel.AddPlayerToList), nameof(ViewModel.AddPlayerEntity));

                formsData.AddBoolValue("Add Player vs. solid collision", nameof(ViewModel.CollideAgainstSolidCollision), nameof(ViewModel.ShowPlayerVsSolidCollision));
                formsData.AddBoolValue("Add Player vs. cloud collision", nameof(ViewModel.CollideAgainstCloudCollision), nameof(ViewModel.ShowPlayerVsCloudCollision));

                formsData.AddBoolValue("Offset Player Instance", nameof(ViewModel.OffsetPlayerPosition), nameof(ViewModel.ShowOffsetPositionUi))
                    .Subtext = "Recommended - offsets the player so it lands on the map at the start of the game";

                formsData.Validate = () =>
                {
                    if(ViewModel.AddPlayerEntity && ViewModel.PlayerCreationType == PlayerCreationType.ImportEntity)
                    {
                        if(elementImportViewModel.UrlStatus == UrlStatus.Failed)
                        {
                            return GeneralResponse.UnsuccessfulWith($"The Player Entity could not be imported from {elementImportViewModel.UrlOrLocalFile}");
                        }
                        else if(elementImportViewModel.UrlStatus == UrlStatus.Unknown)
                        {
                            if(string.IsNullOrWhiteSpace( elementImportViewModel.UrlOrLocalFile))
                            {
                                return GeneralResponse.UnsuccessfulWith($"Player URL must be specified");
                            }
                            else
                            {
                                return GeneralResponse.UnsuccessfulWith($"Still determining whether Player Entity can be imported");
                            }
                        }
                    }
                    return null;
                };

                formsData.NextClicked += () =>
                {
                    ViewModel.PlayerEntityImportUrlOrFile = elementImportViewModel.UrlOrLocalFile;
                };

                formsData.Shown += () =>
                {
                    elementImportViewModel.UrlOrLocalFile = ViewModel.PlayerEntityImportUrlOrFile;
                };

                FormsDataList.Add(formsData);
            }
            #endregion

            #region Levels
            {
                var formsData = new WizardPage(ViewModel);
                formsData.AddTitle("Levels");
                formsData.AddText("Games can have multiple levels. Usually each level is a separate Tiled file.");

                formsData.AddText("Levels cannot be added because there is no game screen.", nameof(ViewModel.NoGameScreen));


                formsData.AddIntValue("Number of levels to create", nameof(ViewModel.NumberOfLevels), nameof(ViewModel.AddGameScreen));

                formsData.AddBoolValue("Include standard tileset", nameof(ViewModel.IncludStandardTilesetInLevels), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Include gameplay layer", nameof(ViewModel.IncludeGameplayLayerInLevels), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Add Border Collision", nameof(ViewModel.IncludeCollisionBorderInLevels), nameof(ViewModel.ShowBorderCollisionCheckBox));

                FormsDataList.Add(formsData);

            }
            #endregion

            #region UI
            {
                var formsData = new WizardPage(ViewModel);
                formsData.AddTitle("UI");
                formsData.AddText("Gum can be used to create layout for UI and game HUD. FlatRedBall.Forms " +
                    "provides common UI controls like buttons, text boxes, and list boxes");

                formsData.AddBoolValue("Add Gum", nameof(ViewModel.AddGum));
                formsData.AddBoolValue("Add FlatRedBall.Forms", nameof(ViewModel.AddFlatRedBallForms));
                var moveToHudLayer = formsData.AddBoolValue("Move GameScreenGum on GameScreen HudLayer", nameof(ViewModel.AddGameScreenGumToHudLayer), nameof(ViewModel.IsAddGumScreenToLayerVisible));
                moveToHudLayer.Subtext = "This option requires a GumScreen object to GameScreen. The wizard will add this object if selected.";

                FormsDataList.Add(formsData);
            }
            #endregion

            #region Camera
            {
                var formsData = new WizardPage(ViewModel);
                formsData.AddTitle("Camera");

                var options = formsData.AddOptions("Game Resolution", nameof(ViewModel.SelectedCameraResolution));
                options.Add("256x224 (8:7)",    CameraResolution._256x224);
                options.Add("360x240 (3:2)",    CameraResolution._360x240);
                options.Add("480x360 (4:3)",    CameraResolution._480x360);
                options.Add("640x480 (4:3)",    CameraResolution._640x480);
                options.Add("800x600 (4:3)",    CameraResolution._800x600);
                options.Add("1024x768 (4:3)",   CameraResolution._1024x768);
                options.Add("1920x1080 (16:9)", CameraResolution._1920x1080);

                formsData.AddIntValue("Game Scale%", nameof(ViewModel.ScalePercent));

                formsData.AddText("The Camera Controller Entity can simplify code for following a player and staying within the bounds of a map.");

                formsData.AddText("A Camera Controller Entity instance cannot be added because there is no game screen.", nameof(ViewModel.NoGameScreen));

                formsData.AddBoolValue("Add Camera Controller", nameof(ViewModel.AddCameraController), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Follow Players with Camera", nameof(ViewModel.FollowPlayersWithCamera), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Keep Camera in Map bounds", nameof(ViewModel.KeepCameraInMap), nameof(ViewModel.AddGameScreen));

                FormsDataList.Add(formsData);
            }
            #endregion

            #region Additional Imports

            {
                // todo - apply from main view model
                var formsData = new WizardPage(ViewModel);

                formsData.AddTitle("Download/Import Screens and Entities");

                formsData.AddText("Additional Screens and Entities can be imported in your project.");

                formsData.AddText("Enter the URLs for the screens and entities to import:");

                var view = new ElementImportView();

                var viewModel = new ElementImportViewModel();

                // start with an empty one:
                viewModel.Items.Add(new ElementImportItemViewModel());

                //var welcomePageViewModel = new WizardWelcomeViewModel();
                view.DataContext = viewModel;

                var item = formsData.AddView(view);
                item.StackOrFill = StackOrFill.Stack;

                viewModel.PropertyChanged += (sender, args) =>
                {
                    if(args.PropertyName == nameof(viewModel.IsValid))
                    {
                        formsData.IsNextButtonEnabled = viewModel.IsValid;
                    }
                };

                formsData.NextClicked += () =>
                {
                    ViewModel.ElementImportUrls.Clear();
                    var toAdd = viewModel.Items
                        .Where(item => !string.IsNullOrEmpty(item.UrlOrLocalFile))
                        .Select(item => item.UrlOrLocalFile)
                        .ToArray();
                    ViewModel.ElementImportUrls.AddRange(toAdd);
                };

                FormsDataList.Add(formsData);
            }


            #endregion

            #region Named Objects

            {
                var formsData = new WizardPage(ViewModel);

                formsData.AddTitle("Additional Objects");

                formsData.AddText("Additional object JSON can be added here. Typically this not hand-written, but pasted from another project.");

                formsData.AddMultiLineStringValue("Enter object JSON", nameof(ViewModel.NamedObjectSavesSerialized));

                FormsDataList.Add(formsData);
            }

            #endregion

            #region All Done!
            {
                var formsData = new WizardPage(ViewModel);
                formsData.AddText("All Done!");
                formsData.AddText("Click the Done button to set up your project, or click the Back button to change settings");
                formsData.AddAction("Copy Wizard Configuration to Clipboard", HandleCopyWizardSettings);
                FormsDataList.Add(formsData);
            }

            #endregion

        }

        private void HandleCopyWizardSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;

            var converted = JsonConvert.SerializeObject(ViewModel, settings);
            Clipboard.SetText(converted);

            // toast?
        }

        public void Start(Grid grid)
        {
            this.grid = grid;
            var formsDataList = FormsDataList;
            Show(formsDataList.First());

            //formsData.Fill(StackP)
        }

        void Show(WizardPage formsData)
        {
            var index = FormsDataList.IndexOf(formsData);

            var isLast =
                index == FormsDataList.Count - 1;
            grid.Children.Clear();
            formsData.Fill(grid, 
                showBack: index > 0,
                isNextButtonDone:isLast);

            if(isLast)
            {
                formsData.NextClicked += () =>
                {
                    DoneClicked();
                };
            }
            else
            {
                formsData.NextClicked += () =>
                {
                    if (index < FormsDataList.Count - 1)
                    {
                        Show(FormsDataList[index + 1]);
                    }
                    else
                    {
                        // do nothing, it's the end
                    }
                };
            }
            formsData.BackClicked += () =>
            {
                if (index > 0)
                {
                    Show(FormsDataList[index - 1]);
                }
                else
                {
                    // do nothing, it's the end
                }
            };
        }
    }

}
