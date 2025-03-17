using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Abstraction;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Geometry;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image;

[Export(typeof(IApiButton))]
[ToolStorageId("WebGIS.Tools.Serialization/{user}/_georefimages")]
[ToolStorageIsolatedUser(isUserIsolated: true)]
[AdvancedToolProperties(MapCrsDependent = true, StaticOverlayServicesDependent = true)]
[ToolHelp("tools/general/georef-image.html")]
[ToolPolicy(RequireAuthentication = true)]
public class GeoReference : IApiServerTool, IApiButtonResources, IApiToolConfirmation
{
    const string ToolbarElementId = "image-georef-toolbar";
    const string GeorefImageNameSelectId = "image-georef-imagename";
    const string GeorefImageUploadNameId = "image-georef-upload-imagename";
    const string GeorefImageDownloadImageId = "image-georef-download-imageid";
    const string GeorefImageDownloadNameId = "image-georef-download-imagename";
    const string GeorefImageDownloadMethodId = "image-georef-download-method";
    const string GeorefImageDownloadProjectionId = "image-georef-download-prj";
    const string GeorefImageLoadId = "image-georef-load-image";
    const string GeorefImageSelectedImagesId = "image-georef-selected-images";

    #region IApiButton

    public string Name
    {
        get { return "Bild Georeferenzieren (Beta)"; }
    }

    public string Container
    {
        get { return "Werkzeuge"; }
    }

    public string Image
    {
        get { return UIImageButton.ToolResourceImage(this, "georef-image"); }
    }

    public string ToolTip
    {
        get { return "Bild Georeferenzieren"; }
    }

    public bool HasUI
    {
        get { return true; }
    }

    #endregion

    #region IApiServerButton

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var apiResponse = new ApiEventResponse();

        AddUI(apiResponse, bridge, e);

