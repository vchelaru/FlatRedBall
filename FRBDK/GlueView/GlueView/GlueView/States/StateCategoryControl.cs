using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GlueView.Facades;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Content.Instructions;
using GlueViewOfficialPlugins.States;
using GlueView.SaveClasses;
using FlatRedBall.Glue.StateInterpolation;
using System.Globalization;
using System.Reflection;
using FlatRedBall.IO;
using System.IO;

namespace GlueViewOfficialPlugins.States
{


    public partial class StateCategoryControl : UserControl
    {
        #region Fields

        public const string InterpolateBetweenConst = "Interpolate Between:";

        List<StateSave> mStates = new List<StateSave>();
                
        public const string AdvancedInterpolationVariableName = "HasAdvancedInterpolations";

        Timer mTweenTimer;
        Tweener mTweener;

        /// <summary>
        /// A value between 0 and 100 (usually).  It can fall outside of these values during interpolation.
        /// </summary>
        float mInterpolationValue;

        #endregion

        #region Properties

        public string FirstInterpolatedStateName
        {
            get
            {
                string text = InterpolateFromComboBox.Text;

                return GetStateNameFromStateToString(text);
            }
        }

        private static string GetStateNameFromStateToString(string text)
        {
            int index = -1;

            if (text.Contains('('))
            {
                index = text.IndexOf('(');
            }

            if (text.Contains(' ') && index == -1 || text.IndexOf(' ') < index)
            {
                index = text.IndexOf(' ');
            }
            
            if (index != -1)
            {
                return text.Substring(0, index);
            }
            else
            {
                return text;
            }
        }


        public string SecondInterpolatedStateName
        {
            get
            {
                string text = InterpolateToComboBox.Text;

                return GetStateNameFromStateToString(text);

            }
        }
        
        public StateCategoryValues StateCategoryValues
        {
            set
            {
                PercentageTrackBar.Value = FlatRedBall.Math.MathFunctions.RoundToInt(value.InterpolationValue * 100);
                InterpolateToComboBox.Text = value.InterpolateTo;
                InterpolateFromComboBox.Text = value.InterpolateFrom;
                StateComboBox.Text = value.MainState;

                UpdatePercentageDisplay();
            }
            get
            {
                StateCategoryValues scv = new StateCategoryValues();
                scv.MainState = StateComboBox.Text;
                scv.InterpolateFrom = InterpolateFromComboBox.Text;
                scv.InterpolateTo = InterpolateToComboBox.Text;
                scv.InterpolationValue = PercentageTrackBar.Value / 100.0f;
                scv.Category = this.CategoryName;

                return scv;
            }
        }

        public InterpolationType InterpolationType
        {
            get
            {
                string asString = this.InterpolationTypeComboBox.Text;
                InterpolationType result = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;

                if (Enum.TryParse<InterpolationType>(asString, out result))
                {
                    return result;
                }
                else
                {
                    return FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear;
                }
            }
        }

        public Easing EasingType
        {
            get
            {
                string asString = this.EasingTypeComboBox.Text;
                Easing result = Easing.In;

                if (Enum.TryParse<Easing>(asString, out result))
                {
                    return result;
                }
                else
                {
                    return Easing.Out;
                }
            }
        }

        public string CategoryName
        {
            get
            {
                return CategoryNameLabel.Text;
            }
            set
            {
                CategoryNameLabel.Text = value;
            }
        }

        public bool IsDefault
        {
            get
            {
                return StateComboBox.SelectedItem == null ||
                    StateComboBox.SelectedItem as StateSave == null;
            }
        }

        public string SelectedStateName
        {
            get
            {
                StateSave stateSave = StateComboBox.SelectedItem as StateSave;

                string name = null;
                if (stateSave != null)
                {
                    name = stateSave.Name;
                }
                return name;
            }
            set
            {
                foreach (object stateSaveAsObject in this.StateComboBox.Items)
                {
                    if (stateSaveAsObject is StateSave && ((StateSave)stateSaveAsObject).Name == value)
                    {

                        StateComboBox.Text = ((StateSave)stateSaveAsObject).ToString();
                        break;
                    }
                }

            }
        }

        public float InterpolationValue
        {
            get
            {
                return mInterpolationValue;
            }
        }
        #endregion

        #region Events

        public event EventHandler ItemSelect;

        #endregion

        public StateCategoryControl()
        {
            InitializeComponent();

            FillAdvancedInterpolationComboBoxes();
            AdvancedInterpolationPanel.Visible = false;

            mTweener = new Tweener();
            mTweenTimer = new Timer();
            mTweenTimer.Interval = 33; // 30fps is a good default I think
            mTweenTimer.Tick += new EventHandler(HandleTimerTick);


        }

