using FlatRedBall.Glue.MVVM;
using GlueCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using TopDownPlugin.Models;

namespace TopDownPlugin.ViewModels
{
    #region Class

    public class TypedValue
    {
        public Type Type;
        public object Value;
    }

    #endregion

    public class TopDownValuesViewModel : ViewModel
    {
        #region Enums

        public enum ImmediateOrAccelerate
        {
            Immediate,
            Accelerate
        }

        public enum VelocityChangeMode
        {
            UpdateFromVelocity,
            UpdateFromInput,
            DoNotUpdate
        }

        #endregion

        #region Fields/Properties

        public string Name
        {
            get => Get<string>();
            set => Set(value); 
        }

        public float MaxSpeed
        {
            get => Get<float>();
            set => Set(value); 
        }

        public ImmediateOrAccelerate MovementMode
        {
            get => Get<ImmediateOrAccelerate>();
            set => Set(value);
        }

        [DependsOn(nameof(MovementMode))]
        public bool IsImmediate
        {
            get => MovementMode == ImmediateOrAccelerate.Immediate;
            set
            {
                Set(value);
                if(value)
                {
                    MovementMode = ImmediateOrAccelerate.Immediate;
                }
            }
        }

        [DependsOn(nameof(MovementMode))]
        public bool UsesAcceleration
        {
            get => MovementMode == ImmediateOrAccelerate.Accelerate;
            set
            {
                Set(value);
                if (value)
                {
                    MovementMode = ImmediateOrAccelerate.Accelerate;
                }
            }
        }

        [DependsOn(nameof(IsImmediate))]
        [DependsOn(nameof(UsesAcceleration))]
        public Visibility AccelerationValuesVisibility => IsImmediate ? Visibility.Collapsed
                    : Visibility.Visible;

        public float AccelerationTime
        {
            get => Get<float>();
            set => Set(value); 
        }

        public float DecelerationTime
        {
            get => Get<float>(); 
            set => Set(value); 
        }

        public VelocityChangeMode ShouldChangeMovementDirection
        {
            get => Get<VelocityChangeMode>();
            set => Set(value);
        }

        [DependsOn(nameof(ShouldChangeMovementDirection))]
        public bool UpdateDirectionFromVelocity
        {
            get => ShouldChangeMovementDirection == VelocityChangeMode.UpdateFromVelocity;
            set
            {
                if (value)
                {
                    ShouldChangeMovementDirection = VelocityChangeMode.UpdateFromVelocity;
                }
            }
        }

        [DependsOn(nameof(ShouldChangeMovementDirection))]
        public bool UpdateDirectionFromInput
        {
            get => ShouldChangeMovementDirection == VelocityChangeMode.UpdateFromInput;
            set
            {
                if(value)
                {
                    ShouldChangeMovementDirection = VelocityChangeMode.UpdateFromInput;
                }
            }
        }

        [DependsOn(nameof(ShouldChangeMovementDirection))]
        public bool DontChangeDirection
        {
            get => ShouldChangeMovementDirection == VelocityChangeMode.DoNotUpdate;
            set
            {
                if (value)
                {
                    ShouldChangeMovementDirection = VelocityChangeMode.DoNotUpdate;
                }
            }
        }

        [XmlIgnore]
        public Dictionary<string, TypedValue> AdditionalProperties
        {
            get; private set;
        } = new Dictionary<string, TypedValue>();

        [XmlIgnore]
        public TopDownValues BackingData
        {
            get;
            private set;
        }