        return apiResponse;
    }

    #endregion

    #region IApiServerTool

    public WebMapping.Core.Api.ToolType Type => WebMapping.Core.Api.ToolType.overlay_georef_def;

    public ToolCursor Cursor => ToolCursor.Crosshair;

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        var georefImageMetadata = bridge.GetGeorefImageMetata(e[GeorefImageNameSelectId]);
        if (georefImageMetadata == null)
        {
            throw new ArgumentException($"Can't laod image metadata for {e[GeorefImageNameSelectId]}");
        }

        var geoRefDef = e.OverlayGeoRefDefintion;
        if (geoRefDef == null)
        {
            throw new ArgumentException("$Can't georeference image: Invalid GeoRefDefintion");
        }
        georefImageMetadata.UpdatePassPoints(geoRefDef);
        georefImageMetadata.UpdatePosition(geoRefDef);

        if (georefImageMetadata.PassPoints.Count() > 0)
        {
            using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, e.MapCrs.Value))
            {
                georefImageMetadata.ProjectWorld(transformer);
                double worldWidth = georefImageMetadata.WorldWidth(), worldHeight = georefImageMetadata.WorldHeight();

                var passPoints = georefImageMetadata.PassPoints.Where(pp => pp.WorldPoint != null).ToArray();
                if (passPoints.Length == 1)
                {
                    var r1x = (georefImageMetadata.TopRight.X - georefImageMetadata.TopLeft.X) / worldWidth;
                    var r1y = (georefImageMetadata.TopRight.Y - georefImageMetadata.TopLeft.Y) / worldWidth;
                    var r2x = (georefImageMetadata.BottomLeft.X - georefImageMetadata.TopLeft.X) / worldHeight;
                    var r2y = (georefImageMetadata.BottomLeft.Y - georefImageMetadata.TopLeft.Y) / worldHeight;

                    var vX = passPoints[0].VectorX * worldWidth;
                    var vY = passPoints[0].VectorY * worldHeight;

                    var l = Vector<double>.Build.DenseOfArray(new double[]
                    {
                        passPoints[0].WorldPoint.X - (r1x * vX + r2x * vY),
                        passPoints[0].WorldPoint.Y - (r1y * vX + r2y * vY),
                    });
                    var A = Matrix<double>.Build.DenseIdentity(2, 2);
                    var x = A * l;  // A eqlas A.inverse()  (x === l)

                    georefImageMetadata.TopLeft.X = x[0];
                    georefImageMetadata.TopLeft.Y = x[1];
                    georefImageMetadata.TopRight.X = georefImageMetadata.TopLeft.X + worldWidth * r1x;
                    georefImageMetadata.TopRight.Y = georefImageMetadata.TopLeft.Y + worldWidth * r1y;
                    georefImageMetadata.BottomLeft.X = georefImageMetadata.TopLeft.X + worldHeight * r2x;
                    georefImageMetadata.BottomLeft.Y = georefImageMetadata.TopLeft.Y + worldHeight * r2y;
                }
                else
                {
                    if (passPoints.Length == 2)
                    {
                        #region Add Pseudo Point

                        var pseudoPasspoint = new PassPoint();

                        var dX = passPoints[1].WorldPoint.X - passPoints[0].WorldPoint.X;
                        var dY = passPoints[1].WorldPoint.Y - passPoints[0].WorldPoint.Y;

                        pseudoPasspoint.WorldPoint = new GeoPosition()
                        {
                            X = passPoints[0].WorldPoint.X - dY,  // perpenticular
                            Y = passPoints[0].WorldPoint.Y + dX
                        };

                        var dx = (passPoints[1].VectorX - passPoints[0].VectorX) * worldWidth;
                        var dy = (passPoints[1].VectorY - passPoints[0].VectorY) * worldHeight;

                        pseudoPasspoint.VectorX = passPoints[0].VectorX + dy / worldWidth;
                        pseudoPasspoint.VectorY = passPoints[0].VectorY - dx / worldHeight;

                        passPoints = new PassPoint[]
                        {
                        passPoints[0],
                        passPoints[1],
                        pseudoPasspoint
                        };

                        #endregion
                    }
                    var lData = new double[2 * passPoints.Length];
                    var AData = new double[2 * passPoints.Length, 6];

                    for (int i = 0; i < passPoints.Length; i++)
                    {
                        lData[i * 2] = passPoints[i].WorldPoint.X;
                        lData[i * 2 + 1] = passPoints[i].WorldPoint.Y;

                        AData[i * 2, 0] = 1.0;
                        AData[i * 2, 1] = 0.0;
                        AData[i * 2, 2] = passPoints[i].VectorX;
                        AData[i * 2, 3] = passPoints[i].VectorY;
                        AData[i * 2, 4] = 0.0;
                        AData[i * 2, 5] = 0.0;
                        AData[i * 2 + 1, 0] = 0.0;
                        AData[i * 2 + 1, 1] = 1.0;
                        AData[i * 2 + 1, 2] = 0.0;
                        AData[i * 2 + 1, 3] = 0.0;
                        AData[i * 2 + 1, 4] = passPoints[i].VectorX;
                        AData[i * 2 + 1, 5] = passPoints[i].VectorY;
                    }

                    //var l = Vector<double>.Build.DenseOfArray(new double[]
                    //{
                    //    passPoints[0].Pos.X,
                    //    passPoints[0].Pos.Y,
                    //    passPoints[1].Pos.X,
                    //    passPoints[1].Pos.Y,
                    //    passPoints[2].Pos.X,
                    //    passPoints[2].Pos.Y
                    //});
                    //var A = Matrix<double>.Build.DenseOfArray(new double[,]
                    //{
                    //    { 1.0, 0.0, passPoints[0].Vector.x, passPoints[0].Vector.y, 0.0,                    0.0 },
                    //    { 0.0, 1.0, 0.0,                    0.0,                    passPoints[0].Vector.x, passPoints[0].Vector.y },
                    //    { 1.0, 0.0, passPoints[1].Vector.x, passPoints[1].Vector.y, 0.0,                    0.0 },
                    //    { 0.0, 1.0, 0.0,                    0.0,                    passPoints[1].Vector.x, passPoints[1].Vector.y },
                    //    { 1.0, 0.0, passPoints[2].Vector.x, passPoints[2].Vector.y, 0.0,                    0.0 },
                    //    { 0.0, 1.0, 0.0,                    0.0,                    passPoints[2].Vector.x, passPoints[2].Vector.y },
                    //});

                    var l = Vector<double>.Build.DenseOfArray(lData);
                    var A = Matrix<double>.Build.DenseOfArray(AData);

                    var x = A.PseudoInverse() * l;

                    georefImageMetadata.TopLeft.X = x[0];
                    georefImageMetadata.TopLeft.Y = x[1];
                    georefImageMetadata.TopRight.X = georefImageMetadata.TopLeft.X + x[2];
                    georefImageMetadata.TopRight.Y = georefImageMetadata.TopLeft.Y + x[4];
                    georefImageMetadata.BottomLeft.X = georefImageMetadata.TopLeft.X + x[3];
                    georefImageMetadata.BottomLeft.Y = georefImageMetadata.TopLeft.Y + x[5];
                }

                georefImageMetadata.ProjectGeographic(transformer);
            }
        }

        georefImageMetadata.SaveGeorefImageMetadata(bridge);

        return new ApiEventResponse()
        {
            AddStaticOverlayServices = new StaticOverlayServiceDefinitionDTO[]
            {
                new StaticOverlayServiceDefinitionDTO()
                {
                    Id = georefImageMetadata.Id,
                    Name = georefImageMetadata.Name,
                    OverlayUrl = bridge.GeorefImageUrl(georefImageMetadata),
                    TopLeft = new double[] {
                        georefImageMetadata.TopLeft.Longitude,
                        georefImageMetadata.TopLeft.Latitude },
                    TopRight = new double[] {
                        georefImageMetadata.TopRight.Longitude,
                        georefImageMetadata.TopRight.Latitude },
                    BottomLeft = new double[] {
                        georefImageMetadata.BottomLeft.Longitude,
                        georefImageMetadata.BottomLeft.Latitude },
                }
            }
        };
    }

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("georef-image", Properties.Resources.georef_image);

        toolResourceManager.AddImageResource("add", Properties.Resources.round_plus);
        toolResourceManager.AddImageResource("remove", Properties.Resources.trashcan_x_16);
        toolResourceManager.AddImageResource("zoomto", Properties.Resources.zoom_in_16);

        toolResourceManager.AddImageResource("download", Properties.Resources.cloud_download_16);
        toolResourceManager.AddImageResource("upload", Properties.Resources.round_plus);
    }

    #endregion

    #region IApiToolConfirmation Member

    public ApiToolConfirmation[] ToolConfirmations
    {
        get
        {
            List<ApiToolConfirmation> confirmations = new List<ApiToolConfirmation>();
            confirmations.AddRange(ApiToolConfirmation.CommandComfirmations(typeof(GeoReference)));
            return confirmations.ToArray();
        }
    }

    #endregion

    #region Server Commands

    [ServerToolCommand("add-image-dialog")]
    public ApiEventResponse OnAddImageDilaog(IBridge bridge, ApiToolEventArguments e)
    {
        CleanupStorage(bridge);

        var georefImageMetadatas = bridge.GetGeorefImageMetatas();
        List<UISelect.Option> options = new List<UISelect.Option>();
        options.AddRange(georefImageMetadatas.Select(m => new UISelect.Option() { label = m.Name, value = m.Id }));

        List<IUIElement> uiElements = new List<IUIElement>();

        if (options.Count > 0)
        {
            uiElements.AddRange(new IUIElement[]
            {
                new UITitle() { label = "Bestehendes Bild auswählen:" },
                new UISelect()
                {
                    options = options.OrderBy(o => o.label).ToArray(),
                    id = GeorefImageLoadId,
                    css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterPersistentImportant })
                },
                new UIBreak(1),
                new UIButton(UIButton.UIButtonType.servertoolcommand, "load-image")
                {
                    text = "Bild in die Karte laden",
                    css = UICss.ToClass(new[] { UICss.OkButtonStyle })
                }
            });
        }

        uiElements.AddRange(new IUIElement[]
        {
            new UIBreak(1),
            new UITitle() { label = "oder hochladen (png, jpg, pdf):" },
            new UIUploadFile(this.GetType(), "upload-file") {
                id = "upload-file",
                css = UICss.ToClass(new string[]{ UICss.ToolParameter })
            }
        });

        List<IUISetter> uiSetter = new List<IUISetter>();
        if (!String.IsNullOrEmpty(e[GeorefImageLoadId]))
        {
            uiSetter.Add(new UISetter(GeorefImageLoadId, e[GeorefImageLoadId]));
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = "Bild der Karte hinzufügen",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = uiElements.ToArray()
                }
            },
            UISetters = uiSetter
        };
    }

    [ServerToolCommand("upload-file")]
    public ApiEventResponse OnUploadFile(IBridge bridge, ApiToolEventArguments e)
    {
        var file = e.GetFile("upload-file");
        if (file == null)
        {
            throw new Exception("No file uploaded");
        }


        var imageExtension = file.FileName.Split('.').Last().ToLower(); ;
        var data = file.Data;

        var importers = new IImportImage[]
        {
            new ImportImage(), new ImportZip(), new ImportPdf()
        };

        List<ImportPackage> imagePackages = new List<ImportPackage>();
        foreach (var importer in importers)
        {
            if (importer.SupportedExtensions.Contains(imageExtension))
            {
                imagePackages.AddRange(importer.GetImages(file.FileName, data));
            }
        }

        if (imagePackages.Where(p => p.ImageData != null).Any() == false)
        {
            throw new Exception("Es konnte leider kein Bild ausgelesen werden. Eventuell wird das PDF Format nicht unterstützt. Versuchen Sie stattdessen einen Screenshot des Bildes anzufertigen und laden Sie eine JPG oder PNG Datei hoch.");
        }

        List<string> imageUrls = new List<string>();
        string firstImageId = null;

        foreach (var imagePackage in imagePackages)
        {
            if (imagePackage.IsPureImage())
            {
                var storageName = $"{Guid.NewGuid().ToTempFilename()}";
                bridge.Storage.Save($"{storageName}",
                                    imagePackage.ImageData,
                                    WebMapping.Core.Api.IO.StorageBlobType.Data);

                imageUrls.Add(bridge.GeorefImageUrl(storageName));
            }
            else
            {
                var imageId = bridge.GenerateGeorefImageId();

                GeorefImageMetadata georefImageMetadata =
                    imagePackage.GetGeorefImageMetadata(bridge, imageId, e.MapCrs.HasValue ? e.MapCrs.Value : 0);

                var storageName = $"{imageId.GeorefImageIdToStorageName()}";

                bridge.Storage.Save($"{storageName}.meta", JSerializer.Serialize(georefImageMetadata));
                bridge.Storage.Save($"{storageName}.{imagePackage.ImageExtension}",
                                    imagePackage.ImageData,
                                    WebMapping.Core.Api.IO.StorageBlobType.Data);
            }
        }

        if (imageUrls.Count > 0)
        {
            var apiResponse = new ApiEventResponse()
            {
                UIElements = new IUIElement[]
                {
                    new UIDiv()
                    {
                        target = UIElementTarget.modaldialog.ToString(),
                        targettitle = "Bilder auswählen",
                        targetwidth = "80%",
                        elements = new IUIElement[]
                        {
                            new UILabel() { label = "Name:"},
                            new UIBreak(),
                            new UIInputText()
                            {
                                id = GeorefImageUploadNameId,
                                css = UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.ToolParameterRequiredClientside }),
                                style="width:100%",
                                value = file.FileName.Substring(0, file.FileName.Length - imageExtension.Length - 1),
                                required_message = "Name ist erforderlich"
                            },
                            new UIImageSelector()
                            {
                                id = GeorefImageSelectedImagesId,
                                ImageUrls = imageUrls,
                                css = UICss.ToClass(new []{ UICss.ToolParameter, UICss.ToolParameterRequiredClientside }),
                                required_message = "Bitte mindesttens ein Bild durch anklicken auswählen"
                            },
                            new UIButton(UIButton.UIButtonType.servertoolcommand, "store-selected-images")
                            {
                                text="Bild(er) übernehmen"
                            }
                        }
                    }
                }
            };

            return apiResponse;
        }

        e[GeorefImageLoadId] = firstImageId;

        return OnAddImageDilaog(bridge, e);
    }

    [ServerToolCommand("store-selected-images")]
    public ApiEventResponse OnStoreSelectedImages(IBridge bridge, ApiToolEventArguments e)
    {
        var imagesData = e[GeorefImageSelectedImagesId]
            .Split(',')
            .Select(s => bridge.GeorefImageUrlToStorageName(s))
            .Select(s => bridge.Storage.Load(s, WebMapping.Core.Api.IO.StorageBlobType.Data))
            .ToArray();

        string name = e[GeorefImageUploadNameId].Trim(), firstImageId = null;
        int counter = 1;
        foreach (var imageData in imagesData)
        {
            var imageId = bridge.GenerateGeorefImageId();
            var imageName = imagesData.Length > 1 ? $"{name}-{counter++}" : name;

            GeorefImageMetadata georefImageMetadata =
                   imageData.GetGeorefImageMetadata(bridge, imageId, imageName);

            var storageName = $"{imageId.GeorefImageIdToStorageName()}";

            bridge.Storage.Save($"{storageName}.meta", JSerializer.Serialize(georefImageMetadata));
            bridge.Storage.Save($"{storageName}.{georefImageMetadata.ImageExtension}",
                                imageData,
                                WebMapping.Core.Api.IO.StorageBlobType.Data);

            if (String.IsNullOrEmpty(firstImageId))
            {
                firstImageId = imageId;
            }
        }

        CleanupStorage(bridge);

        e[GeorefImageLoadId] = firstImageId;

        return OnAddImageDilaog(bridge, e);
    }

    [ServerToolCommand("download")]
    public ApiEventResponse OnDownload(IBridge bridge, ApiToolEventArguments e)
    {
        var id = e.ServerCommandArgument;
        if (String.IsNullOrEmpty(id))
        {
            return null;
        }

        var georefImageMetadata = bridge.GetGeorefImageMetata(id);
        if (georefImageMetadata != null)
        {
            #region Available PrjFiles 

            List<UISelect.Option> prjOptions = new List<UISelect.Option>();

            var di = new System.IO.DirectoryInfo($"{bridge.AppEtcPath}/prj");
            if (di.Exists)
            {
                foreach (var fi in di.GetFiles("*.prj"))
                {
                    try
                    {
                        int epsg = int.Parse(fi.Name.Substring(0, fi.Name.Length - 4));
                        var sRef = bridge.CreateSpatialReference(epsg);

                        if (sRef != null)
                        {
                            prjOptions.Add(new UISelect.Option() { value = epsg.ToString(), label = $"{epsg}: {sRef.Name}" });
                        }
                    }
                    catch { }
                }
            }

            var defaultEpsgCode = (e.MapCrs ?? 0).ToString();

            #endregion

            return new ApiEventResponse()
            {
                UIElements = new IUIElement[]
                {
                    new UIDiv()
                    {
                        target = UIElementTarget.modaldialog.ToString(),
                        targettitle="Bild herunterladen",
                        css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                        elements = new IUIElement[]
                        {
                            new UIHidden()
                            {
                                id=GeorefImageDownloadImageId,
                                css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                                value = id,
                            },
                            new UILabel() { label="Name (optional):"},
                            new UIInputText()
                            {
                                id = GeorefImageDownloadNameId,
                                css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                                value = georefImageMetadata.Name
                            },
                            new UIBreak(2),
                            new UISelect()
                            {
                                id = GeorefImageDownloadMethodId,
                                css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                                options=new UISelect.Option[]
                                {
                                    new UISelect.Option(){ value= "worldfile", label = "Worldfile" },
                                    new UISelect.Option(){ value= "passpoints", label= "Worldfile + Passpunkte"}
                                }
                            },
                            new UISelect()
                                {
                                    id = GeorefImageDownloadProjectionId,
                                    css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                                    options = prjOptions
                                },
                            new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand, "download-file")
                            {
                                css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle }),
                                text = "Herunterladen"
                            }),

                        }
                    }
                },
                UISetters = new IUISetter[]
                {
                    new UISetter(GeorefImageDownloadProjectionId, defaultEpsgCode)
                }
            };
        }

        return null;
    }

    [ServerToolCommand("download-file")]
    public ApiEventResponse OnDownloadFile(IBridge bridge, ApiToolEventArguments e)
    {
        var id = e[GeorefImageDownloadImageId];
        if (String.IsNullOrEmpty(id))
        {
            return null;
        }

        var georefImageMetadata = bridge.GetGeorefImageMetata(id);

        if (georefImageMetadata != null)
        {
            var title = e[GeorefImageDownloadNameId].OrTake(georefImageMetadata.Name);
            if (title.EndsWith($".{georefImageMetadata.ImageExtension}", StringComparison.OrdinalIgnoreCase))
            {
                title = title.Substring(0, title.Length - georefImageMetadata.ImageExtension.Length - 1);
            }

            var export = new ZipExport(bridge, georefImageMetadata);
            var zipBytes = export.GetBytes(
                title,
                "worldfile".Equals(e[GeorefImageDownloadMethodId]) || "passpoints".Equals(e[GeorefImageDownloadMethodId]),
                "passpoints".Equals(e[GeorefImageDownloadMethodId]),
                int.Parse(e[GeorefImageDownloadProjectionId]));

            return new ApiRawDownloadEventResponse($"{title}.zip", zipBytes);
        }

        return null;
    }

    [ServerToolCommand("load-image")]
    public ApiEventResponse LoadImage(IBridge bridge, ApiToolEventArguments e)
    {
        var id = e[GeorefImageLoadId];
        if (String.IsNullOrEmpty(id))
        {
            return null;
        }

        string unreferencedImageName = null;
        if (e.MapOverlayServices.Count() > 0)
        {
            var metadatas = bridge.GetGeorefImageMetatas();
            foreach (var overlayServiceId in e.MapOverlayServices)
            {
                var metadata = metadatas.Where(m => m.Id == overlayServiceId).FirstOrDefault();
                if (metadata != null && metadata.IsGeoreferenced() == false)
                {
                    unreferencedImageName = metadata.Name;
                    break;
                }
            }
        }

        var georefImageMetadata = bridge.GetGeorefImageMetata(id);
        if (georefImageMetadata != null)
        {
            if (!String.IsNullOrEmpty(unreferencedImageName) && georefImageMetadata.IsGeoreferenced() == false)
            {
                throw new Exception($"Das Bild kann nicht in die Karte geladen werden! Es darf sich nur ein nicht vollständig georeferenziertes Bild gleichzeitig in der Karte befinden. Bitte zuerst das nicht georeferenzierten Bild \"{unreferencedImageName}\" aus der Karte entfernen, bevor ein neues Bild zum Georeferenzieren in die Karte eingefügt wird.");
            }

            var overlayServiceDefinition = new StaticOverlayServiceDefinitionDTO()
            {
                Id = georefImageMetadata.Id,
                Name = georefImageMetadata.Name,
                OverlayUrl = bridge.GeorefImageUrl(georefImageMetadata),
                Opacity = 1.0, // 0.5,
                EditMode = true,
                PassPoints = georefImageMetadata.StaticOverlayServiceDefinitionPassPoints(),
                WidthHeightRatio = georefImageMetadata.CalcWidthHeightRatio()
            };

            if (georefImageMetadata.TopLeft.IsValid())
            {
                overlayServiceDefinition.TopLeft = new double[] {
                    georefImageMetadata.TopLeft.Longitude,
                    georefImageMetadata.TopLeft.Latitude
                };
            }
            if (georefImageMetadata.TopRight.IsValid())
            {
                overlayServiceDefinition.TopRight = new double[] {
                    georefImageMetadata.TopRight.Longitude,
                    georefImageMetadata.TopRight.Latitude
                };
            }
            if (georefImageMetadata.BottomLeft.IsValid())
            {
                overlayServiceDefinition.BottomLeft = new double[] {
                    georefImageMetadata.BottomLeft.Longitude,
                    georefImageMetadata.BottomLeft.Latitude
                };
            }

            var envelope = georefImageMetadata.Envelope4326(110.0);

            var apiEventResponse = new ApiEventResponse()
            {
                AddStaticOverlayServices = new StaticOverlayServiceDefinitionDTO[] { overlayServiceDefinition },
                ZoomTo4326 = envelope?.ToArray(),
                UISetters = new IUISetter[]
                {
                    new UISetter(GeorefImageNameSelectId, georefImageMetadata.Id)
                }
            };

            e.MapOverlayServices.Add(id);
            AddUI(apiEventResponse, bridge, e);

            // close dialog
            apiEventResponse.UIElements.Add(new UIEmpty()
            {
                target = UIElementTarget.modaldialog.ToString()
            });

            return apiEventResponse;
        }

        return null;
    }

    [ServerToolCommand("delete-image")]
    [ToolCommandConfirmation("Bild unwiederruflich löschen?", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
    public ApiEventResponse DeleteImage(IBridge bridge, ApiToolEventArguments e)
    {
        var id = e.ServerCommandArgument;
        if (String.IsNullOrEmpty(id))
        {
            return null;
        }

        GeorefImageMetadata georefImageMetadata = null;

        var name = id.GeorefImageIdToStorageName();

        if (bridge.Storage.Exists($"{name}.meta"))
        {
            georefImageMetadata = JSerializer.Deserialize<GeorefImageMetadata>(bridge.Storage.LoadString($"{name}.meta"));
        }
        bridge.Storage.Remove(name, recursive: true);

        var apiResponse = new ApiEventResponse()
        {
            RemoveStaticOverlayServices = georefImageMetadata != null ?
                new StaticOverlayServiceDefinitionDTO[]
                {
                    new StaticOverlayServiceDefinitionDTO()
                    {
                        Id = georefImageMetadata.Id,
                        Name = georefImageMetadata.Name,
                        OverlayUrl = bridge.GeorefImageUrl(georefImageMetadata),
                    }
                } : null
        };

        e.MapOverlayServices.Remove(id);
        e[GeorefImageNameSelectId] = String.Empty;
        AddUI(apiResponse, bridge, e);

        return apiResponse;
    }

    [ServerToolCommand("zoomto-image")]
    public ApiEventResponse ZoomToImage(IBridge bridge, ApiToolEventArguments e)
    {
        var georefImageMetadata = bridge.GetGeorefImageMetata(e.ServerCommandArgument);
        if (georefImageMetadata == null)
        {
            throw new ArgumentException($"Can't laod image metadata for {e.ServerCommandArgument}");
        }

        return new ApiEventResponse()
        {
            ZoomTo4326 = georefImageMetadata.Envelope4326(110.0)?.ToArray()
        };
    }

    #endregion

    #region Helper

    private void AddUI(ApiEventResponse apiResponse, IBridge bridge, ApiToolEventArguments e)
    {
        var uiElements = new List<IUIElement>();

        uiElements.Add(CreateToolbarUI(bridge, e));
        if (e.MapOverlayServices.Count() == 0)
        {
            uiElements.Add(new UILabel()
            {
                label = "Mit diesem Werkzeug können Bilder hochgeladen und verortet werden."
            });
        }
        uiElements.Add(new UIStaticOverlayControl()
        {
            id = GeorefImageNameSelectId,
            css = UICss.ToClass(new string[] { UICss.ToolParameter }),
            command_buttons = new UIStaticOverlayControl.CommandButton[]
            {
                new UIStaticOverlayControl.CommandButton("zoomto-image",UIImageButton.ToolResourceImage(this, "zoomto")),
                new UIStaticOverlayControl.CommandButton("download", UIImageButton.ToolResourceImage(this, "download")),
                new UIStaticOverlayControl.CommandButton("delete-image",UIImageButton.ToolResourceImage(this, "remove"))
            }
        });

        apiResponse.UIElements = new List<IUIElement>(new IUIElement[] {
                new UIDiv()
                {
                    //target=UIElementTarget.modaldialog.ToString(),
                    targettitle="Dokument georeferenzieren",
                    elements = uiElements.ToArray()
                }
            });

        if (apiResponse.UISetters == null)
        {
            apiResponse.UISetters = new IUISetter[]
            {
                new UISetter(GeorefImageNameSelectId, e[GeorefImageNameSelectId])
            };
        }
    }

    private IUIElement CreateToolbarUI(IBridge bridge, ApiToolEventArguments e, bool replace = false)
    {
        var id = e[GeorefImageNameSelectId];

        List<IUIElement> uiImageButtons = new List<IUIElement>();

        uiImageButtons.Add(
            new UIButton(UIButton.UIButtonType.servertoolcommand, "add-image-dialog")
            {
                text = "Bild hinzufügen",
                css = UICss.ToClass(new[] { UICss.OptionRectButtonStyle, UICss.Width_25Percent }),
                icon = UIButton.ToolResourceImage(this.GetType(), "upload")
            });

        return new UIDiv()
        {
            id = ToolbarElementId,
            elements = uiImageButtons.ToArray(),
            target = replace ? $"#{ToolbarElementId}" : null
        };
    }

    private void CleanupStorage(IBridge bridge)
    {
        foreach (var storageName in bridge.Storage.GetNames(true, false, WebMapping.Core.Api.IO.StorageBlobType.Data))
        {
            if (storageName.IsTemFilename())
            {
                bridge.Storage.Remove(storageName, WebMapping.Core.Api.IO.StorageBlobType.Data);
            }
        }
    }

    #endregion
}
