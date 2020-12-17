using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
	
namespace Vokseverk {
	
	public class Helpers {
		
		#region Private Methods
		
		/// <summary>
		/// This returns the site settings as an XmlDocument
		/// </summary>
		private static XmlDocument GetSettings() {
			XmlDocument settings = new XmlDocument();
			
			try {
				settings.Load(HttpContext.Current.Server.MapPath("~/Config/SiteSettings.config"));
			} catch (Exception ex) {
				settings.LoadXml("<Error>" + ex.ToString() + "</Error>");
			}
			return settings;
		}
		
		/// <summary>
		/// Format an exception as an XML node that can be handled by XSLT.
		/// </summary>
		private static XPathNodeIterator ErrorDoc(Exception ex) {
			var doc = new XmlDocument();
			doc.LoadXml(string.Format("<error>{0}</error>", ex.ToString()));
			return doc.CreateNavigator().Select("/error");
		}
		
		#endregion
		
		#region Public Methods
		
		/// <summary>
		/// Returns a string identifying which Umbraco Cloud environment the site is running,
		/// e.g., `live` or `development`.
		/// </summary>
		public static string GetCurrentEnvironment() {
			return GetSiteSetting("@environment", true);
		}
		
		/// <summary>
		/// Get a single setting from the current environment as a string
		/// </summary>
		/// <param name="xpath">An XPath expression to select the desired setting</param>
		public static string GetSiteSetting(string xpath) {
			return GetSiteSetting(xpath, false);
		}

		/// <summary>
		/// Get a single setting as a string
		/// </summary>
		/// <param name="xpath">An XPath expression to select the desired setting</param>
		/// <param name="isGlobal">A boolean flag indicating if <paramref name="xpath" /> is global or should only select within the current environment</param>
		public static string GetSiteSetting(string xpath, bool isGlobal) {
			XmlDocument settingsDoc = GetSettings();
			XmlNode settings = settingsDoc.DocumentElement;
			
			string searchPath = string.Empty;
			string settingValue = string.Empty;
			
			if (xpath != null) {
				if (isGlobal) {
					searchPath = xpath;
				} else {
					string environment = GetCurrentEnvironment();
					searchPath = string.Format("Settings[@for = '{0}']/{1}", environment, xpath);
				}

				var setting = settings.SelectSingleNode(searchPath);
				if (setting != null) {
					settingValue = setting.InnerText;
				}
			}
			
			return settingValue;
		}
		
		/// <summary>
		/// Gets the path for the current assets folder
		/// </summary>
		/// <returns>A path string</returns>
		public static string GetAssetsFolder() {
			var versionFolder = GetSiteSetting("assetsFolder");
			return string.Format("/assets/{0}/", versionFolder);
		}

		/// <summary>
		/// Gets the path to the <paramref name="filename"/> file in the assets folder
		/// </summary>
		/// <returns>The asset's path as a string to be used in e.g. a src attribute</returns>
		/// <param name="filename">Filename.</param>
		public static string GetAssetPath(string filename) {
			var baseDir = GetAssetsFolder();
			return baseDir + filename;
		}
		
		/// <summary>
		/// Returns <paramref name="count" /> random children from the <paramref name="parent"/>.
		/// </summary>
		/// <param name="parent">The node to get children from</param>
		/// <param name="count">The number of random nodes to return</param>
		public static IEnumerable<IPublishedContent> GetRandomContent(IPublishedContent parent, int count = 1) {
			var randomContent = Enumerable.Empty<IPublishedContent>();
			
			if (parent != null && count >= 1) {
				randomContent = GetRandomContent(parent.Children, count);
			}
			
			return randomContent;
		}

		/// <summary>
		/// Returns <paramref name="count" /> random elements from the <paramref name="collection"/>.
		/// </summary>
		/// <param name="collection">The collection to get elements from</param>
		/// <param name="count">The number of random elements to return</param>
		public static IEnumerable<IPublishedContent> GetRandomContent(IEnumerable<IPublishedContent> collection, int count = 1) {
			var randomContent = Enumerable.Empty<IPublishedContent>();
			var random = new Random();
			
			if (collection != null && count >= 1) {
				randomContent = collection.OrderBy(x => random.Next()).Take(count);
			}
			
			return randomContent;
		}
		
		#endregion
	}
}
