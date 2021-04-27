using FlatRedBall.Instructions;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPluginsCore.Wizard.ViewModels;
using OfficialPluginsCore.Wizard.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPluginsCore.Wizard.Models
{
    class WizardFormsDefinition
    {
        #region Fields/Properties

        public List<FormsData> FormsDataList { get; private set; } = new List<FormsData>();
        public WizardData ViewModel { get; private set; }

        public event Action GoToLast;

        Grid grid;

        #endregion

        #region Events

        public event Action DoneClicked;

        #endregion

        public void CreatePages()
        {
            ViewModel = new WizardData();

            #region Welcome Page
            {
                var formsData = new FormsData(ViewModel);
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
                var formsData = new FormsData(ViewModel);

                formsData.AddTitle("GameScreen");
                formsData.AddText("Most games have a GameScreen which is where the main gameplay happens. " +
                    "Adding a GameScreen enables lots of automated Glue behavior");



                formsData.AddBoolValue("Add GameScreen", nameof(ViewModel.AddGameScreen));
                formsData.AddText("We strongly recommend adding a GameScreen.", nameof(ViewModel.NoGameScreen));
                formsData.AddBoolValue("Add Tiled Map", nameof(ViewModel.AddTiledMap), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Add SolidCollision", nameof(ViewModel.AddSolidCollision), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Add CloudCollision", nameof(ViewModel.AddCloudCollision), nameof(ViewModel.AddGameScreen));



                FormsDataList.Add(formsData);
            }
            #endregion

            #region Player Entity
            {
                var formsData = new FormsData(ViewModel);
                formsData.AddTitle("Player Entity");

                formsData.AddText("A Player Entity is controlled by the player. Examples include a " +
                    "character in a platformer, a space ship in a shooter, or a car in a racing game.");

                formsData.AddBoolValue("Add Player Entity", nameof(ViewModel.AddPlayerEntity));

                formsData.AddOptions("What kind of control will the Player have?", nameof(ViewModel.PlayerControlType), nameof(ViewModel.AddPlayerEntity))
                    .Add("Top-down", GameType.Topdown)
                    .Add("Platformer", GameType.Platformer)
                    .Add("None (controls be added later)", GameType.None);

                formsData.AddBoolValue("Offset Player on Map", nameof(ViewModel.OffsetPlayerPosition), nameof(ViewModel.ShowOffsetPositionUi))
                    .Subtext = "Recommended - offsets the player so it lands on the map at the start of the game";

                formsData.AddOptions("What kind of collision will the Player have?", nameof(ViewModel.PlayerCollisionType), nameof(ViewModel.AddPlayerEntity))
                    .Add("Rectangle", CollisionType.Rectangle)
                    .Add("Circle", CollisionType.Circle)
                    .Add("None (will still be an ICollidable)", CollisionType.None);

                formsData.AddBoolValue("Add Player list to GameScreen", nameof(ViewModel.AddPlayerListToGameScreen), nameof(ViewModel.AddPlayerEntity));
                formsData.AddBoolValue("Add Player instance to list", nameof(ViewModel.AddPlayerToList), nameof(ViewModel.AddPlayerEntity));

                formsData.AddBoolValue("Add Player vs. solid collision", nameof(ViewModel.CollideAgainstSolidCollision), nameof(ViewModel.ShowPlayerVsSolidCollision));
                formsData.AddBoolValue("Add Player vs. cloud collision", nameof(ViewModel.CollideAgainstCloudCollision), nameof(ViewModel.ShowPlayerVsCloudCollision));

                formsData.AddBoolValue("Add Sprite to Player Entity", nameof(ViewModel.AddPlayerSprite), nameof(ViewModel.AddPlayerEntity));

                FormsDataList.Add(formsData);
            }
            #endregion

            #region Levels
            {
                var formsData = new FormsData(ViewModel);
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
                var formsData = new FormsData(ViewModel);
                formsData.AddTitle("UI");
                formsData.AddText("Gum can be used to create layout for UI and game HUD. FlatRedBall.Forms " +
                    "provides common UI controls like buttons, text boxes, and list boxes");

                formsData.AddBoolValue("Add Gum", nameof(ViewModel.AddGum));
                formsData.AddBoolValue("Add FlatRedBall.Forms", nameof(ViewModel.AddFlatRedBallForms));

                FormsDataList.Add(formsData);
            }
            #endregion

            #region Camera
            {
                var formsData = new FormsData(ViewModel);
                formsData.AddTitle("Camera");
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
                var formsData = new FormsData(ViewModel);

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
                        .Where(item => !string.IsNullOrEmpty(item.Url))
                        .Select(item => item.Url)
                        .ToArray();
                    ViewModel.ElementImportUrls.AddRange(toAdd);
                };

                FormsDataList.Add(formsData);
            }


            #endregion

            #region Named Objects

            {
                var formsData = new FormsData(ViewModel);

                formsData.AddTitle("Additional Objects");

                formsData.AddText("Additional object JSON can be added here. Typically this not hand-written, but pasted from another project.");

                formsData.AddMultiLineStringValue("Enter object JSON", nameof(ViewModel.NamedObjectSavesSerialized));

                FormsDataList.Add(formsData);
            }

            #endregion

            #region All Done!
            {
                var formsData = new FormsData(ViewModel);
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

        void Show(FormsData formsData)
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
