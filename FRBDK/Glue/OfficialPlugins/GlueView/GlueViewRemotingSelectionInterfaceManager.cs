using System;
using System.Threading;
using FlatRedBall.Glue;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;

namespace OfficialPlugins.GlueView
{
    public class GlueViewRemotingSelectionInterfaceManager : RemotingHelper.RemotingManager<SelectionInterface>
    {
        public GlueViewRemotingSelectionInterfaceManager()
            : base(8686)
        {
        }

        public void SetGlueProjectFile(string file, bool showError)
        {
            if (ConnectionFailed) return;

            EditorLogic.TakeSnapshot();
            ThreadPool.QueueUserWorkItem(SetGlueProjectAndSelectedObjectsThreadProc, showError);
        }


        public void UpdateSelectedNode(bool immediate)
        {
            if (ConnectionFailed) return;

            var currentNode = EditorLogicSnapshot.CurrentElementTreeNode;

            const bool showError = false;
            if (immediate)
            {
                ShowElementThreadProc(showError);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(ShowElementThreadProc, showError);

            }

                // No more state setting:
                // SetState("", immediate);

            // We used to set
            // state when the user
            // selected something in
            // Glue.  Now there is a plugin
            // in GlueView that controls the
            // current State so we just want to
            // use that.
            //if (EditorLogicSnapshot.CurrentState != null)
            //{
            //    SetState(EditorLogicSnapshot.CurrentState.Name, immediate);
            //}
            //else if (EditorLogicSnapshot.CurrentTreeNode != null && EditorLogicSnapshot.CurrentTreeNode.IsRootCustomVariablesNode())
            //{
            //    SetState("", immediate);
            //}

            if (immediate)
            {
                HighlightElementThreadProc(false);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(HighlightElementThreadProc, false);
            }
        }




        public void UnloadProject(bool shouldShowWarning)
        {
            if (ConnectionFailed) return;

            EditorLogic.TakeSnapshot();

            ThreadPool.QueueUserWorkItem(UnloadProjectThreadProc, shouldShowWarning);
        }

        public void RefreshGlueProject(bool showError)
        {
            if (ConnectionFailed) return;

            EditorLogic.TakeSnapshot();

            ThreadPool.QueueUserWorkItem(RefreshGlueProjectThreadProc, showError);
        }

        public void RefreshFile(bool showError, string fileName)
        {
            if (ConnectionFailed) return;

            EditorLogic.TakeSnapshot();

            object[] args = new object[] { showError, fileName };

            ThreadPool.QueueUserWorkItem(RefreshFileThreadProc, args);
        }


        public void RefreshCurrentElement(bool showError)
        {
            if (ConnectionFailed) return;

            EditorLogic.TakeSnapshot();

            ThreadPool.QueueUserWorkItem(RefreshCurrentElementThreadProc, showError);
        }

        public void RefreshVariables(bool showError)
        {
            if (ConnectionFailed) return;

            EditorLogic.TakeSnapshot();

            ThreadPool.QueueUserWorkItem(RefreshVariablesThreadProc, showError);
        }


        public void SetState(string state, bool isImmediate)
        {
            if (ConnectionFailed) return;

            if (isImmediate)
            {
                SetStateThreadProc(false);
            }
            else
            {
                EditorLogic.TakeSnapshot();

                ThreadPool.QueueUserWorkItem(SetStateThreadProc, false);
            }
        }

        public void SendScript(string script)
        {
            ThreadPool.QueueUserWorkItem(SendScripThreadProc, script);
        }

        public void ShowElementThreadProc(object showError)
        {
            try
            {
                var element = EditorLogicSnapshot.CurrentElement;

                OutInterface.ShowElement(element?.Name);
            }
            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e);
                }
            }
        }

        public void HighlightElementThreadProc(object showError)
        {
            try
            {
                if (EditorLogicSnapshot.CurrentNamedObject != null)
                {
                    OutInterface.HighlightElement(EditorLogicSnapshot.CurrentNamedObject.InstanceName);
                }
                else
                {
                    OutInterface.HighlightElement(null);
                }
            }

            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e);
                }
            }
        }

        public void SetGlueProjectThreadProc(object showError)
        {
            try
            {
                OutInterface.LoadGluxFile(ProjectManager.GlueProjectFileName);
            }
            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e);
                }
            }

        }

        public void SetGlueProjectAndSelectedObjectsThreadProc(object showError)
        {
            SetGlueProjectThreadProc(showError);

            // This tells us to hold on a minute while the GLUX is loading
            Thread.Sleep(200);

            UpdateSelectedNode(true);

        }

        public void UnloadProjectThreadProc(object showError)
        {
            try
            {
                OutInterface.UnloadGlux();
            }
            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e, false);
                }
            }
        }

        public void RefreshGlueProjectThreadProc(object showError)
        {
            try
            {
                if (OutInterface == null)
                {
                    PluginManager.ReceiveOutput("Error trying to send refresh glue project to GlueView - OutInterface is null");
                }
                else
                {
                    OutInterface.RefreshGlueProject();
                }
            }
            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e);
                }
            }
        }

        public void RefreshFileThreadProc(object args)
        {
            object[] array = args as object[];
            try
            {
                OutInterface.RefreshFile((string)array[1]);
            }
            catch(Exception e)
            {
                if((bool)array[0])
                {
                    OnConnectionFail(e);
                }
            }


        }

        public void RefreshCurrentElementThreadProc(object showError)
        {
            try
            {
                OutInterface.RefreshCurrentElement();
            }
            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e);
                }
            }
        }

        public void RefreshVariablesThreadProc(object showError)
        {
            try
            {
                OutInterface.RefreshVariables();

            }
            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e, false);
                }
            }

        }


        public void SetStateThreadProc(object showError)
        {
            try
            {
                string currentState = null;

                if (EditorLogicSnapshot.CurrentState != null)
                {
                    currentState = EditorLogicSnapshot.CurrentState.Name;
                }
                OutInterface.SetState(currentState);
            }
            catch (Exception e)
            {
                if ((bool)showError)
                {
                    OnConnectionFail(e);
                }
            }
        }

        public void SendScripThreadProc(object script)
        {

            try
            {

                OutInterface.ExecuteScript((string)script);
            }
            catch (Exception e)
            {
                // do nothing, this happens if GView isn't connected
                //throw e;
            }                    

        }
    }
}
