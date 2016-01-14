// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Alsing.Design
{
    public class ComponaCollectionEditor : CollectionEditor
    {
        #region EditorImplementation

        private ComponaCollectionForm Form;
        public ComponaCollectionEditor(Type t) : base(t) {}

        public IDesignerHost DesignerHost
        {
            get
            {
                var designer = (IDesignerHost) GetService(typeof (IDesignerHost));
                return designer;
            }
        }

        public void AddObject(object o)
        {
            Form.AddObject(o);
        }

        public void RemoveObject(object o)
        {
            Form.RemoveObject(o);
        }


        protected virtual CollectionEditorGui CreateGUI()
        {
            return new CollectionEditorGui();
        }

        protected override CollectionForm CreateCollectionForm()
        {
            Form = new ComponaCollectionForm(this)
                   {
                       StartPosition = FormStartPosition.CenterScreen
                   };
            return Form;
        }

        #endregion

        #region CollectionForm

        protected class ComponaCollectionForm : CollectionForm
        {
            private readonly ArrayList CreatedItems = new ArrayList();
            private readonly ComponaCollectionEditor Editor;
            private readonly CollectionEditorGui GUI;
            private readonly ArrayList RemovedItems = new ArrayList();
            private bool IsDirty;


            public ComponaCollectionForm(CollectionEditor e) : base(e)
            {
                Editor = e as ComponaCollectionEditor;

                if (Editor != null) 
                    GUI = Editor.CreateGUI();

                GUI.Visible = true;
                GUI.Dock = DockStyle.Fill;
                Controls.Add(GUI);
                AcceptButton = GUI.btnOK;
                CancelButton = GUI.btnCancel;
                Size = new Size(630, 470);
                GUI.Editor = e as ComponaCollectionEditor;

                Type[] types = NewItemTypes;
                if (types.Length > 1)
                {
                    GUI.btnDropdown.Visible = true;
                    GUI.btnDropdown.ContextMenu = new ContextMenu();
                    for (int i = 0; (i < types.Length); i ++)
                    {
                        GUI.btnDropdown.ContextMenu.MenuItems.Add(new TypeMenuItem(types[i], btnDropDownMenuItem_Click));
                    }
                }

                GUI.btnRemove.Click += btnRemove_Click;
                GUI.btnAdd.Click += btnAdd_Click;
                GUI.btnCancel.Click += btnCancel_Click;
                GUI.btnOK.Click += btnOK_Click;
                GUI.btnUp.Click += btnUp_Click;
                GUI.btnDown.Click += btnDown_Click;
                GUI.btnDropdown.Click += btnDropDown_Click;
            }


            public void RemoveObject(object o)
            {
                int index = GUI.lstMembers.Items.IndexOf(o);
                RemovedItems.Add(o);
                object i = o;
                Editor.DestroyInstance(i);
                CreatedItems.Remove(i);
                GUI.lstMembers.Items.RemoveAt(GUI.lstMembers.SelectedIndex);
                IsDirty = true;
                if (index < GUI.lstMembers.Items.Count)
                    GUI.lstMembers.SelectedIndex = index;
                else if (GUI.lstMembers.Items.Count > 0)
                    GUI.lstMembers.SelectedIndex = GUI.lstMembers.Items.Count - 1;
            }

            public void AddObject(object o)
            {
                var e = GUI.EditValue as IList;
                
                e.Add(o);

                IsDirty = true;
                GUI.lstMembers.Items.Add(o);
                CreatedItems.Add(o);
                if (o is Component)
                {
                    var cp = o as Component;
                    Editor.DesignerHost.Container.Add(cp);
                }
                var Items = new object[((uint) GUI.lstMembers.Items.Count)];
                for (int i = 0; (i < Items.Length); i++)
                {
                    Items[i] = GUI.lstMembers.Items[i];
                }
            }

            protected void btnUp_Click(object o, EventArgs e)
            {
                int i = GUI.lstMembers.SelectedIndex;
                if (i < 1)
                {
                    return;
                }

                IsDirty = true;
                int j = GUI.lstMembers.TopIndex;
                object item = GUI.lstMembers.Items[i];
                GUI.lstMembers.Items[i] = GUI.lstMembers.Items[(i - 1)];
                GUI.lstMembers.Items[(i - 1)] = item;
                if (j > 0)
                {
                    GUI.lstMembers.TopIndex = (j - 1);
                }
                GUI.lstMembers.ClearSelected();
                GUI.lstMembers.SelectedIndex = (i - 1);
            }

            protected void btnDropDown_Click(object o, EventArgs e)
            {
                GUI.btnDropdown.ContextMenu.Show(GUI.btnDropdown, new Point(0, GUI.btnDropdown.Height));
            }

            protected void btnDropDownMenuItem_Click(object o, EventArgs e)
            {
                var tmi = o as TypeMenuItem;
                if (tmi != null) 
                    CreateAndAddInstance(tmi.Type as Type);
            }

            protected void btnDown_Click(object o, EventArgs e)
            {
                int i = GUI.lstMembers.SelectedIndex;
                if (i >= GUI.lstMembers.Items.Count - 1 && i >= 0)
                {
                    return;
                }

                IsDirty = true;
                int j = GUI.lstMembers.TopIndex;
                object item = GUI.lstMembers.Items[i];

                GUI.lstMembers.Items[i] = GUI.lstMembers.Items[(i + 1)];
                GUI.lstMembers.Items[(i + 1)] = item;


                if (j < GUI.lstMembers.Items.Count - 1)
                {
                    GUI.lstMembers.TopIndex = (j + 1);
                }
                GUI.lstMembers.ClearSelected();
                GUI.lstMembers.SelectedIndex = (i + 1);
            }

            protected void btnRemove_Click(object o, EventArgs e)
            {
                int index = GUI.lstMembers.SelectedIndex;
                RemovedItems.Add(GUI.lstMembers.SelectedItem);
                object i = GUI.lstMembers.SelectedItem;
                Editor.DestroyInstance(i);
                CreatedItems.Remove(i);
                GUI.lstMembers.Items.RemoveAt(GUI.lstMembers.SelectedIndex);
                IsDirty = true;
                if (index < GUI.lstMembers.Items.Count)
                    GUI.lstMembers.SelectedIndex = index;
                else if (GUI.lstMembers.Items.Count > 0)
                    GUI.lstMembers.SelectedIndex = GUI.lstMembers.Items.Count - 1;
            }

            protected void btnAdd_Click(object o, EventArgs e)
            {
                CreateAndAddInstance(base.NewItemTypes[0]);
            }

            protected void btnCancel_Click(object o, EventArgs e)
            {
                if (IsDirty)
                {
                    foreach (object i in RemovedItems)
                    {
                        base.DestroyInstance(i);
                    }

//					object[] items = new object[((uint) GUI.lstMembers.Items.Count)];
//					for (int i = 0; i < items.Length; i++)
//					{
//						items[i] = GUI.lstMembers.Items[i];
//					}
//					base.Items = items;
                }
                ClearAll();
            }

            protected void btnOK_Click(object o, EventArgs e)
            {
                if (IsDirty)
                {
                    foreach (object i in RemovedItems)
                    {
                        base.DestroyInstance(i);
                    }

                    var items = new object[((uint) GUI.lstMembers.Items.Count)];
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = GUI.lstMembers.Items[i];
                    }
                    base.Items = items;
                }
                ClearAll();
            }

            private void ClearAll()
            {
                CreatedItems.Clear();
                RemovedItems.Clear();
                IsDirty = false;
            }


            protected override void OnEditValueChanged() {}

            protected static void OnComponentChanged(object o, ComponentChangedEventArgs e) {}

            protected override DialogResult ShowEditorDialog(IWindowsFormsEditorService edSvc)
            {
                IComponentChangeService Service = null;
                DialogResult Result;
                Result = DialogResult.Cancel;
                GUI.EditorService = edSvc;

                try
                {
                    Service = ((IComponentChangeService) Editor.Context.GetService(typeof (IComponentChangeService)));
                    if (Service != null)
                    {
                        Service.ComponentChanged += OnComponentChanged;
                    }
                    GUI.EditValue = EditValue;
                    GUI.Bind();
                    GUI.ActiveControl = GUI.lstMembers;
                    ActiveControl = GUI;

                    Result = base.ShowEditorDialog(edSvc);
                }
                finally
                {
                    if (Service != null)
                    {
                        Service.ComponentChanged -= OnComponentChanged;
                    }
                }
                return Result;
            }

            private void CreateAndAddInstance(Type type)
            {
                try
                {
                    object NewInstance = CreateInstance(type);
                    if (NewInstance != null)
                    {
                        IsDirty = true;
                        CreatedItems.Add(NewInstance);


                        GUI.lstMembers.Items.Add(NewInstance);
                        GUI.lstMembers.Invalidate();
                        GUI.lstMembers.ClearSelected();
                        GUI.lstMembers.SelectedIndex = (GUI.lstMembers.Items.Count - 1);

                        var array1 = new object[((uint) GUI.lstMembers.Items.Count)];
                        for (int i = 0; (i < array1.Length); i++)
                        {
                            array1[i] = GUI.lstMembers.Items[i];
                        }
                        Items = array1;
                    }
                    IsDirty = true;
                }
                catch (Exception x)
                {
                    base.DisplayError(x);
                }
            }
        }

        #endregion

        #region Nested type: TypeMenuItem

        public class TypeMenuItem : MenuItem
        {
            #region PUBLIC PROPERTY TYPE

            public object Type { get; set; }

            #endregion

            public TypeMenuItem(object o, EventHandler e)
            {
                Text = o.ToString();
                Type = o;
                Click += e;
            }
        }

        #endregion
    }
}