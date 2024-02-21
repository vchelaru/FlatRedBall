using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.TypeConversions;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Data;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System.Threading.Tasks;
using FlatRedBall.Instructions.Reflection;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Forms.Integration;
using GlueFormsCore.Controls;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
using System.ServiceModel.Channels;
using Glue.Managers;

namespace Glue;

public partial class MainGlueWindow : Form
{
    #region Fields/Properties

    MainPanelControl MainWpfControl { get; set; }

    private MenuStrip mMenu;

    #endregion

    public MainGlueWindow()
    {
        // Vic says - this makes Glue use the latest MSBuild environments
        // Running on AnyCPU means we run in 64 bit and can load VS 22 64 bit libs.
        StartupManager.SetMsBuildEnvironmentVariable();

        GlueCommands.Self.DialogCommands.IsMainWindowDisposed = () => IsDisposed;
        GlueCommands.Self.DialogCommands.Win32Window = this;
        GlueCommands.Self.DialogCommands.ManagedThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        GlueCommands.Self.DialogCommands.SetTitle = (newtitle) => this.Text = newtitle;
        GlueCommands.Self.DialogCommands.CloseMainWindow = () => this.Close();

        InitializeComponent();

        CreateMenuStrip();

        this.FormClosing += this.MainGlueWindow_FormClosing;
        this.Load += (sender, args) =>
        {
            // Initialize GlueGui before using it:
            GlueGui.Initialize(mMenu);
            ErrorReporter.Initialize(this);

            StartupManager.StartUpGlue(ShareUiReferences);

            if (EditorData.GlueLayoutSettings.Maximized)
                WindowState = FormWindowState.Maximized;

            ProjectManager.mForm = this;
            if (GlueGui.ShowGui)
            {
                this.BringToFront();
            }

        };
        this.Move += HandleWindowMoved;

        // this fires continually, so instead overriding wndproc
        this.ResizeEnd += HandleResizeEnd;

        CreateMainWpfPanel();
        // so docking works
        this.Controls.Add(this.mMenu);
    }


    private static void HandleResizeEnd(object sender, EventArgs e)
    {
        PluginManager.ReactToMainWindowResizeEnd();
    }

    private static void HandleWindowMoved(object sender, EventArgs e)
    {
        PluginManager.ReactToMainWindowMoved();
    }

    private void CreateMenuStrip()
    {
        this.mMenu = new MenuStrip()
        {
            Location = new System.Drawing.Point(0, 0),
            Name = "mMenu",
            Size = new System.Drawing.Size(764, 24),
            TabIndex = 1,
            Text = Localization.Texts.MenuStripTitle
        };
        this.MainMenuStrip = this.mMenu;
    }

    private void CreateMainWpfPanel()
    {
        var wpfHost = new ElementHost();
        wpfHost.Dock = DockStyle.Fill;
        MainWpfControl = new MainPanelControl();
        wpfHost.Child = MainWpfControl;
        this.Controls.Add(wpfHost);
        this.PerformLayout();
    }

    private void ShareUiReferences(PluginCategories pluginCategories)
    {
        PluginManager.ShareMenuStripReference(mMenu, pluginCategories);

        PluginManager.PrintPreInitializeOutput();
        Application.DoEvents();
    }

    private static bool _wantsToExit = false;
    private void MainGlueWindow_FormClosing(object sender, FormClosingEventArgs e)
    {
        // If this function is async, all the awaited calls in here may get called after the window
        // is closed, and that's bad. But we can't Wait the task to finish as that would freeze the UI.
        // Therefore to fix this, we'll tell Glue to not shut down if this is the first time the user wanted
        // to shut it. Then we'll wait for all tasks to finish and then try again to close it.
        if (!_wantsToExit)
        {
            CloseAfterTasks();
            e.Cancel = true;
        }
    }

    private async void CloseAfterTasks()
    {
        ProjectManager.WantsToCloseProject = true;
        _wantsToExit = true;
        //MainPanelSplitContainer.ReactToFormClosing();

        //EditorData.GlueLayoutSettings.BottomPanelSplitterPosition = MainPanelSplitContainer.SplitterDistance;
        EditorData.GlueLayoutSettings.Maximized = this.WindowState == FormWindowState.Maximized;
        EditorData.GlueLayoutSettings.SaveSettings();

        await TaskManager.Self.WaitForAllTasksFinished();

        // ReactToCloseProject should be called before ReactToGlueClose so that plugins 
        // can react to the glux unloaded before the plugins get disabled.
        MainWpfControl.ReactToCloseProject(true, true);

        PluginManager.ReactToGlueClose();

        GlueCommands.Self.CloseGlue();
    }
}