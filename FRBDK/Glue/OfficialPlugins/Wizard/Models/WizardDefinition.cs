using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace OfficialPluginsCore.Wizard.Models
{
    class WizardFormsDefinition
    {
        #region Fields/Properties

        public List<FormsData> FormsDataList { get; private set; } = new List<FormsData>();
        public WizardData ViewModel { get; private set; }

        #endregion

        public event Action DoneClicked;

        public void CreatePages()
        {
            ViewModel = new WizardData();

            {
                var formsData = new FormsData(ViewModel);
                formsData.AddTitle("Welcome");
                formsData.AddText("This wizard will help you set up a Glue project quickly. Let's get started!");
                FormsDataList.Add(formsData);
            }

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

                formsData.AddOptions("What kind of collision will the Player have?", nameof(ViewModel.PlayerCollisionType), nameof(ViewModel.AddPlayerEntity))
                    .Add("Rectangle", CollisionType.Rectangle)
                    .Add("Circle", CollisionType.Circle)
                    .Add("None (will still be an ICollidable)", CollisionType.None);

                formsData.AddBoolValue("Add Player list to GameScreen", nameof(ViewModel.AddPlayerListToGameScreen), nameof(ViewModel.AddPlayerEntity));
                formsData.AddBoolValue("Add Player instance to list", nameof(ViewModel.AddPlayerToList), nameof(ViewModel.AddPlayerEntity));

                formsData.AddBoolValue("Add Player vs. solid collision", nameof(ViewModel.CollideAgainstSolidCollision), nameof(ViewModel.AddPlayerEntity));
                formsData.AddBoolValue("Add Player vs. cloud collision", nameof(ViewModel.CollideAgainstCloudCollision), nameof(ViewModel.AddPlayerEntity));

                formsData.AddBoolValue("Add Sprite to Player Entitty", nameof(ViewModel.AddPlayerSprite), nameof(ViewModel.AddPlayerEntity));

                FormsDataList.Add(formsData);
            }

            {
                var formsData = new FormsData(ViewModel);
                formsData.AddTitle("Levels");
                formsData.AddText("Games can have multiple levels. Usually each level is a separate Tiled file.");

                formsData.AddText("Levels cannot be added because there is no game screen.", nameof(ViewModel.NoGameScreen));


                formsData.AddIntValue("Number of levels to create", nameof(ViewModel.NumberOfLevels), nameof(ViewModel.AddGameScreen));

                formsData.AddBoolValue("Include standard tileset", nameof(ViewModel.IncludStandardTilesetInLevels), nameof(ViewModel.AddGameScreen));
                formsData.AddBoolValue("Include gameplay layer", nameof(ViewModel.IncludeGameplayLayerInLevels), nameof(ViewModel.AddGameScreen));

                FormsDataList.Add(formsData);

            }

            {
                var formsData = new FormsData(ViewModel);
                formsData.AddTitle("UI");
                formsData.AddText("Gum can be used to create layout for UI and game HUD. FlatRedBall.Forms " +
                    "provides common UI controls like buttons, text boxes, and list boxes");

                formsData.AddBoolValue("Add Gum", nameof(ViewModel.AddGum));
                formsData.AddBoolValue("Add FlatRedBall.Forms", nameof(ViewModel.AddFlatRedBallForms));

                FormsDataList.Add(formsData);
            }

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

            {
                var formsData = new FormsData(ViewModel);
                formsData.AddText("All Done!");
                formsData.AddText("Click the Done button to set up your project, or click the Back button to change settings");
                FormsDataList.Add(formsData);
            }

        }
        public void Start(Grid grid)
        {
            var formsDataList = FormsDataList;
            void Show(FormsData formsData)
            {
                var index = formsDataList.IndexOf(formsData);

                var isLast =
                    index == formsDataList.Count - 1;
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
                        grid.Children.Clear();
                        if (index < formsDataList.Count - 1)
                        {
                            Show(formsDataList[index + 1]);
                        }
                        else
                        {
                            // do nothing, it's the end
                        }
                    };
                }
                formsData.BackClicked += () =>
                {
                    grid.Children.Clear();
                    if (index > 0)
                    {
                        Show(formsDataList[index - 1]);
                    }
                    else
                    {
                        // do nothing, it's the end
                    }
                };
            }
            Show(formsDataList.First());

            //formsData.Fill(StackP)
        }
    }

}
