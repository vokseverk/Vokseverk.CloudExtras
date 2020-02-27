using Umbraco;
using Umbraco.Web;
using Umbraco.Core.Models;
using System;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace Vokseverk {
	
	public class MediaHelper {
		private readonly static UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
		
		/// <summary>
		/// Render an img tag with srcset and src attributes for a media item,
		/// using the specified crop and output width.
		/// </summary>
		public static HtmlString RenderMedia(object mediaId, string crop, int width) {
			string imageTag = "";
			
			try {
				var media = umbracoHelper.TypedMedia(mediaId);
				if (media != null) {
					var crop1x = media.GetCropUrl(cropAlias: crop, width: width);
					var crop2x = media.GetCropUrl(cropAlias: crop, width: width * 2);
					
					imageTag = GetOutputTag(crop1x, crop2x, media.Name);
				}
			} catch (Exception ex) {
				imageTag = GetOutputTag("/media/blank.png", string.Format("Did not find the media item. ({0})", ex.Message));
			}
			
			return new HtmlString(imageTag);
		}

		/// <summary>
		/// Overload to render an img tag with srcset and src attributes for a media item,
		/// using the specified output width.
		/// </summary>
		public static HtmlString RenderMedia(object mediaId, int width) {
			string imageTag = "";
			
			try {
				var media = umbracoHelper.TypedMedia(mediaId);
				if (media != null) {
					var size1x = media.GetCropUrl(width: width);
					var size2x = media.GetCropUrl(width: width * 2);
					
					var extension = media.GetPropertyValue<string>("umbracoExtension");
					imageTag = extension == "gif"
						? GetOutputTag(media.GetCropUrl(), media.Name)
						: GetOutputTag(size1x, size2x, media.Name);
				}
			} catch (Exception ex) {
				imageTag = GetOutputTag("/media/blank.png", string.Format("Could not find media item. ({0})", ex.Message));
			}
			
			return new HtmlString(imageTag);
		}
		
		public static string GetPlaceholderUrl(string size) {
			return string.Format("//placehold.it/{0}", size);
		}
		
		public static string GetMediaUrl(object mediaId) {
			try {
				var media = umbracoHelper.TypedMedia(mediaId);
				if (media != null) {
					return media.Url;
				} else {
					return "(Media not found in GetMediaUrl)";
				}
			} catch {
				return "(Error in GetMediaUrl)";
			}
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

		#region Private
		
		private static string GetOutputTag(string image, string altText) {
			return string.Format("<img src=\"{0}\" alt=\"{1}\" />", image, altText);
		}
		
		private static string GetOutputTag(string size1x, string size2x, string altText) {
			return string.Format("<img srcset=\"{0} 2x\" src=\"{1}\" alt=\"{2}\" />", size2x, size1x, altText);
		}
		
		#endregion
	}
}
