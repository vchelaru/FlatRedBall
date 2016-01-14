using System;
using System.Threading;
using FlatRedBall.Glue;
using FlatRedBall.Glue.FormHelpers;

namespace PluginTestbed.LevelEditor
{
    public class LevelEditorRemotingSelectionInterfaceManager : RemotingHelper.RemotingManager<SelectionInterface>
    {
        public LevelEditorRemotingSelectionInterfaceManager()
            : base(9426)
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

            if (currentNode != null)
            {
                const bool showError = false;
                if (immediate)
                {
                    ShowElementThreadProc(showError);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(ShowElementThreadProc, showError);

                }

                SetState("", immediate);
            }

            if (EditorLogicSnapshot.CurrentState != null)
            {
                SetState(EditorLogicSnapshot.CurrentState.Name, immediate);
            }
            else if (EditorLogicSnapshot.CurrentTreeNode != null && EditorLogicSnapshot.CurrentTreeNode.IsRootCustomVariablesNode())
            {
                SetState("", immediate);
            }

            if (EditorLogicSnapshot.CurrentNamedObject == null)
            {
                if (immediate)
                {
                    HighlightElementThreadProc(false);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(HighlightElementThreadProc, false);
                }
            }
            else
            {
                if (immediate)
                {
                    HighlightElementThreadProc(false);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(HighlightElementThreadProc, false);

                }
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


        public void ShowElementThreadProc(object showError)
        {
            try
            {
                OutInterface.ShowElement(EditorLogicSnapshot.CurrentElement.Name);
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
                OutInterface.RefreshGlueProject();
            }
            catch (Exception e)
            {
                if ((bool)showError)
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
    }
}
