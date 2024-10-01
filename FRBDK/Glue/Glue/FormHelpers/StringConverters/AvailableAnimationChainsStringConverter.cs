using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Content;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.SpriteFrame;
using GluePropertyGridClasses.StringConverters;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Processors;
using System.Formats.Asn1;
using FlatRedBall.IO;
using AsepriteDotNet.IO;
using FlatRedBall.Glue.Content.Aseprite;

namespace FlatRedBall.Glue.GuiDisplay
{
    // Not sure why but if this is a TypeConverter, then the user can't type in custom AnimationChain names
    // We want the user to be able to do that in case an AniamtionChain file hasn't been set on whatever we're tunneling into,
    // so I'm inheriting from StringConverter.
    public class AvailableAnimationChainsStringConverter : StringConverter, IObjectsInFileConverter
    {
        GlueElement element;
        StateSave stateSave;
        NamedObjectSave referencedNos;

        string[] mAvailableChains;

        public string[] AvailableChains
        {
            get { return mAvailableChains; }
        }

        public string ContentDirectory
        {
            get
            {
                return GlueState.Self.ContentDirectory;
            }
        }

        public ReferencedFileSave ReferencedFileSave
        {
            get;
            set;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
            // The user may be 
            // setting up states
            // for Sprites which set
            // CurrentChainName, but the
            // AnimationChain is loaded at
            // runtime, so Glue doesn't know
            // which values are available.  So
            // we should allow for custom values.
            //return true;
            return false;
		}

        public AvailableAnimationChainsStringConverter(CustomVariable customVariable, StateSave stateSave = null)
        {
            GlueElement element = ObjectFinder.Self.GetVariableContainer(customVariable);

            NamedObjectSave referencedNos = element.GetNamedObjectRecursively(customVariable.SourceObject);

            Initialize(element, referencedNos, stateSave);

        }

        public AvailableAnimationChainsStringConverter(GlueElement element, NamedObjectSave namedObjectSave)
        {
            Initialize(element, namedObjectSave);
        }

        void Initialize(GlueElement element, NamedObjectSave referencedNos, StateSave stateSave = null)
        {
            this.element = element;
            this.stateSave = stateSave;
            this.referencedNos = referencedNos;


            RefreshList();


        }

        private void RefreshList()
        {
            AnimationChainListSave acls = null;
            acls = GetAnimationChainListFile(element, referencedNos, stateSave);


            if (acls == null)
            {
                mAvailableChains = new string[0];
            }
            else
            {
                var referencedFile = element.ReferencedFiles.FirstOrDefault(item => ObjectFinder.Self.MakeAbsoluteContent(item.Name) == acls.FileName);

                this.ReferencedFileSave = referencedFile;

                // +1, include empty entry
                mAvailableChains = new string[acls.AnimationChains.Count + 1];

                mAvailableChains[0] = "";

                for (int i = 0; i < acls.AnimationChains.Count; i++)
                {
                    mAvailableChains[i + 1] = acls.AnimationChains[i].Name;
                }
            }
        }

        public static AnimationChainListSave GetAnimationChainListFile(GlueElement element, NamedObjectSave referencedNos, StateSave stateSave)
        {
            AnimationChainListSave acls = null;

            if (referencedNos != null)
            {
                if (referencedNos.SourceType == SourceType.File &&
                    !string.IsNullOrEmpty(referencedNos.SourceFile) &&
                    !string.IsNullOrEmpty(referencedNos.SourceName) &&
                    referencedNos.SourceFile.EndsWith(".scnx"))
                {
                    // This is the AnimationChainListSave
                    // referenced by the file...
                    acls = GetAnimationChainListFromScnxReference(referencedNos);

                    // ... but the user may be overriding that
                    // through variables, so let's check if that's
                    // the case
                    AnimationChainListSave foundAcls = GetReferencedAclsThroughSetVariables(element, referencedNos, stateSave);
                    if (foundAcls != null)
                    {
                        acls = foundAcls;
                    }

                }
                else if (referencedNos.SourceType == SourceType.FlatRedBallType &&
                    (referencedNos.SourceClassType == "Sprite" || referencedNos.SourceClassType == "SpriteFrame" ||
                     referencedNos.SourceClassType == "FlatRedBall.Sprite" || referencedNos.SourceClassType == "FlatRedBall.ManagedSpriteGroups.SpriteFrame"
                    ))
                {
                    AnimationChainListSave foundAcls = GetReferencedAclsThroughSetVariables(element, referencedNos, stateSave);

                    acls = foundAcls;


                }
            }
            return acls;
        }

