using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FlatRedBall.Gui.PropertyGrids
{
    #region XML Docs
    /// <summary>
    /// A StructReferencePropertyGrid used by the FlatRedBall Engine when editing enums in a ListDisplayWindow.
    /// </summary>
    #endregion
    public class EnumPropertyGrid<EnumType> : StructReferencePropertyGrid<EnumType>
    {
        #region Fields

        ComboBox mComboBox = null;

        #endregion

        #region Properties

        public override EnumType SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;
                if (mComboBox != null)
                {
                    UpdateObject(null);
                    mComboBox.Text = SelectedObject.ToString();
                }

            }
        }

        #endregion

        #region Event Methods

        private void ChangeEnum(Window callingWindow)
        {
            Type enumType = typeof(EnumType);

            bool ignoreCase = false;

            SelectedObject =
                (EnumType)Enum.Parse(typeof(EnumType), mComboBox.Text, ignoreCase);

        }

        #endregion

        #region Methods

        public EnumPropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject) :
            base(cursor, windowOfObject, indexOfObject)
        {
#if !XBOX360 && !WINDOWS_PHONE
            ExcludeAllMembers();

            mComboBox = new ComboBox(mCursor);
            mComboBox.ScaleX = 15;

            this.AddWindow(mComboBox);

            mComboBox.ItemClick += ChangeEnum;

            Type type = typeof(EnumType);

            
            string[] availableValues = Enum.GetNames(type);
            Array array = Enum.GetValues(type);

            int i = 0;

            foreach (object enumValue in array)
            {
                string s = availableValues[i];
                mComboBox.AddItem(s, enumValue);

                i++;
            }
#else
            throw new NotImplementedException("This isn't implemented due to the compact framework limitations on the Enum class.  Can prob implement this using reflection if necessary.");
#endif

        }


        #endregion

    }
}