        void HandleTimerTick(object sender, EventArgs e)
        {
            mTweener.Update(mTweenTimer.Interval/1000.0f);
            if (mTweener.Running)
            {
                float value = 1 - mTweener.Position;


                PercentageTrackBar.Value = (int)( Math.Max(PercentageTrackBar.Minimum,   Math.Min(PercentageTrackBar.Maximum, value * 100)));
                UpdatePercentageDisplay();
                mInterpolationValue = value * 100;
                //if (ItemSelect != null)
                //{
                //    ItemSelect(sender, e);
                //}
                ShowStateFromComboBox();
            }
            else
            {
                mTweenTimer.Stop();
            }
        }

        public bool ContainsState(StateSave state)
        {
            return mStates.Contains(state);
        }

        public void Initialize(string categoryName, List<StateSave> states)
        {
            mStates.Clear();
            mStates.AddRange(states);

            CategoryName = categoryName;

            StateComboBox.Items.Clear();
            StateComboBox.Items.Add("<Default>");
            foreach (StateSave stateSave in mStates)
            {
                StateComboBox.Items.Add(stateSave);

                InterpolateFromComboBox.Items.Add(stateSave);
                InterpolateToComboBox.Items.Add(stateSave);
            }
            
            if (mStates.Count > 1)
            {
                StateComboBox.Items.Add(InterpolateBetweenConst);
            }


            this.InterpolationPanel.Visible = false;

        }

        private void FillAdvancedInterpolationComboBoxes()
        {
            string[] interpolationTypeNames =
                Enum.GetNames(typeof(FlatRedBall.Glue.StateInterpolation.InterpolationType));
            foreach (string name in interpolationTypeNames)
            {
                InterpolationTypeComboBox.Items.Add(name);
            }
            InterpolationTypeComboBox.SelectedItem = InterpolationTypeComboBox.Items[0];

            string[] easingTypeNames =
                Enum.GetNames(typeof(FlatRedBall.Glue.StateInterpolation.Easing));
            foreach (string name in easingTypeNames)
            {
                EasingTypeComboBox.Items.Add(name);
            }
            EasingTypeComboBox.SelectedIndex = (int)(Easing.Out);// out is the most common
        }



        private void PercentageTrackBar_Scroll(object sender, EventArgs e)
        {
            mInterpolationValue = this.PercentageTrackBar.Value;

            ShowStateFromComboBox();
            UpdatePercentageDisplay();
        }

        private void UpdatePercentageDisplay()
        {

            this.FromPercentageTextBox.Text = PercentageTrackBar.Value.ToString();
            this.ToPercentageTextBox.Text = (100 - PercentageTrackBar.Value).ToString();
        }

        private void StateComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ItemSelect != null)
            {
                ItemSelect(sender, e);
            }

            this.InterpolationPanel.Visible = (this.StateComboBox.Text == InterpolateBetweenConst);
            this.AdvancedInterpolationPanel.Visible = InterpolationPanel.Visible &&
                DoesCurrentElementHaveAdvancedInterpolation();

            SetInterpolationStatesIfEmpty();

