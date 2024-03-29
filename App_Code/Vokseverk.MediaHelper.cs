using Umbraco;
using Umbraco.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors.ValueConverters;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.IO;

namespace Vokseverk {
	
	public class PictureSource {
		public string Media { get; set; }
		public string Crop { get; set; }
		public int Width { get; set; }
	}
	
	public class MediaSize {
		public int Width { get; set; }
		public int Height { get; set; }
		public decimal Ratio { get; set; }
	}
	
	public class MediaHelper {
				
		/// <summary>
		/// Render a `picture` tag with specified source elements
		/// the (picture|img)Attrs are string like "#pic1.showing.off"
		/// that can assign an id and/or multiple classnames
		/// </summary>
		public static HtmlString RenderPicture(IPublishedContent mediaItem, List<PictureSource> sources, string pictureAttrs = "", string imgAttrs = "") {
			var pictureClass = GetClassesFromAttrString(pictureAttrs);
			var pictureId = GetIdFromAttrString(pictureAttrs);
			var extension = mediaItem.Value<string>("UmbracoExtension");
			
			var html = "<picture";
			if (pictureId != "") {
				html += $" id=\"{pictureId}\"";
			}
			if (pictureClass != "") {
				html += $" class=\"{pictureClass}\"";
			}
			html += ">";
			
			string mediaAttr = "";
			
			try {
				if (extension.ToLower() == "gif") {
					// We need to bail and not try to resize/crop GIFs at all
					html += GetOutputTag(mediaItem.Url, mediaItem.Name);
				} else {
					foreach (var source in sources) {
						var mediaURL1x = GetCropUrl(mediaItem, source.Crop, source.Width, 80);
						var mediaURL2x = GetCropUrl(mediaItem, source.Crop, source.Width * 2, 60);
					
						if (source.Media == "2x") {
							// Special case for rendering a single image for 1x and 2x using a `<picture<` tag
							html += GetSourceTag(mediaURL1x, mediaURL2x, mediaAttr);
							html += GetOutputTag(mediaURL1x, mediaItem.Name);
						}
						else if (string.IsNullOrEmpty(source.Media)) {
							// Add required `<img>` tag
							var dimensions = GetCropSize(mediaItem, source.Crop, source.Width);
							html += GetOutputTag(mediaURL1x, mediaItem.Name, dimensions);
						} else {
							// Add `<source>` tag
							mediaAttr = source.Media.Replace("min", "min-width: ");
							mediaAttr = mediaAttr.Replace("max", "max-width: ");
					
							mediaAttr = string.Format("({0}px)", mediaAttr);
							html += GetSourceTag(mediaURL1x, mediaURL2x, mediaAttr);
						}
					}
				}
			} catch (Exception ex) {
				html += "<p style=\"color:red;font-weight:bold;\">Error: " + ex.Message + "</p>";
			}
			
			var imgClass = GetClassesFromAttrString(imgAttrs);
			var imgId = GetIdFromAttrString(imgAttrs);
			if (imgClass != "") {
				html = html.Replace("<img ", $"<img class=\"{imgClass}\" ");
			}
			if (imgId != "") {
				html = html.Replace("<img ", $"<img id=\"{imgId}\" ");
			}
			
			// Return a HtmlString
			html += "</picture>";
			
			return new HtmlString(html);
		}
		
		/// <summary>
		/// Retrieves the width, height and aspect ratio of a media item.
		/// Returns 0 for both if either of the properties are missing.
		/// </summary>
		public static MediaSize GetMediaSize(IPublishedContent media, int newWidth = 0) {
			var result = new MediaSize { Width = 0, Height = 0, Ratio = 1.0M };
			
			if (media.HasProperty("UmbracoWidth") && media.HasProperty("UmbracoHeight")) {
				var mediaWidth = media.Value<int>("UmbracoWidth");
				var mediaHeight = media.Value<int>("UmbracoHeight");
				var mediaRatio = Decimal.Divide(mediaHeight, mediaWidth);
				
				result.Width = newWidth > 0 ? newWidth : mediaWidth;
				result.Height = newWidth > 0 ? (int)(newWidth * mediaRatio) : mediaHeight;
				result.Ratio = mediaRatio;
			}
			
			return result;
		}
		
