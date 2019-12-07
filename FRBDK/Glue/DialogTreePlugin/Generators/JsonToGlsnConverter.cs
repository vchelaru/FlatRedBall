using DialogTreePlugin.Controllers;
using DialogTreePlugin.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogTreePlugin.Generators
{
    public class JsonToGlsnConverter : Singleton<JsonToGlsnConverter>
    {
        public string currentPluginVersion;
        private const string stringKeyPrefix = "T_";
        private const string passageText = "Passage";
        private const string linkText = "Link";

        private const string rawFileType = ".json";
        private const string convertedFileType = ".glsn";
        private const string convertedFileNameEnding = "_DT";
        private const string combinedFileNameEnd = convertedFileNameEnding + convertedFileType;
        public void HandleJsonFile(ReferencedFileSave newFile, bool isGlueLoad = false)
        {
            var localizationDbEntriesToAdd = new List<string[]>();
            
            DialogTreeConverted.Rootobject convertedDialogTree = null;
            var didConvert = ConvertJsonToGlsn(newFile, localizationDbEntriesToAdd, ref convertedDialogTree);

            if (didConvert)
            {
                //Save the new .glsn file.
                var convertedFileName = GlueCommands.Self.GetAbsoluteFileName(newFile).Replace(rawFileType, combinedFileNameEnd);
                DialogTreeFileController.Self.SerializeConvertedDialogTree(convertedDialogTree, convertedFileName);

                //Update the localizationdb
                DialogTreeFileController.Self.UpdateLocalizationDb(localizationDbEntriesToAdd.ToArray(), isGlsnChange: true);


                RootObjectCodeGenerator.Self.GenerateAndSave();
            }
        }

        private void ReplaceGluxJsonReferenceWithGlsn(ReferencedFileSave newFile, string convertedFileName)
        {

            var elementName = newFile.Name.Substring(0, newFile.Name.LastIndexOf('/'));
            var element = GlueState.Self.GetElement(elementName);
            while (elementName.LastIndexOf('/') > 0 && element == null)
            {
                elementName = elementName.Substring(0, elementName.LastIndexOf('/'));
                element = GlueState.Self.GetElement(elementName);
            }
            
            
            var convertedName = newFile.Name.Replace(rawFileType, combinedFileNameEnd);

            bool alreadyExists = element.ReferencedFiles.FirstOrDefault(item => item.Name == convertedName) != null;

            if (alreadyExists == false)
            {
                GlueCommands.Self.GluxCommands.AddSingleFileTo(
                    convertedFileName,
                    FileManager.RemovePath(convertedFileName),
                    string.Empty,
                    null,
                    false,
                    null,
                    element,
                    null
                    );
            }
        }

        private bool ConvertJsonToGlsn(ReferencedFileSave newFile, List<string[]> localizationDbEntriesToAdd, ref DialogTreeConverted.Rootobject convertedDialogTree)
        {
            //Deserialize the raw dialog tree.
            var rawDialogTree = DialogTreeFileController.Self.DeserializeRawDialogTree(GlueCommands.Self.GetAbsoluteFileName(newFile));

            if (rawDialogTree != null)
            {
                //Strip the file name of the path and extenstion to use as the tree name and 
                var strippedExtenstion = FileManager.RemoveExtension(newFile.Name);
                //The root string will be for
                var rootStringKeyName = FileManager.RemovePath(strippedExtenstion);

                //Temporarily store the converted pasages in a list.
                var converedPassaged = new List<DialogTreeConverted.Passage>();

                foreach (var passage in rawDialogTree.passages)
                {
                    //Get the start of the converted passage from the passage.
                    //This preserves the tags array and pid but converted to an int.
                    var newPassage = passage.ToConvertedPassage();

                    //Generate a string key for the passage text.
                    //Se the new passage's stringid.
                    //As of v3.1.0 we will preserve the story name for design.
                    //So they can quickly find the passage.
                    var newPassageStringKey = stringKeyPrefix + rawDialogTree.name + "_" + rootStringKeyName + passageText + newPassage.pid;
                    newPassage.stringid = newPassageStringKey;

                    //Add the passage to the temp list of passages.
                    converedPassaged.Add(newPassage);

                    //Add the new stringId and parsedPassageText to localizationDbToAdd
                    //If it exists, we are assuming design wants to change the db entry.
                    var newdDbEntry = DialogTreeFileController.Self.GetLocalizationDbEntryOrDefault(newPassageStringKey);
                    newdDbEntry[0] = newPassageStringKey;
                    newdDbEntry[1] = ParsePassageText(passage.text);

                    localizationDbEntriesToAdd.Add((string[])newdDbEntry.Clone());

                    if (passage.links != null)
                    {
                        var convertedLinks = new List<DialogTreeConverted.Link>();
                        int linkIndex = 0;

                        foreach (var link in passage.links)
                        {
                            //Generate a new converted link preserving pid it links too.
                            var newLink = link.ToConvertedLink();

                            //Generate stringId for the dialog text.
                            var newLinkStringKey = newPassageStringKey + linkText + linkIndex;
                            newLink.stringid = newLinkStringKey;

                            //Add the link to the temp list of links.
                            convertedLinks.Add(newLink);

                            //Add the link text to the localizationDbEntriesToAdd.
                            //If it exists, we are assuming design wants to change the db entry.
                            newdDbEntry = DialogTreeFileController.Self.GetLocalizationDbEntryOrDefault(newLinkStringKey);
                            newdDbEntry[0] = newLinkStringKey;
                            newdDbEntry[1] = link.name;

                            localizationDbEntriesToAdd.Add((string[])newdDbEntry.Clone());

                            linkIndex++;
                        }
                        newPassage.links = convertedLinks.ToArray();
                    }
                }

                convertedDialogTree = new DialogTreeConverted.Rootobject()
                {
                    name = rootStringKeyName,
                    passages = converedPassaged.ToArray(),
                    pluginversion = currentPluginVersion,
                    startnodepid = int.Parse(rawDialogTree.startnode)
                };
            }

            return rawDialogTree != null;
        }

        private string ParsePassageText(string rawPassageText)
        {

            int indexOfBracket = rawPassageText.IndexOf('[');
            int indexToUse = indexOfBracket < 0 ? rawPassageText.Length : indexOfBracket;
            var noLinkText = rawPassageText.Substring(0, indexToUse);
            var noNewLines = noLinkText.Replace("\n", string.Empty);

            return noNewLines;
        }
    }
}
