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
using System.Drawing;
using System.Globalization;
using System.Web;
using System.Windows.Forms;
using Alsing.Drawing.GDI;
using Alsing.Windows.Forms.FormatLabel;
using System.Collections.Generic;

namespace Alsing.Windows.Forms.CoreLib
{
    /// <summary>
    /// 
    /// </summary>
    public class FormatLabelControl : BaseControl
    {
        private readonly Dictionary<string, GDIFont> _Fonts = new Dictionary<string, GDIFont>();
        private Element _ActiveElement;
        private Element[] _Elements;
        private bool _HasImageError;

        private ImageList _ImageList;
        private Color _Link_Color = Color.Blue;
        private Color _Link_Color_Hover = Color.Blue;
        private bool _Link_UnderLine;
        private bool _Link_UnderLine_Hover = true;
        private List<Row> _Rows;
        private ScrollBars _ScrollBars = 0;
        private string _Text = "format <b>label</b>";
        private bool _WordWrap = true;
        private PictureBox Filler;
        private HScrollBar hScroll;
        private VScrollBar vScroll;

        public ImageList ImageList
        {
            get { return _ImageList; }
            set
            {
                _ImageList = value;
                Invalidate();
                //this.Text = this.Text;
            }
        }

        public Color Link_Color
        {
            get { return _Link_Color; }
            set
            {
                _Link_Color = value;
                Invalidate();
            }
        }

        public Color Link_Color_Hover
        {
            get { return _Link_Color_Hover; }
            set
            {
                _Link_Color_Hover = value;
                Invalidate();
            }
        }

        public bool Link_UnderLine
        {
            get { return _Link_UnderLine; }
            set
            {
                _Link_UnderLine = value;
                Invalidate();
            }
        }

        public bool Link_UnderLine_Hover
        {
            get { return _Link_UnderLine_Hover; }
            set
            {
                _Link_UnderLine_Hover = value;
                Invalidate();
            }
        }


        public bool AutoSizeHorizontal { get; set; }

        public bool AutoSizeVertical { get; set; }