		/// <summary>
		/// Retrieves the width, height and aspect ratio of a specific crop.
		/// </summary>
		public static MediaSize GetCropSize(IPublishedContent media, string crop, int newWidth = 0) {
			var result = new MediaSize { Width = 0, Height = 0, Ratio = 1.0M };

			var cropperValue = media.Value<ImageCropperValue>("UmbracoFile");
			var cropData = cropperValue != null ? cropperValue.GetCrop(crop) : null;
			
			if (cropData != null) {
				var cropWidth = cropData.Width;
				var cropHeight = cropData.Height;
				var cropRatio = Decimal.Divide(cropHeight, cropWidth);
				
				result.Width = newWidth > 0 ? newWidth : cropWidth;
				result.Height = newWidth > 0 ? (int)(newWidth * cropRatio) : cropHeight;
				result.Ratio = cropRatio;
			}
			
			return result;
		}
		
		/// <summary>
		/// Render an img tag with srcset and src attributes for a media item,
		/// using the specified crop and output width.
		/// </summary>
		public static HtmlString RenderMedia(IPublishedContent image, string crop, int width) {
			string imageTag = "";
			
			try {
				if (image != null) {
					var dimensions = GetCropSize(image, crop, width);
					
					var crop1x = image.GetCropUrl(cropAlias: crop, width: width, quality: 80);
					var crop2x = image.GetCropUrl(cropAlias: crop, width: width * 2, quality: 60);
					
					imageTag = GetOutputTag(crop1x, crop2x, image.Name, dimensions);
				}
			} catch (Exception ex) {
				imageTag = GetOutputTag("/media/blank.png", string.Format("Did not find the media item. ({0})", ex.Message));
			}
			
			return new HtmlString(imageTag);
			
		}

		/// <summary>
		/// Render an img tag with srcset and src attributes for a media item,
		/// using the specified output width.
		/// </summary>
		public static HtmlString RenderMedia(IPublishedContent image, int width) {
			string imageTag = "";
			
			try {
				if (image != null) {
					var url = image.Url;
					var dimensions = GetMediaSize(image, width);
					var size1x = image.GetCropUrl(width: width, quality: 80);
					var size2x = image.GetCropUrl(width: width * 2, quality: 60);
					
					var extension = image.Value<string>("UmbracoExtension");
					
					if (extension == "gif") {
						// Don't resize GIFs
						imageTag = GetOutputTag(url, image.Name, dimensions);
					} else if (extension == "svg") {
						// Use the supplied width for SVGs
						imageTag = GetOutputTag(url, image.Name, width);
					} else {
						imageTag = GetOutputTag(size1x, size2x, image.Name, dimensions);
					}
				}
			} catch (Exception ex) {
				imageTag = GetOutputTag("/media/blank.png", string.Format("Could not find media item. ({0})", ex.Message));
			}
			
			return new HtmlString(imageTag);
			
		}
		
		/// <summary>
		/// Render the entire media inside a box defined by <paramref name="size" />
		/// </summary>
		/// <param name="size">A string in the form {width}x{height}, e.g.: 300x200</param>
		public static HtmlString RenderMedia(IPublishedContent image, string size) {
			string imageTag = "";
			string mediaUrl = GetMediaUrl(image);
			int w = 0;
			int h = 0;
			
			if (mediaUrl.Contains("GetMediaUrl")) {
				imageTag = string.Format("<!-- {0} -->", mediaUrl);
			} else {
				var dimensions = size.Split('x');

				if (Int32.TryParse(dimensions[0], out w) && Int32.TryParse(dimensions[1], out h)) {
					var size1x = string.Format("{0}?width={1}&height={2}&quality=70", mediaUrl, w, h);
					var size2x = string.Format("{0}?width={1}&height={2}&quality=40", mediaUrl, w * 2, h * 2);
				
					imageTag = GetOutputTag(size1x, size2x, image.Name);
				}
				
			}
			
			return new HtmlString(imageTag);
		}
		
