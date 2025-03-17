using gView.GraphicsEngine;
using System;
using System.Text;

namespace E.Standard.WebMapping.Core.Renderer;

public class LabelRenderer
{
    public enum HowManyLabelsEnum
    {
        One_label_per_part = 0,
        One_label_per_name = 1,
        One_label_per_shape = 2
    }
    public enum LabelStyleEnum
    {
        regular = 0,
        bold = 1,
        italic = 2,
        bolditalic = 3,
        underline = 4,
        outline = 5
    }
    public enum LabelBorderStyleEnum
    {
        none = 0,
        shadow = 1,
        glowing = 2,
        blockout = 3
    }

    private HowManyLabelsEnum _howManyLabels = HowManyLabelsEnum.One_label_per_part;
    private LabelStyleEnum _labelStyle = LabelStyleEnum.regular;
    private LabelBorderStyleEnum _labelBorderStyle = LabelBorderStyleEnum.none;

    private float _fontSize = 10f;
    private ArgbColor _fontColor = ArgbColor.Black, _borderColor = ArgbColor.Yellow;
    private string _labelField = String.Empty;

    #region Properties 
    public HowManyLabelsEnum HowManyLabels
    {
        get { return _howManyLabels; }
        set { _howManyLabels = value; }
    }
    public LabelStyleEnum LabelStyle
    {
        get { return _labelStyle; }
        set { _labelStyle = value; }
    }
    public LabelBorderStyleEnum LabelBorderStyle
    {
        get { return _labelBorderStyle; }
        set { _labelBorderStyle = value; }
    }
    public float FontSize
    {
        get { return _fontSize; }
        set { _fontSize = value; }
    }
    public ArgbColor FontColor
    {
        get { return _fontColor; }
        set { _fontColor = value; }
    }
    public ArgbColor BorderColor
    {
        get { return _borderColor; }
        set { _borderColor = value; }
    }
    public string LabelField
    {
        get { return _labelField; }
        set { _labelField = value; }
    }
    #endregion

    #region Members
    public string ToArcXML()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<SIMPLELABELRENDERER field=\"" + _labelField + "\" howmanylabels=\"" + _howManyLabels.ToString() + "\" >");
        sb.Append("<TEXTSYMBOL font=\"Arial\" fontstyle=\"" + _labelStyle.ToString() + "\" fontsize=\"" + _fontSize.ToString() + "\" ");
        sb.Append("fontcolor=\"" + _fontColor.R + "," + _fontColor.G + "," + _fontColor.B + "\" ");
        switch (_labelBorderStyle)
        {
            case LabelBorderStyleEnum.none:
                break;
            case LabelBorderStyleEnum.shadow:
                sb.Append("shadow=\"" + _borderColor.R + "," + _borderColor.G + "," + _borderColor.B + "\" ");
                break;
            case LabelBorderStyleEnum.glowing:
                sb.Append("glowing=\"" + _borderColor.R + "," + _borderColor.G + "," + _borderColor.B + "\" ");
                break;
            case LabelBorderStyleEnum.blockout:
                sb.Append("blockout=\"" + _borderColor.R + "," + _borderColor.G + "," + _borderColor.B + "\" ");
                break;
        }
        sb.Append(" antialias=\"true\" />");
        sb.Append("</SIMPLELABELRENDERER>");

        return sb.ToString();
    }
    #endregion
}