            UpdateInterpolionCodeTextbox();
        }

        private bool DoesCurrentElementHaveAdvancedInterpolation()
        {
            if (GlueViewState.Self.CurrentEntitySave != null)
            {
                bool value = GlueViewState.Self.CurrentEntitySave.Properties.GetValue<bool>(AdvancedInterpolationVariableName);

                return value;
            }
            else if (GlueViewState.Self.CurrentScreenSave != null)
            {
                return GlueViewState.Self.CurrentScreenSave.Properties.GetValue<bool>(AdvancedInterpolationVariableName);
            }
            else
            {
                return false;
            }
        }

        private void SetInterpolationStatesIfEmpty()
        {
            if (string.IsNullOrEmpty(InterpolateFromComboBox.Text) && string.IsNullOrEmpty(InterpolateToComboBox.Text) &&
                mStates.Count > 0)
            {
                InterpolateFromComboBox.SelectedItem = mStates[0];
                if (mStates.Count > 1)
                {
                    InterpolateToComboBox.SelectedItem = mStates[1];
                }
                else
                {
                    InterpolateToComboBox.SelectedItem = mStates[0];
                }
            }
        }

        
        public void ShowStateFromComboBox()
        {
            StateSave stateSave = this.StateComboBox.SelectedItem as StateSave;
            string asString = this.StateComboBox.SelectedItem as string;

            if (stateSave != null)
            {
                GlueViewCommands.Self.ElementCommands.ShowState(stateSave);
            }
            else if (asString != null)
            {
                if (asString == InterpolateBetweenConst)
                {
                    // finish here
                    IElement currentElement = GlueViewState.Self.CurrentElement;

                    StateSave firstState = currentElement.GetStateRecursively(FirstInterpolatedStateName, this.CategoryName);
                    StateSave secondState = currentElement.GetStateRecursively(SecondInterpolatedStateName, this.CategoryName);
                    if (firstState != null && secondState != null)
                    {
                        float interpolationValue = 1 - (InterpolationValue / 100.0f);

                        StateSave combined = StateSaveExtensionMethodsGlueView.CreateCombinedState(firstState, secondState, interpolationValue);

                        GlueViewCommands.Self.ElementCommands.ShowState(combined);
                    }
                }
            }
            
        }


        private void FromPercentageTextBox_TextChanged(object sender, EventArgs e)
        {
            int value = PercentageTrackBar.Value;

            if (int.TryParse(FromPercentageTextBox.Text, out value))
            {
                PercentageTrackBar.Value = value;
                UpdatePercentageDisplay();
                // This refreshes all states.  We don't want to do that, we only want to refresh
                // *this* state:
                //if (ItemSelect != null)
                //{
                //    ItemSelect(sender, e);
                //}
                ShowStateFromComboBox();
            }
            else
            {

            }
        }

        private void ToPercentageTextBox_TextChanged(object sender, EventArgs e)
        {
            int value = PercentageTrackBar.Value;

            if (int.TryParse(ToPercentageTextBox.Text, out value))
            {
                PercentageTrackBar.Value = 100 - value;
                UpdatePercentageDisplay();
                // This refreshes all states.  We don't want to do that, we only want to refresh
                // *this* state:
                //if (ItemSelect != null)
                //{
                //    ItemSelect(sender, e);
                //}
                ShowStateFromComboBox();
            }
            else
            {

            }
        }

        private void StartTweenButton_Click(object sender, EventArgs e)
        {
            float timeToTake = 1;
            if (!float.TryParse(InterpolationTimeTextBox.Text, System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture, out timeToTake ))
            {
                timeToTake = 1;
            }

            mTweener.Start(0, 1, timeToTake, Tweener.GetInterpolationFunction(this.InterpolationType, this.EasingType));
            mTweenTimer.Start();
            
        }

        public void ApplyInterpolateToState(StateSave firstState, StateSave secondState, float time, InterpolationType interpolationType, Easing easing)
        {
            this.StateComboBox.Text = InterpolateBetweenConst;

            this.InterpolateFromComboBox.Text = firstState.Name;
            this.InterpolateToComboBox.Text = secondState.Name;

            this.InterpolationPanel.Visible = (this.StateComboBox.Text == InterpolateBetweenConst);
            this.AdvancedInterpolationPanel.Visible = InterpolationPanel.Visible &&
                DoesCurrentElementHaveAdvancedInterpolation();

            mTweener.Start(0, 1, time, Tweener.GetInterpolationFunction(interpolationType, easing));
            mTweenTimer.Start();
        }

        private void PercentageTrackBar_ValueChanged(object sender, EventArgs e)
        {
            mInterpolationValue = PercentageTrackBar.Value;
        }

        private void InterpolationTimeTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                this.StartTweenButton.Focus();
                this.StartTweenButton.PerformClick();
            }
        }

        private void InterpolationTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInterpolionCodeTextbox();
        }

        private void UpdateInterpolionCodeTextbox()
        {
            string enumType = "VariableState";
            if (this.CategoryName != "Uncategorized")
            {
                enumType = this.CategoryName;
            }

            string rawFirstStateName = "";
            string firstComboBoxText = InterpolateFromComboBox.Text;
            if (firstComboBoxText.Contains(" "))
            {
                rawFirstStateName = firstComboBoxText.Substring(0, firstComboBoxText.IndexOf(" "));
            }




            string rawSecondStateName = "";
            string secondComboBoxText = InterpolateToComboBox.Text;
            if (secondComboBoxText.Contains(" "))
            {
                rawSecondStateName = secondComboBoxText.Substring(0, secondComboBoxText.IndexOf(" "));
            }

            string firstState = GlueViewState.Self.CurrentElement.ClassName + "." + enumType + "." + rawFirstStateName;
            string secondState = GlueViewState.Self.CurrentElement.ClassName + "." + enumType + "." + rawSecondStateName;
            string time = InterpolationTimeTextBox.Text;
            string interpolationType = "FlatRedBall.Glue.StateInterpolation.InterpolationType." + InterpolationTypeComboBox.Text;
            string easingType = "FlatRedBall.Glue.StateInterpolation.Easing." + EasingTypeComboBox.Text;

            string value =
                string.Format("this.InterpolateToState({0}, {1}, {2}, {3}, {4});",
                firstState,
                secondState,
                time,
                interpolationType,
                easingType);

            ExampleCodeTextBox.Text = value;
        }

        private void EasingTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateInterpolionCodeTextbox();

        }

        private void InterpolationTimeTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateInterpolionCodeTextbox();

        }

        private void CopyStateCodeClick_Click(object sender, EventArgs e)
        {
            ExampleCodeTextBox.SelectAll();
            ExampleCodeTextBox.Copy();
            ExampleCodeTextBox.DeselectAll();
        }


    }

    public class StateCategoryValues
    {
        public string Category;
        public string MainState;

        public string InterpolateFrom;
        public string InterpolateTo;
        public float InterpolationValue;

    }




}
