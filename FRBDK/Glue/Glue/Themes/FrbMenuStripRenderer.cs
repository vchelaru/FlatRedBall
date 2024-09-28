using System.Drawing;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Themes;

public class FrbMenuStripColorTable : ProfessionalColorTable
{
    private Color _backgroundColor;
    private Color _foregroundColor;
    private Color _primaryColor;

    public FrbMenuStripColorTable(Color backgroundColor, Color foregroundColor, Color primaryColor)
    {
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _primaryColor = primaryColor;
    }

    public void SetBackgroundColor(Color color)
    {
        _backgroundColor = color;
    }

    public void SetForegroundColor(Color color)
    {
        _foregroundColor = color;
    }

    public void SetPrimaryColor(Color color)
    {
        _primaryColor = color;
    }

    public override Color MenuItemSelected
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemSelectedGradientBegin
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemSelectedGradientEnd
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemBorder
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemPressedGradientBegin
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemPressedGradientEnd
    {
        get { return _primaryColor; }
    }

    public override Color MenuItemPressedGradientMiddle
    {
        get { return _primaryColor; }
    }

    public override Color MenuBorder
    {
        get { return _primaryColor; }
    }

    public override Color MenuStripGradientBegin
    {
        get { return _backgroundColor; }
    }

    public override Color MenuStripGradientEnd
    {
        get { return _backgroundColor; }
    }

    //public override Color MenuStripGradientMiddle
    //{
    //    get { return _backgroundColor; }
    //}

    //public override Color MenuStripText
    //{
    //    get { return _foregroundColor; }
    //}

    public override Color ToolStripDropDownBackground
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripGradientBegin
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripGradientEnd
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripGradientMiddle
    {
        get { return _backgroundColor; }
    }

    public override Color ToolStripBorder
    {
        get { return _primaryColor; }
    }

    public override Color ToolStripContentPanelGradientBegin
    {
        get { return _backgroundColor; }
    }
}

public class FrbMenuStripRenderer : ToolStripProfessionalRenderer
{
    private Color _backgroundColor;
    private Color _foregroundColor;
    private Color _primaryColor;
    private Color _primaryTransparent;
    private Font _font;

    public FrbMenuStripRenderer(Color backgroundColor, Color foregroundColor, Color primaryColor) : base(new FrbMenuStripColorTable(backgroundColor, foregroundColor, primaryColor))
    {
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
        _primaryColor = primaryColor;
        _primaryTransparent = Color.FromArgb(64, primaryColor);
        _font = new Font("Verdana", 8, FontStyle.Regular);
    }

    public void SetBackgroundColor(Color color)
    {
        _backgroundColor = color;
    }

    public void SetForegroundColor(Color color)
    {
        _foregroundColor = color;
    }

    public void SetPrimaryColor(Color color)
    {
        _primaryColor = color;
    }

    // Render background of MenuStrip and dropdown menus
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using (SolidBrush brush = new SolidBrush(_backgroundColor))
        {
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }
    }

    // Render background of menu items, including the margin area
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Rectangle menuItemBounds = new Rectangle(Point.Empty, e.Item.Size);

        

        using (SolidBrush brush = new SolidBrush(_backgroundColor))
        {
            e.Graphics.FillRectangle(brush, menuItemBounds);
        }

        // Use primary color for hovered or selected items
        if (e.Item.Selected || e.Item.Pressed)
        {
            using (SolidBrush brush = new SolidBrush(_primaryTransparent))
            {
                e.Graphics.FillRectangle(brush, menuItemBounds);
            }

            using (SolidBrush brush = new SolidBrush(_primaryColor))
            {
                e.Graphics.DrawRectangle(new Pen(brush, 2), menuItemBounds.X, menuItemBounds.Y, menuItemBounds.Width, menuItemBounds.Height);
            }
        }
    }

    // Render the text of menu items
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = _foregroundColor; // Set the custom text color
        e.TextFont = _font;
        base.OnRenderItemText(e); // Call base to render the text with the new color
    }

    // Render arrows for dropdown items
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = _foregroundColor; // Use the foreground color for the arrow
        base.OnRenderArrow(e);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {

    }
}