		public static string GetPlaceholderUrl(string size) {
			return string.Format("//placehold.it/{0}", size);
		}
		
		public static string GetMediaUrl(IPublishedContent media) {
			try {
				if (media != null) {
					return media.Url;
				} else {
					return "(Media not found in GetMediaUrl)";
				}
			} catch {
				return "(Error in GetMediaUrl)";
			}
		}
		
		/// <summary>
		/// Single point of getting a URL for a mediaitem
		/// </summary>
		public static string GetCropUrl(IPublishedContent mediaItem, string crop, int width, int quality = 70) {
			string outputUrl = "";
			
			try {
				if (mediaItem != null) {
					outputUrl = mediaItem.GetCropUrl(cropAlias: crop, width: width, quality: quality);
					if (outputUrl.StartsWith("?")) {
						// Crop likely hasn't been saved yet for this image
						var xpath = $"/SiteSettings/CropSettings/crop[@name = '{crop}']/@size";
						var cropSize = Helpers.GetSiteSetting(xpath, true).Split('x');
						if (cropSize.Length == 2) {
							int cropWidth = Convert.ToInt32(cropSize[0]);
							int cropHeight = Convert.ToInt32(cropSize[1]);
							
							int height = (width / cropWidth) * cropHeight;
							// Now try again with specific width & height
							outputUrl = mediaItem.GetCropUrl(cropAlias: crop, width: width, height: height, quality: quality);
						}
					}
				}
			} catch (Exception ex) {
				outputUrl = "(error)";
			}
			
			return outputUrl;
		}
		
		public static HtmlString RenderSVGInline(string filename) {
			string toReturn = string.Empty;
			try {
				var mediaPath = Helpers.GetAssetPath(filename);
				
				if (!string.IsNullOrWhiteSpace(mediaPath)) {
					string path = HttpContext.Current.Server.MapPath(mediaPath);
					if (System.IO.File.Exists(path)) {
						toReturn = System.IO.File.ReadAllText(path);
						if (!string.IsNullOrWhiteSpace(toReturn) && toReturn.IndexOf("<svg") > 0) {
							toReturn = toReturn.Substring(toReturn.IndexOf("<svg"));
						}
					}
				}
			} catch (Exception ex) {
				return new HtmlString("<span style=\"background:#f998;color:#000;\">" + ex.ToString() + "</span>");
			}
			
			return new HtmlString(toReturn);
		}
		
		public static HtmlString RenderSVG(string reference, int width = 70, int height = 70) {
			string name = "";
			string svgTag = "";
			string prefix = "icon-";
			
			if (reference.EndsWith(".svg")) {
				var nameRE = new Regex(@"^.*?([^\/]+?)\.svg$");
				var match = nameRE.Match(reference);
				if (match.Success) {
					name = match.Groups[1].Value;
				}
			} else {
				// Assume a simple "chat" or "rollerblade-yellow" name
				name = prefix + reference;
			}
			svgTag = string.Format("<svg class=\"icon {0}\" viewBox=\"0 0 {1} {2}\" width=\"{1}\"><use xlink:href=\"#{0}\" /></svg>", name, width, height);

			return new HtmlString(svgTag);
		}
		
		public static HtmlString RenderIcon(string iconName, string iconStyle = null, string iconColor = null) {
			var svgMarkup = TransformIcon(iconName, "svgIcon.xslt");
			
			var iconTemplate = "<div class=\"icon {0} {1}\">{2}</div>";
			var html = string.Format(iconTemplate, iconStyle, iconColor, svgMarkup);
			
			return new HtmlString(html);
		}
		
		/// <summary>
		/// Extracts a single icon from the `icons.svg` file in the
		/// `assetsFolder` for rendering inline.
		/// </summary>
		public static HtmlString RenderIconInline(string iconName, string iconStyle = null, string iconColor = null) {
			var svgMarkup = TransformIcon(iconName, "svgInlineIcon.xslt");
			
			var iconTemplate = "<div class=\"icon {0} {1}\">{2}</div>";
			var html = string.Format(iconTemplate, iconStyle, iconColor, svgMarkup);
			
			return new HtmlString(html);
		}
		
