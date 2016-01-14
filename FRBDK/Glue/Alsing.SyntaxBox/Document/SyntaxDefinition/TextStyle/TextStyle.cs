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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;

namespace Alsing.SourceCode
{
    /// <summary>
    /// TextStyles are used to describe the apperance of text.
    /// </summary>
    [Editor(typeof (TextStyleUIEditor), typeof
        (UITypeEditor))]
    public class TextStyle :
        ICloneable
    {
        /// <summary>
        /// Name of the style
        /// </summary>
        public string Name;

        #region PUBLIC PROPERTY BOLD

        private bool _Bold;

        [Category("Font")]
        [Description("Gets or Sets if the style uses a BOLD font")
        ]
        public bool Bold
        {
            get { return _Bold; }
            set
            {
                _Bold = value;
                OnChange();
            }
        }

        #endregion

        #region PUBLIC PROPERTY ITALIC

        private bool _Italic;

        [Category("Font")]
        [Description(
            "Gets or Sets if the style uses an ITALIC font")]
        public bool
            Italic
        {
            get { return _Italic; }
            set
            {
                _Italic = value;
                OnChange();
            }
        }

        #endregion

        #region PUBLIC PROPERTY UNDERLINE

        private bool _Underline;

        [Category("Font")]
        [Description(
            "Gets or Sets if the style uses an UNDERLINED font")]
        public bool
            Underline
        {
            get { return _Underline; }
            set
            {
                _Underline = value;
                OnChange();
            }
        }

        #endregion

        #region PUBLIC PROPERTY FORECOLOR

        private Color _ForeColor = Color.Black;

        [Category("Color")]
        [Description("Gets or Sets the fore color of the style")
        ]
        public Color ForeColor
        {
            get { return _ForeColor; }
            set
            {
                _ForeColor = value;
                OnChange();
            }
        }

        #endregion

        #region PUBLIC PROPERTY BACKCOLOR

        private Color _BackColor = Color.Transparent;

        [Category("Color")]
        [Description(
            "Gets or Sets the background color of the style")]
        public Color
            BackColor
        {
            get { return _BackColor; }
            set
            {
                _BackColor = value;
                OnChange();
            }
        }

        #endregion

        /// <summary>
        /// Gets or Sets if the style uses a Bold font
        /// </summary>

        /// <summary>
        /// Gets or Sets if the style uses an Italic font
        /// </summary>

        /// <summary>
        /// Gets or Sets if the style uses an Underlined font
        /// </summary>

        /// <summary>
        /// Gets or Sets the ForeColor of the style
        /// </summary>

        /// <summary>
        /// Gets or Sets the BackColor of the style
        /// </summary>

        /// <summary>
        /// Default constructor
        /// </summary>
        public TextStyle()
        {
            ForeColor = Color.Black;
            BackColor = Color.Transparent;
        }

        /// <summary>
        /// Returns true if no color have been assigned to the backcolor
        /// </summary>
        [Browsable(false)]
        public bool Transparent
        {
            get { return (BackColor.A == 0); }
        }

        public event EventHandler Change = null;

        protected virtual void OnChange()
        {
            if (Change != null)
                Change(this, EventArgs.Empty);
        }

        public override string ToString()
        {
            if (Name == null)
                return "TextStyle";

            return Name;
        }

        #region Implementation of ICloneable

        public object Clone()
        {
            var ts = new TextStyle
                     {
                         //TODO: verify if this actually works
                         BackColor = BackColor,
                         Bold = Bold,
                         ForeColor = ForeColor,
                         Italic = Italic,
                         Underline = Underline,
                         Name = Name
                     };
            return ts;
        }

        #endregion
    }
}