        public bool IsCustomDecelerationChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsCustomDecelerationChecked))]
        public bool IsCustomDecelerationValueEnabled => IsCustomDecelerationChecked;

        public float CustomDecelerationValue
        {
            get => Get<float>();
            set => Set(value);
        }

        public InheritOrOverwrite InheritOrOverwrite
        {
            get => Get<InheritOrOverwrite>();
            set => Set(value);
        }

        [DependsOn(nameof(InheritOrOverwrite))]
        public bool InheritMovementValues
        {
            get => InheritOrOverwrite == InheritOrOverwrite.Inherit;
            set
            {
                Set(value);
                if(value)
                {
                    InheritOrOverwrite = InheritOrOverwrite.Inherit;
                }
            }
        }

        [DependsOn(nameof(InheritOrOverwrite))]
        public bool OverwriteMovementValues
        {
            get => InheritOrOverwrite == InheritOrOverwrite.Overwrite;
            set
            {
                Set(value);
                if(value)
                {
                    InheritOrOverwrite = InheritOrOverwrite.Overwrite;
                }
            }
        }

        public bool IsInDerivedTopDownEntity
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsInDerivedTopDownEntity))]
        public Visibility InheritBoxVisibility => IsInDerivedTopDownEntity.ToVisibility();

        [DependsOn(nameof(IsInDerivedTopDownEntity))]
        public Visibility DeleteButtonVisibility => (IsInDerivedTopDownEntity == false).ToVisibility();

        [DependsOn(nameof(InheritOrOverwrite))]
        [DependsOn(nameof(IsInDerivedTopDownEntity))]
        public bool IsEditable => InheritOrOverwrite == InheritOrOverwrite.Overwrite || IsInDerivedTopDownEntity == false;

        [DependsOn(nameof(IsInDerivedTopDownEntity))]
        public bool IsNameEditable => IsInDerivedTopDownEntity == false;

        #endregion

        public TopDownValuesViewModel Clone()
        {
            //return (TopDownValuesViewModel)this.MemberwiseClone();

            string serialized = null;
            FlatRedBall.IO.FileManager.XmlSerialize(this, out serialized);

            var newCopy = FlatRedBall.IO.FileManager
                .XmlDeserializeFromString<TopDownValuesViewModel>(serialized);

            newCopy.AdditionalProperties = new Dictionary<string, TypedValue>();
            newCopy.AdditionalProperties.Clear();

            foreach(var kvp in this.AdditionalProperties)
            {
                newCopy.AdditionalProperties[kvp.Key] = kvp.Value;
            }

            return newCopy;
        }

        internal void SetFrom(TopDownValues values, List<Type> additionalValueTypes, bool isInDerivedTopDown)
        {
            this.Name = values.Name;
            this.MaxSpeed = values.MaxSpeed;
            this.UsesAcceleration = values.UsesAcceleration;

            this.AccelerationTime = values.AccelerationTime;
            this.DecelerationTime = values.DecelerationTime;
            this.UpdateDirectionFromVelocity = values.UpdateDirectionFromVelocity;
            this.UpdateDirectionFromInput = values.UpdateDirectionFromInput;
            this.IsCustomDecelerationChecked = values.IsUsingCustomDeceleration;
            this.CustomDecelerationValue = values.CustomDecelerationValue;
            this.InheritOrOverwrite = values.InheritOrOverwrite;

            this.IsInDerivedTopDownEntity = isInDerivedTopDown;

            this.BackingData = values;


            int index = 0;
            foreach(var kvp in values.AdditionalValues)
            {
                var typedValue = new TypedValue();
                typedValue.Value = kvp.Value;
                typedValue.Type = additionalValueTypes[index];
                AdditionalProperties.Add(kvp.Key, typedValue);
                index++;
            }
        }

        public void NotifyAdditionalPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(AdditionalProperties));
        }

        internal TopDownValues ToValues()
        {
            var toReturn = new TopDownValues();

            toReturn.Name = this.Name;
            toReturn.MaxSpeed = this.MaxSpeed;
            toReturn.AccelerationTime = this.AccelerationTime;
            toReturn.DecelerationTime = this.DecelerationTime;
            toReturn.UpdateDirectionFromVelocity = this.UpdateDirectionFromVelocity;
            toReturn.UpdateDirectionFromInput = this.UpdateDirectionFromInput;
            toReturn.UsesAcceleration = this.UsesAcceleration;
            toReturn.IsUsingCustomDeceleration = this.IsCustomDecelerationChecked;
            toReturn.CustomDecelerationValue = this.CustomDecelerationValue;
            toReturn.InheritOrOverwriteAsInt = (int)this.InheritOrOverwrite;

            foreach (var kvp in AdditionalProperties)
            {
                toReturn.AdditionalValues[kvp.Key] = kvp.Value;
            }

            return toReturn;
        }

        public override string ToString()
        {
            return Name;
        }


    }
}