        public bool WordWrap
        {
            get { return _WordWrap; }
            set
            {
                _WordWrap = value;
                CreateRows();
                Invalidate();
            }
        }


        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), Obsolete("", false)]
        public override Image BackgroundImage
        {
            get { return base.BackgroundImage; }
            set { base.BackgroundImage = value; }
        }

        public ScrollBars ScrollBars
        {
            get { return _ScrollBars; }
            set
            {
                _ScrollBars = value;
                InitScrollbars();
            }
        }

        #region Defaults

        private Container components;

        public FormatLabelControl()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Opaque, true);
            InitializeComponent();
            Text = Text;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (GDIObject o in _Fonts.Values)
                    o.Dispose();

                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return _Text; }
            set
            {
                try
                {
                    //Text=value;
                    _Text = value;

                    CreateAll();
                    this.Invalidate();
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message);
                    System.Diagnostics.Debugger.Break();
                }
            }
        }

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Filler = new System.Windows.Forms.PictureBox();
            this.vScroll = new System.Windows.Forms.VScrollBar();
            this.hScroll = new System.Windows.Forms.HScrollBar();
            this.SuspendLayout();
            // 
            // Filler
            // 
            this.Filler.BackColor = System.Drawing.SystemColors.Control;
            this.Filler.Cursor = System.Windows.Forms.Cursors.Default;
            this.Filler.Location = new System.Drawing.Point(136, 112);
            this.Filler.Name = "Filler";
            this.Filler.Size = new System.Drawing.Size(16, 16);
            this.Filler.TabIndex = 5;
            this.Filler.TabStop = false;
            // 
            // vScroll
            // 
            this.vScroll.Cursor = System.Windows.Forms.Cursors.Default;
            this.vScroll.LargeChange = 2;
            this.vScroll.Location = new System.Drawing.Point(136, -8);
            this.vScroll.Name = "vScroll";
            this.vScroll.Size = new System.Drawing.Size(16, 112);
            this.vScroll.TabIndex = 4;
            this.vScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScroll_Scroll);
            // 
            // hScroll
            // 
            this.hScroll.Cursor = System.Windows.Forms.Cursors.Default;
            this.hScroll.LargeChange = 1;
            this.hScroll.Location = new System.Drawing.Point(0, 112);
            this.hScroll.Maximum = 600;
            this.hScroll.Name = "hScroll";
            this.hScroll.Size = new System.Drawing.Size(128, 16);
            this.hScroll.TabIndex = 3;
            // 
            // FormatLabelControl
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.AddRange(new System.Windows.Forms.Control[]
                                   {
                                       this.Filler,
                                       this.vScroll,
                                       this.hScroll
                                   });
            this.Name = "FormatLabelControl";
            this.Size = new System.Drawing.Size(160, 136);
            this.ResumeLayout(false);
        }

        private void CreateAll()
        {
            _Elements = CreateElements();
            ClearFonts();


            ApplyFormat(_Elements);
            CreateWords(_Elements);
            CreateRows();
            SetAutoSize();
        }

        private void ClearFonts()
        {
            foreach (GDIFont gf in _Fonts.Values)
            {
                gf.Dispose();
            }
            _Fonts.Clear();
        }

        #endregion

        #endregion

        public event ClickLinkEventHandler ClickLink = null;

        protected void OnClickLink(string Link)
        {
            if (ClickLink != null)
                ClickLink(this, new ClickLinkEventArgs(Link));
        }

        private void SetAutoSize()
        {
            if (AutoSizeHorizontal)
                Width = GetWidth();

            if (AutoSizeVertical)
                Height = GetHeight();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SetAutoSize();
            //base.OnPaint (e);

            if (_HasImageError)
                CreateAll();

            var bbuff = new GDISurface(Width, Height, this, true);
            Graphics g = Graphics.FromHdc(bbuff.hDC);
            try
            {
                bbuff.FontTransparent = true;

                if (BackgroundImage != null)
                {
                    g.DrawImage(BackgroundImage, 0, 0, Width, Height);
                }
                else
                {
                    bbuff.Clear(BackColor);
                }
                int x = Margin;
                int y = Margin;
                for (int i = vScroll.Value; i < _Rows.Count; i++)
                {
                    var r = (Row) _Rows[i];
                    x = Margin;
                    r.Visible = true;
                    r.Top = y;
                    if (r.RenderSeparator)
                    {
                        Color c1 = Color.FromArgb(120, 0, 0, 0);
                        Brush b1 = new SolidBrush(c1);
                        g.FillRectangle(b1, 0, y, Width, 1);

                        Color c2 = Color.FromArgb(120, 255, 255, 255);
                        Brush b2 = new SolidBrush(c2);
                        g.FillRectangle(b2, 0, y + 1, Width, 1);

                        b1.Dispose();
                        b2.Dispose();


                        //bbuff.DrawLine (this.ForeColor,new Point (0,y),new Point (this.Width,y));
                    }

                    foreach (Word w in r.Words)
                    {
                        int ypos = r.Height - w.Height + y;

                        if (w.Image != null)
                        {
                            g.DrawImage(w.Image, x, y);
                            //bbuff.FillRect (Color.Red ,x,ypos,w.Width ,w.Height);
                        }
                        else
                        {
                            GDIFont gf;
                            if (w.Element.Link != null)
                            {
                                Font f = null;

                                FontStyle fs = w.Element.Font.Style;
                                if (w.Element.Link == _ActiveElement)
                                {
                                    if (_Link_UnderLine_Hover)
                                        fs |= FontStyle.Underline;

                                    f = new Font(w.Element.Font, fs);
                                }
                                else
                                {
                                    if (_Link_UnderLine)
                                        fs |= FontStyle.Underline;

                                    f = new Font(w.Element.Font, fs);
                                }

                                gf = GetFont(f);
                            }
                            else
                            {
                                gf = GetFont(w.Element.Font);
                            }

                            bbuff.Font = gf;
                            if (w.Element.Effect != TextEffect.None)
                            {
                                bbuff.TextForeColor = w.Element.EffectColor;

                                if (w.Element.Effect == TextEffect.Outline)
                                {
                                    for (int xx = -1; xx <= 1; xx++)
                                        for (int yy = -1; yy <= 1; yy++)
                                            bbuff.DrawTabbedString(w.Text, x + xx, ypos + yy, 0, 0);
                                }
                                else if (w.Element.Effect != TextEffect.None)
                                {
                                    bbuff.DrawTabbedString(w.Text, x + 1, ypos + 1, 0, 0);
                                }
                            }


                            if (w.Element.Link != null)
                            {
                                if (w.Element.Link == _ActiveElement)
                                {
                                    bbuff.TextForeColor = Link_Color_Hover;
                                }
                                else
                                {
                                    bbuff.TextForeColor = Link_Color;
                                }
                            }
                            else
                                bbuff.TextForeColor = w.Element.ForeColor;

                            bbuff.TextBackColor = w.Element.BackColor;
                            bbuff.DrawTabbedString(w.Text, x, ypos, 0, 0);
                        }

                        w.ScreenArea.X = x;
                        w.ScreenArea.Y = ypos;
                        x += w.Width;
                    }

                    y += r.Height + r.BottomPadd;
                    if (y > Height)
                        break;
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
            bbuff.RenderToControl(0, 0);
            bbuff.Dispose();
            g.Dispose();
        }

        private Element[] CreateElements()
        {
            string text = Text.Replace("\n", "");
            text = text.Replace("\r", "");
            string[] parts = text.Split('<');
            var elements = new List<Element>();
            int i = 0;
            foreach (string part in parts)
            {
                var cmd = new Element();

                if (i == 0)
                {
                    cmd.Text = part;
                }
                else
                {
                    string[] TagTextPair = part.Split('>');
                    cmd.Tag = TagTextPair[0].ToLowerInvariant();
                    if (cmd.Tag.IndexOfAny(" \t".ToCharArray()) >= 0)
                    {
                        int ws = cmd.Tag.IndexOfAny(" \t".ToCharArray());
                        string s1 = TagTextPair[0].Substring(0, ws).ToLowerInvariant();
                        string s2 = TagTextPair[0].Substring(ws + 1);
                        cmd.Tag = s1 + " " + s2;
                    }


                    cmd.Text = TagTextPair[1];


                    if (cmd.TagName == "img")
                    {
                        var img = new Element
                                  {
                                      Tag = cmd.Tag
                                  };

                        elements.Add(img);
                        cmd.Tag = "";
                        //	Elements.Add (cmd);					
                    }
//
//					if (cmd.TagName == "hr")
//					{
//						Element hr=new Element();
//						hr.Tag = cmd.Tag;					
//						Elements.Add (hr);
//						cmd.Tag ="";
//						cmd.Text ="a";
//						//	Elements.Add (cmd);					
//					}

                    cmd.Text = cmd.Text.Replace("\t", "     ");
                    cmd.Text = cmd.Text.Replace("&#145;", "'");
                    cmd.Text = cmd.Text.Replace("&#146;", "'");


                    cmd.Text = cmd.Text.Replace(" ", ((char) 1).ToString());
                    cmd.Text = HttpUtility.HtmlDecode(cmd.Text);
                    //	cmd.Text =cmd.Text.Replace (" ","*");
                    cmd.Text = cmd.Text.Replace(((char) 1).ToString(), " ");
                }


                elements.Add(cmd);
                i++;
            }

            var res = new Element[elements.Count];
            elements.CopyTo(res);
            return res;
        }

        private string GetAttrib(string attrib, string tag)
        {
            try
            {
                if (tag.IndexOf(attrib) < 0)
                    return "";

                //tag=tag.Replace("\"","");
                tag = tag.Replace("\t", " ");

                int start = tag.IndexOf(attrib);
                int end = start + attrib.Length;
                int valuestart = tag.IndexOf("=", end);
                if (valuestart < 0)
                    return "";
                valuestart++;


                string value = tag.Substring(valuestart);

                while (value.StartsWith(" "))
                    value = value.Substring(1);

                //int pos=0;

                if (value.StartsWith("\""))
                {
                    // = "value"
                    value = value.Substring(1);
                    int valueend = value.IndexOf("\"");
                    value = value.Substring(0, valueend);
                    return value;
                }
                else
                {
                    // = value
                    int valueend = value.IndexOf(" ");
                    if (valueend < 0)
                        valueend = value.Length;
                    value = value.Substring(0, valueend);
                    return value;
                }
                //return "";
            }
            catch
            {
                return "";
            }
        }

        private void ApplyFormat(Element[] Elements)
        {
            var bold = new Stack();
            var italic = new Stack();
            var underline = new Stack();
            var forecolor = new Stack();
            var backcolor = new Stack();
            var fontsize = new Stack();
            var fontname = new Stack();
            var link = new Stack();
            var effectcolor = new Stack();
            var effect = new Stack();

            bold.Push(Font.Bold);
            italic.Push(Font.Italic);
            underline.Push(Font.Underline);
            forecolor.Push(ForeColor);
            backcolor.Push(Color.Transparent);
            fontsize.Push((int) (Font.Size*1.3));
            fontname.Push(Font.Name);
            effect.Push(TextEffect.None);
            effectcolor.Push(Color.Black);
            link.Push(null);


            foreach (Element Element in Elements)
            {
                switch (Element.TagName)
                {
                    case "b":
                        {
                            bold.Push(true);
                            break;
                        }
                    case "a":
                        {
                            //underline.Push (true);
                            //forecolor.Push (_l);
                            link.Push(Element);
                            break;
                        }
                    case "i":
                    case "em":
                        {
                            italic.Push(true);
                            break;
                        }
                    case "u":
                        {
                            underline.Push(true);
                            break;
                        }
                    case "font":
                        {
                            string _fontname = GetAttrib("face", Element.Tag);
                            string _size = GetAttrib("size", Element.Tag);
                            string _color = GetAttrib("color", Element.Tag);
                            string _effectcolor = GetAttrib("effectcolor", Element.Tag);
                            string _effect = GetAttrib("effect", Element.Tag);


                            if (_size == "")
                                fontsize.Push(fontsize.Peek());
                            else
                                fontsize.Push(int.Parse(_size));

                            if (_fontname == "")
                                fontname.Push(fontname.Peek());
                            else
                                fontname.Push(_fontname);

                            if (_color == "")
                                forecolor.Push(forecolor.Peek());
                            else
                                forecolor.Push(Color.FromName(_color));

                            if (_effectcolor == "")
                                effectcolor.Push(effectcolor.Peek());
                            else
                                effectcolor.Push(Color.FromName(_effectcolor));

                            if (_effect == "")
                                effect.Push(effect.Peek());
                            else
                                effect.Push(Enum.Parse(typeof (TextEffect), _effect, true));

                            break;
                        }
                    case "br":
                        {
                            Element.NewLine = true;
                            break;
                        }
                    case "hr":
                        {
                            Element.NewLine = true;
                            break;
                        }
                    case "h3":
                        {
                            fontsize.Push((int) (Font.Size*1.4));
                            bold.Push(true);
                            Element.NewLine = true;
                            break;
                        }
                    case "h4":
                        {
                            fontsize.Push((int) (Font.Size*1.2));
                            bold.Push(true);
                            Element.NewLine = true;
                            break;
                        }
                    case "/b":
                        {
                            bold.Pop();
                            break;
                        }
                    case "/a":
                        {
                            //underline.Pop ();
                            //forecolor.Pop ();
                            link.Pop();
                            break;
                        }
                    case "/i":
                    case "/em":
                        {
                            italic.Pop();
                            break;
                        }
                    case "/u":
                        {
                            underline.Pop();
                            break;
                        }
                    case "/font":
                        {
                            fontname.Pop();
                            fontsize.Pop();
                            forecolor.Pop();
                            effect.Pop();
                            effectcolor.Pop();
                            break;
                        }
                    case "/h3":
                        {
                            fontsize.Pop();
                            bold.Pop();
                            Element.NewLine = true;
                            break;
                        }
                    case "/h4":
                        {
                            fontsize.Pop();
                            bold.Pop();
                            Element.NewLine = true;
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }


                //---------------------------------------------------------------------
                var Bold = (bool) bold.Peek();
                var Italic = (bool) italic.Peek();
                var Underline = (bool) underline.Peek();
                var Link = (Element) link.Peek();
                var FontName = (string) fontname.Peek();
                var FontSize = (int) fontsize.Peek();
                var BackColor = (Color) backcolor.Peek();
                var ForeColor1 = (Color) forecolor.Peek();
                var Effect = (TextEffect) effect.Peek();
                var EffectColor = (Color) effectcolor.Peek();

                FontStyle fs = 0;
                if (Bold) fs |= FontStyle.Bold;
                if (Italic) fs |= FontStyle.Italic;
                if (Underline) fs |= FontStyle.Underline;

                var font = new Font(FontName, FontSize, fs);
                Element.Font = font;
                Element.BackColor = BackColor;
                Element.ForeColor = ForeColor1;
                Element.Link = Link;
                Element.Effect = Effect;
                Element.EffectColor = EffectColor;
            }
        }

        private bool IsIndex(string src)
        {
            int i;
            return int.TryParse(src,out i);            
        }

        private void CreateWords(Element[] Elements)
        {
            var bbuff = new GDISurface(1, 1, this, false);

            _HasImageError = false;
            foreach (Element Element in Elements)
            {
                if (Element.TagName == "img")
                {
                    Element.words = new Word[1];

                    Element.words[0] = new Word();

                    Image img = null;

                    try
                    {
                        string SRC = GetAttrib("img", Element.Tag).ToLowerInvariant();
                        if (IsIndex(SRC))
                        {
                            int index = int.Parse(SRC);
                            img = ImageList.Images[index];
                        }
                        else if (SRC.StartsWith("http://")) //from url
                        {}
                        else if (SRC.StartsWith("file://")) // from file
                        {
                            img = Image.FromFile(SRC.Substring(7));
                        }
                        else //from file
                        {
                            img = Image.FromFile(SRC);
                        }
                    }
                    catch
                    {
                        img = new Bitmap(20, 20);
                        _HasImageError = true;
                    }

                    Element.words[0].Image = img;


                    Element.words[0].Element = Element;


                    if (img != null)
                    {
                        Element.words[0].Height = img.Height;
                        Element.words[0].Width = img.Width;
                        Element.words[0].ScreenArea.Width = img.Width;
                        Element.words[0].ScreenArea.Height = img.Height;
                    }
                }
                else
                {
                    string[] words = Element.Text.Split(' ');
                    Element.words = new Word[words.Length];
                    int i = 0;
                    foreach (string word in words)
                    {
                        Element.words[i] = new Word();
                        string tmp ;
                        Element.words[i].Element = Element;
                        if (i == words.Length - 1)
                        {
                            Element.words[i].Text = word;
                            tmp = word;
                        }
                        else
                        {
                            Element.words[i].Text = word + " ";
                            tmp = word + " "; //last space cant be measured , lets measure an "," instead
                        }
                        //SizeF size=g.MeasureString (tmp,Element.Font);
                        bbuff.Font = GetFont(Element.Font);
                        Size s = bbuff.MeasureTabbedString(tmp, 0);
                        Element.words[i].Height = s.Height;
                        Element.words[i].Width = s.Width - 0;
                        Element.words[i].ScreenArea.Width = Element.words[i].Width;
                        Element.words[i].ScreenArea.Height = Element.words[i].Height;
                        //	Element.words[i].Link =Element.Link ;

                        i++;
                    }
                }
            }

            bbuff.Dispose();
        }

        private GDIFont GetFont(Font font)
        {
            GDIFont gf = null;
            if (!_Fonts.TryGetValue(GetFontKey(font),out gf))            
            {
                gf = new GDIFont(font.Name, font.Size, font.Bold, font.Italic, font.Underline, false);
                _Fonts[GetFontKey(font)] = gf;
            }

            return gf;
        }

        private string GetFontKey(Font font)
        {
            return font.Name + font.Bold + font.Italic + font.Underline + font.Size;
        }


        private void CreateRows()
        {
            if (_Elements != null)
            {
                int x = 0;
                _Rows = new List<Row>();

                //build rows---------------------------------------------
                var row = new Row();
                _Rows.Add(row);
                bool WhiteSpace = false;
                foreach (Element Element in _Elements)
                {
                    if (Element.words == null)
                        return;

                    if (Element.NewLine)
                    {
                        //tag forces a new line
                        x = 0;
                        row = new Row();
                        _Rows.Add(row);
                        WhiteSpace = true;
                    }
                    if (Element.TagName == "hr")
                    {
                        row.RenderSeparator = true;
                    }

                    //else
                    //{


                    foreach (Word word in Element.words)
                    {
                        if (WordWrap)
                        {
                            int scrollwdh = 0;
                            if (ScrollBars == ScrollBars.Both || ScrollBars == ScrollBars.Vertical)
                                scrollwdh = vScroll.Width;

                            if ((word.Width + x) > ClientWidth - Margin - scrollwdh)
                            {
                                //new line due to wordwrap
                                x = 0;
                                row = new Row();
                                _Rows.Add(row);
                                WhiteSpace = true;
                            }
                        }

                        if (word.Text.Replace(" ", "") != "" || word.Image != null)
                            WhiteSpace = false;

                        if (!WhiteSpace)
                        {
                            row.Words.Add(word);

                            x += word.Width;
                        }
                    }
                    //}
                }

                //apply width and height to all rows
                int index = 0;
                foreach (Row r in _Rows)
                {
                    int width = 0;
                    int height = 0;
                    int padd = 0;

                    if (index > 0)
                    {
                        int previndex = index - 1;
                        var prev = (Row) _Rows[previndex];
                        while (previndex >= 0 && prev.Words.Count == 0)
                        {
                            prev = (Row) _Rows[previndex];
                            previndex--;
                        }

                        if (previndex >= 0)
                        {
                            prev = (Row) _Rows[previndex];
                            if (prev.Words.Count > 0)
                            {
                                var w = (Word) prev.Words[prev.Words.Count - 1];
                                height = w.Height;
                            }
                        }
                    }


                    foreach (Word w in r.Words)
                    {
                        if (w.Height > height && (w.Text != ""))
                            height = w.Height;

                        width += w.Width;
                    }
                    r.Height = height;

                    int MaxImageH = 0;
                    foreach (Word w in r.Words)
                    {
                        if (w.Image != null)
                        {
                            if (w.Height > height)
                                MaxImageH = w.Height;
                        }
                    }

                    foreach (Word w in r.Words)
                    {
                        int imgH = 0;
                        int imgPadd = 0;
                        if (w.Image != null)
                        {
                            string valign = GetAttrib("valign", w.Element.Tag);
                            switch (valign)
                            {
                                case "top":
                                    {
                                        imgH = r.Height;
                                        imgPadd = w.Height - imgH;
                                        break;
                                    }
                                case "middle":
                                case "center":
                                    {
                                        imgH = r.Height;
                                        int tmp = (w.Height - imgH)/2;
                                        imgH += tmp;
                                        imgPadd = tmp;

                                        break;
                                    }
                                case "bottom":
                                    {
                                        imgH = w.Height;
                                        imgPadd = 0;
                                        break;
                                    }
                                default:
                                    {
                                        imgH = w.Height;
                                        imgPadd = 0;
                                        break;
                                    }
                            }

                            if (imgH > height)
                                height = imgH;

                            if (imgPadd > padd)
                                padd = imgPadd;


                            width += w.Width;
                        }
                    }
                    r.Width = width;
                    r.Height = height;
                    r.BottomPadd = padd;
                    index++;
                }

                vScroll.Maximum = _Rows.Count;
            }
        }

        private void InitScrollbars()
        {
            if (vScroll == null || hScroll == null)
                return;

            if (ScrollBars == ScrollBars.Both)
            {
                vScroll.Left = ClientWidth - vScroll.Width;
                vScroll.Top = 0;
                vScroll.Height = ClientHeight - hScroll.Height;

                hScroll.Left = 0;
                hScroll.Top = ClientHeight - hScroll.Height;
                hScroll.Width = ClientWidth - vScroll.Width;

                Filler.Left = vScroll.Left;
                Filler.Top = hScroll.Top;

                Filler.Visible = true;
                vScroll.Visible = true;
                hScroll.Visible = true;
            }
            else if (ScrollBars == ScrollBars.Vertical)
            {
                vScroll.Left = ClientWidth - vScroll.Width;
                vScroll.Top = 0;
                vScroll.Height = ClientHeight;

                hScroll.Left = 0;
                hScroll.Top = ClientHeight - hScroll.Height;
                hScroll.Width = ClientWidth - vScroll.Width;

                Filler.Left = vScroll.Left;
                Filler.Top = hScroll.Top;

                Filler.Visible = false;
                vScroll.Visible = true;
                hScroll.Visible = false;
            }
            else if (ScrollBars == ScrollBars.Horizontal)
            {
                vScroll.Left = ClientWidth - vScroll.Width;
                vScroll.Top = 0;
                vScroll.Height = ClientHeight;

                hScroll.Left = 0;
                hScroll.Top = ClientHeight - hScroll.Height;
                hScroll.Width = ClientWidth;

                Filler.Left = vScroll.Left;
                Filler.Top = hScroll.Top;

                Filler.Visible = false;
                vScroll.Visible = false;
                hScroll.Visible = true;
            }
            else if (ScrollBars == ScrollBars.None)
            {
                vScroll.Left = ClientWidth - vScroll.Width;
                vScroll.Top = 0;
                vScroll.Height = ClientHeight;

                hScroll.Left = 0;
                hScroll.Top = ClientHeight - hScroll.Height;
                hScroll.Width = ClientWidth;

                Filler.Left = vScroll.Left;
                Filler.Top = hScroll.Top;

                Filler.Visible = false;
                vScroll.Visible = false;
                hScroll.Visible = false;
            }
        }


        protected override void OnResize(EventArgs e)
        {
            try
            {
                InitScrollbars();
                SetAutoSize();
            }
            catch {}
            CreateRows();
            base.OnResize(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            int y = e.Y;
            int x = e.X;

            int index = 0;
            bool Link = false;
            //this.Cursor =Cursors.Arrow;
            _ActiveElement = null;
            if (_Rows != null)
            {
                foreach (Row r in _Rows)
                {
                    if (y >= r.Top && y <= r.Top + r.Height)
                    {
                        foreach (Word w in r.Words)
                        {
                            if (y >= w.ScreenArea.Top && y <= w.ScreenArea.Bottom)
                            {
                                if (x >= w.ScreenArea.Left && x <= w.ScreenArea.Right)
                                {
                                    if (w.Element.Link != null)
                                    {
                                        Link = true;
                                        _ActiveElement = w.Element.Link;
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    index++;
                }
            }
            if (Link)
            {
                Cursor = Cursors.Hand;
                Invalidate();
                OnClickLink(GetAttrib("href", _ActiveElement.Tag));
            }
            else
            {
                Cursor = Cursors.Arrow;
                Invalidate();
            }


            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int y = e.Y;
            int x = e.X;

            int index = 0;
            bool Link = false;
            //this.Cursor =Cursors.Arrow;
            _ActiveElement = null;
            if (_Rows != null)
            {
                foreach (Row r in _Rows)
                {
                    if (y >= r.Top && y <= r.Top + r.Height)
                    {
                        foreach (Word w in r.Words)
                        {
                            if (y >= w.ScreenArea.Top && y <= w.ScreenArea.Bottom)
                            {
                                if (x >= w.ScreenArea.Left && x <= w.ScreenArea.Right)
                                {
                                    if (w.Element.Link != null)
                                    {
                                        Link = true;
                                        _ActiveElement = w.Element.Link;
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    index++;
                }
            }
            if (Link)
            {
                Cursor = Cursors.Hand;
                Invalidate();
            }
            else
            {
                Cursor = Cursors.Arrow;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        private void vScroll_Scroll(object sender, ScrollEventArgs e)
        {
            Invalidate();
        }


        public int GetWidth()
        {
            int max = 0;
            foreach (Row r in _Rows)
            {
                if (r.Width > max)
                    max = r.Width;
            }

            return max + Margin*2 + BorderWidth*2;
        }

        public int GetHeight()
        {
            int max = 0;
            foreach (Row r in _Rows)
            {
                max += r.Height;
            }

            return max + Margin*2 + BorderWidth*2;
        }

        #region PUBLIC PROPERTY MARGIN

        private int _Margin;

        public new int Margin
        {
            get { return _Margin; }
            set
            {
                _Margin = value;
                CreateRows();
                Invalidate();
            }
        }

        #endregion
    }
}