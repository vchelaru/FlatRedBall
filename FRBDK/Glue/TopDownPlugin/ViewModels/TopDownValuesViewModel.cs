using FlatRedBall.Glue.MVVM;
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
    public class TypedValue
    {
        public Type Type;
        public object Value;
    }

    public class TopDownValuesViewModel : ViewModel
    {
        #region Enums

        public enum ImmediateOrAccelerate
        {
            Immediate,
            Accelerate
        }

        public enum ChangeOrUnchange
        {
            Change,
            Unchange
        }

        #endregion

        #region Fields/Properties

        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
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
        public Visibility AccelerationValuesVisibility
        {
            get
            {
                return IsImmediate ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

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

        public ChangeOrUnchange ShouldChangeMovementDirection
        {
            get => Get<ChangeOrUnchange>();
            set => Set(value);
        }

        public bool UpdateDirectionFromVelocity
        {
            get => ShouldChangeMovementDirection == ChangeOrUnchange.Change;
            set
            {
                Set(value);
                if (value)
                {
                    ShouldChangeMovementDirection = ChangeOrUnchange.Change;
                }
            }
        }

        public bool DontChangeDirectionFromVelocity
        {
            get => ShouldChangeMovementDirection == ChangeOrUnchange.Unchange;
            set
            {
                Set(value);
                if (value)
                {
                    ShouldChangeMovementDirection = ChangeOrUnchange.Unchange;
                }
            }
        }

        [XmlIgnore]
        public Dictionary<string, TypedValue> AdditionalProperties
        {
            get; private set;
        } = new Dictionary<string, TypedValue>();

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

        internal void SetFrom(TopDownValues values, List<Type> additionalValueTypes)
        {
            this.Name = values.Name;
            this.MaxSpeed = values.MaxSpeed;
            this.UsesAcceleration = values.UsesAcceleration;

            this.AccelerationTime = values.AccelerationTime;
            this.DecelerationTime = values.DecelerationTime;
            this.UpdateDirectionFromVelocity = values.UpdateDirectionFromVelocity;

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

            foreach(var kvp in AdditionalProperties)
            {
                toReturn.AdditionalValues[kvp.Key] = kvp.Value;
            }

            return toReturn;
        }
    }
}