        public static AnimationChainListSave GetReferencedAclsThroughSetVariables(GlueElement element, NamedObjectSave referencedNos, StateSave stateSave)
        {
            AnimationChainListSave foundAcls = null;

            // Give states the priority
            if(stateSave != null)
            {
                foreach(var item in stateSave.InstructionSaves)
                {
                    var customVariable = element.CustomVariables.FirstOrDefault(variable => variable.Name == item.Member);

                    if(customVariable != null && customVariable.SourceObject == referencedNos.InstanceName && customVariable.SourceObjectProperty == "AnimationChains")
                    {
                        string value = (string)item.Value;

                        foundAcls = LoadAnimationChainListSave(element, value);
                    }
                }

            }

            if (foundAcls == null)
            {
                // Does this have a set AnimationChainList?
                foreach (CustomVariable customVariable in element.CustomVariables)
                {
                    if (customVariable.SourceObject == referencedNos.InstanceName && customVariable.SourceObjectProperty == "AnimationChains")
                    {
                        string value = (string)customVariable.DefaultValue;

                        foundAcls = LoadAnimationChainListSave(element, value);

                    }
                }
            }

            // If the acls is null that means that it hasn't been set by custom variables, but the NOS itself may have a value set right on the Sprite for the current AnimationChain
            if (foundAcls == null)
            {
                var instruction = referencedNos.GetCustomVariable("AnimationChains");
                if (instruction != null)
                {
                    foundAcls = LoadAnimationChainListSave(element, (string)instruction.Value);
                }
            }


            return foundAcls;
        }

        private static AnimationChainListSave LoadAnimationChainListSave(GlueElement element, string rfsName)
        {
            AnimationChainListSave acls = null;

            ReferencedFileSave rfs = element.GetReferencedFileSaveByInstanceNameRecursively(rfsName);

            if (rfs != null)
            {
                FilePath filePath = GlueState.Self.ContentDirectory + rfs.Name;

                if (filePath.Exists())
                {
                    var extension = filePath.Extension;
                    if(extension == "aseprite")
                    {
                        acls = AsepriteAnimationChainLoader.ToAnimationChainListSave(filePath);
                    }
                    else
                    {
                        acls = AnimationChainListSave.FromFile(filePath.FullPath);
                    }
                }
            }
            return acls;
        }

        private static AnimationChainListSave GetAnimationChainListFromScnxReference(NamedObjectSave referencedNos)
        {
            string sourceFileName = GlueState.Self.ContentDirectory + referencedNos.SourceFile;

            string sourceFileDirectory = FlatRedBall.IO.FileManager.GetDirectory(sourceFileName);
            
            AnimationChainListSave acls = null;

            SpriteEditorScene ses;
            if (System.IO.File.Exists(sourceFileName))
            {
                ses = SpriteEditorScene.FromFile(sourceFileName);
                string truncatedName = referencedNos.SourceName.Substring(0, referencedNos.SourceName.LastIndexOf('(') - 1);



                SpriteSave spriteSave = ses.FindSpriteByName(truncatedName);

                if (spriteSave != null && !string.IsNullOrEmpty(spriteSave.AnimationChainsFile))
                {
                    acls = AnimationChainListSave.FromFile(
                        sourceFileDirectory + spriteSave.AnimationChainsFile);
                }

                if (acls == null)
                {
                    SpriteFrameSave sfs = ses.FindSpriteFrameSaveByName(truncatedName);

                    if (sfs != null)
                    {
                        acls = AnimationChainListSave.FromFile(
                        sourceFileDirectory + sfs.ParentSprite.AnimationChainsFile);
                    }

                }
            }




            return acls;
        }

		List<string> stringToReturn = new List<string>();
		public override StandardValuesCollection
					 GetStandardValues(ITypeDescriptorContext context)
		{
            // We can't cache this because the same object may reuse its type converter when properties change on the property grid
            RefreshList();
            StandardValuesCollection svc = new StandardValuesCollection(mAvailableChains);

			return svc;
		}






    }
}
