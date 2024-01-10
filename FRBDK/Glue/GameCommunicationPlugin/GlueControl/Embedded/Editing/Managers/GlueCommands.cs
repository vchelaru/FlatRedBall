{CompilerDirectives}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlueControl.Dtos;
using GlueControl.Models;
using FlatRedBall.IO;


namespace GlueControl.Managers
{
    internal class GlueCommands : GlueCommandsStateBase
    {
        #region Fields/properties

        public static GlueCommands Self { get; }

        public GluxCommands GluxCommands { get; private set; }

        public GenerateCodeCommands GenerateCodeCommands { get; private set; }

        FilePath GlueProjectFilePath;
        #endregion

        #region  Constructors

        static GlueCommands() => Self = new GlueCommands();

        public GlueCommands()
        {
            GluxCommands = new GluxCommands();
            GenerateCodeCommands = new GenerateCodeCommands();
        }

        #endregion

        public string GetAbsoluteFileName(ReferencedFileSave rfs)
        {
            if (rfs == null)
            {
                throw new ArgumentNullException("rfs", "The argument ReferencedFileSave should not be null");
            }
            var gameDirectory = GlueProjectFilePath.GetDirectoryContainingThis();
            var contentDirectory = gameDirectory + "Content/";
            return contentDirectory + rfs.Name;
        }

        public FilePath GetAbsoluteFilePath(ReferencedFileSave rfs)
        {
            return GetAbsoluteFileName(rfs);
        }

        public void PrintOutput(string output)
        {
            SendMethodCallToGame(nameof(PrintOutput), output);
        }

        public void Undo()
        {
            SendMethodCallToGame(nameof(Undo));
        }

        private Task<object> SendMethodCallToGame(string caller = null, params object[] parameters)
        {
            return base.SendMethodCallToGame(new GlueCommandDto(), caller, parameters);
        }

        // It's async in Glue but we just do non-async here
        //public void LoadProjectAsync(string fileName)
        public void LoadProject(string fileName)
        {
            GlueProjectFilePath = fileName;
            ObjectFinder.Self.GlueProject = GlueProjectSaveExtensions.Load(fileName);
        }
    }
}
