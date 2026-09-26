using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    /// <summary>
    /// Catches a running game up on element changes its build missed. A change made while the game is
    /// building (after the compiler read the element's generated code, before the game connected) is in
    /// neither the build nor any live push. On connect, the game reports the <see cref="GlueSourceHash"/>
    /// each element was compiled with, and every element whose current hash differs is resent.
    /// </summary>
    public static class CompiledElementSweep
    {
        public static List<GlueElement> GetElementsToResend(
            IReadOnlyDictionary<string, string> compiledHashes, IEnumerable<GlueElement> elements)
        {
            var toResend = new List<GlueElement>();
            foreach (var element in elements)
            {
                if (compiledHashes.TryGetValue(element.Name, out var compiledHash) &&
                    compiledHash != GlueSourceHash.Compute(element))
                {
                    toResend.Add(element);
                }
            }
            return toResend;
        }

        /// <summary>
        /// Asks the connected game for its compiled hashes and resends the custom variables of every element
        /// that changed since. Only runs when hot reload is available.
        /// </summary>
        public static async Task RunAsync(RefreshManager refreshManager)
        {
            var project = GlueState.Self.CurrentGlueProject;
            if (project == null)
            {
                return;
            }

            var response = await CommandSender.Self.Send<GetCompiledGlueSourceHashesResponse>(
                new GetCompiledGlueSourceHashesDto(), SendImportance.RetryOnFailure);
            if (response.Succeeded == false || response.Data == null)
            {
                return;
            }

            // Snapshot: this runs off the UI thread while the user may be adding elements.
            var allElements = project.Screens.Concat<GlueElement>(project.Entities).ToArray();
            var elements = GetElementsToResend(response.Data.Hashes, allElements);
            if (elements.Count == 0)
            {
                return;
            }

            // Deliberately no restart when hot reload is off: if a hash ever mismatches spuriously, a restart
            // would reconnect, mismatch again, and loop.
            if (refreshManager.ShouldRestartOnChange == false)
            {
                return;
            }

            foreach (var element in elements)
            {
                // A variable with no default value isn't assigned by generated code either.
                foreach (var variable in element.CustomVariables.Where(item => item.DefaultValue != null))
                {
                    await refreshManager.VariableSendingManager.PushElementVariable(element, variable);
                }
            }
        }
    }
}
