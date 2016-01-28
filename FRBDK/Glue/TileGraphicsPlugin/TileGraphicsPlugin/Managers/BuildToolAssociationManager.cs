using EditorObjects.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileGraphicsPlugin.Managers
{
    public class BuildToolAssociationManager : FlatRedBall.Glue.Managers.Singleton<BuildToolAssociationManager>
    {
        public BuildToolAssociation TmxToScnx
        {
            get
            {
                BuildToolAssociation toReturn = new BuildToolAssociation();

                toReturn.BuildTool = "Libraries/Tmx/TmxToScnx.exe";
                toReturn.SourceFileType = "tmx";
                toReturn.DestinationFileType = "scnx";

                return toReturn;
            }
        }

        public BuildToolAssociation TmxToNntx
        {
            get
            {
                BuildToolAssociation toReturn = new BuildToolAssociation();

                toReturn.BuildTool = "Libraries/Tmx/TmxToNntx.exe";
                toReturn.SourceFileType = "tmx";
                toReturn.DestinationFileType = "nntx";

                return toReturn;
            }
        }




        public BuildToolAssociation TmxToCsv
        {
            get
            {
                BuildToolAssociation toReturn = new BuildToolAssociation();

                toReturn.BuildTool = "Libraries/Tmx/TmxToCSV.exe";
                toReturn.SourceFileType = "tmx";
                toReturn.DestinationFileType = "csv";

                return toReturn;
            }
        }

        public BuildToolAssociation TmxToShcx
        {
            get
            {
                BuildToolAssociation toReturn = new BuildToolAssociation();

                toReturn.BuildTool = "Libraries/Tmx/TmxToShcx.exe";
                toReturn.SourceFileType = "tmx";
                toReturn.DestinationFileType = "shcx";

                return toReturn;
            }
        }

        public BuildToolAssociation TmxToTilb
        {
            get
            {
                BuildToolAssociation toReturn = new BuildToolAssociation();

                toReturn.BuildTool = "Libraries/Tmx/TmxToScnx.exe";
                toReturn.SourceFileType = "tmx";
                toReturn.DestinationFileType = "tilb";

                return toReturn;
            }
        }


        public void UpdateBuildToolAssociations()
        {
            // new TMX lib loads directly from TMX, no build tools

            //BuildToolAssociationAdder adder = new BuildToolAssociationAdder();
            //adder.AddIfNecessary(TmxToScnx);
            //adder.AddIfNecessary(TmxToCsv);
            //adder.AddIfNecessary(TmxToShcx);
            //adder.AddIfNecessary(TmxToTilb);
            //adder.AddIfNecessary(TmxToNntx);
        }
    }
}