		/// <summary>
		/// Transforms an icon from the `icons.svg` file in the `assetsFolder`
		/// using the XSLT file in `xsltFile`
		/// </summary>
		public static HtmlString TransformIcon(string iconName, string xsltFile) {
			string toReturn = string.Empty;
			string xsltDirectory = Umbraco.Core.IO.IOHelper.ResolveUrl("~/xslt/");
			string iconsFile = Helpers.GetAssetPath("icons.svg");
			
			try {
				if (!string.IsNullOrEmpty(iconName)) {
					string xmlPath = HttpContext.Current.Server.MapPath(iconsFile);
					string xsltPath = HttpContext.Current.Server.MapPath(xsltDirectory + xsltFile);
					if (System.IO.File.Exists(xmlPath) && System.IO.File.Exists(xsltPath)) {
						var xmlDoc = new XPathDocument(xmlPath);
						var processor = new XslCompiledTransform();
						
						var args = new XsltArgumentList();
						args.AddParam("iconsFile", "", iconsFile);
						args.AddParam("iconName", "", iconName);
						
						processor.Load(xsltPath);
						
						using (StringWriter output = new StringWriter())
						using (XmlWriter xw = XmlWriter.Create(output, processor.OutputSettings)) {
							processor.Transform(xmlDoc, args, xw, null);
							toReturn = output.ToString();
						}
					}
				}
			} catch (Exception error) {
				toReturn = error.Message;
			}
			
			return new HtmlString(toReturn);
		}
		
		#region Private
		
		// Actual <img> tag rendering
		private static string GetOutputTag(string image, string altText) {
			return string.Format("<img src=\"{0}\" alt=\"{1}\" />", image, altText);
		}
		private static string GetOutputTag(string image, string altText, int width) {
			return $"<img src=\"{image}\" alt=\"{altText}\" width=\"{width}\" />";
		}
		private static string GetOutputTag(string image, string altText, MediaSize sizes) {
			return string.Format("<img src=\"{0}\" width=\"{2}\" height=\"{3}\" alt=\"{1}\" />", image, altText, sizes.Width, sizes.Height);
		}
		
		// Overloads for a retina-ready image (srcset w/ 2x)
		private static string GetOutputTag(string size1x, string size2x, string altText) {
			return string.Format("<img srcset=\"{0} 2x\" src=\"{1}\" alt=\"{2}\" />", size2x, size1x, altText);
		}
		private static string GetOutputTag(string size1x, string size2x, string altText, MediaSize sizes) {
			return string.Format("<img srcset=\"{0} 2x\" src=\"{1}\" width=\"{3}\" height=\"{4}\" alt=\"{2}\" />", size2x, size1x, altText, sizes.Width, sizes.Height);
		}
		
		
		private static string GetSourceTag(string size1x, string size2x, string media = "") {
			var outputTag = "";
			
			if (media == "") {
				outputTag = string.Format("<source srcset=\"{0} 2x,{1}\" />", size2x, size1x);
			} else {
				outputTag = string.Format("<source media=\"{2}\" srcset=\"{0} 2x,{1}\" />", size2x, size1x, media);
			}
			
			return outputTag;
		}
	
		private static string GetIdFromAttrString(string attrs) {
			var id = "";
		
			var attrRE = new Regex(@"^#([^\.]+)");
			var match = attrRE.Match(attrs);
			if (match.Success) {
				id = match.Groups[1].Value;
			}
		
			return id;
		}
	
		private static string GetClassesFromAttrString(string attrs) {
			var klasses = "";
		
			var attrRE = new Regex(@"\.(.+)$");
			var match = attrRE.Match(attrs);
			if (match.Success) {
				klasses = match.Groups[1].Value.Replace(".", " ");
			}
		
			return klasses;
		}
		
		#endregion
	}
}
