using System;

namespace E.Standard.WebGIS.Core.Reflection;

public class AdvancedToolPropertiesAttribute : Attribute
{
    public bool VisFilterDependent { get; set; }
    public bool AllowCtrlBBox { get; set; }
    public bool SelectionInfoDependent { get; set; }
    public bool ScaleDependent { get; set; }
    public bool AnonymousUserIdDependent { get; set; }
    public bool ClientDeviceDependent { get; set; }
    public bool MapCrsDependent { get; set; }
    public bool MapBBoxDependent { get; set; }
    public bool MapImageSizeDependent { get; set; }
    public bool AsideDialogExistsDependent { get; set; }
    public bool LiveShareClientnameDependent { get; set; }
    public bool PrintLayoutRotationDependent { get; set; }
    public bool LabelingDependent { get; set; }
    public bool CustomServiceRequestParametersDependent { get; set; }
    public bool StaticOverlayServicesDependent { get; set; }
    public bool UIElementDependency { get; set; }
    public FocusableUIElements UIElementFocus { get; set; }
    public bool QueryMarkersVisibliityDependent { get; set; }
    public bool CoordinateMarkersVisibilityDependent { get; set; }
    public bool ChainageMarkersVisibilityDependent { get; set; }
}
