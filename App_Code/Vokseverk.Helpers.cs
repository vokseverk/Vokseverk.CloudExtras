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
		/// Simple (very) Markdown parsing for headers etc.
		/// Currently handles *emphasis* and **strong emphasis**
		/// </summary>
		/// <param name="text">The string to parse</param>
		public static HtmlString Markdownify(string text) {
			var patternStrong = @"\*\*([^\*]+)\*\*";
			var replaceStrong = "<strong>$1</strong>";

			var patternEmph = @"\*([^\*]+)\*";
			var replaceEmph = "<em>$1</em>";
			
			var strongRE = new Regex(patternStrong);
			var emphRE = new Regex(patternEmph);
			
			var parsed = strongRE.Replace(text, replaceStrong);
			parsed = emphRE.Replace(parsed, replaceEmph);
			
			return new HtmlString(parsed);
		}

		
		public static HtmlString DataMarkup(string text) {
			var patternData = @"\*([^\*]+)\*";
			var replaceData = "</p><data>$1</data><p>";
			var dataRE = new Regex(patternData);
			
			var parsed = dataRE.Replace(text, replaceData);
			
			return new HtmlString(parsed);
		}

		/// <summary>
		/// Converts a token string inside text to HTML breaks
		/// </summary>
		/// <param name="text">The string to to parse</param>
		/// <param name="token">The string to look for and replace with a break</param>
		public static HtmlString ReplaceTokenWithLineBreak(string text, string token) {
			var replaced = text.Replace(token, "<br/>");
			return new HtmlString(replaced);
		}

		/// <summary>
		/// Wraps a portion of a string as an HTML link to the specied link.
		/// </summary>
		/// <param name="text">The string to parse</param>
		/// <param name="url">A URL/link to insert into the result</param>
		public static HtmlString Linkify(string text, string url) {
			var patternWrap = @"\*([^\*]+)\*";
			var replaceWrap = "<a href=\"" + url + "\">$1</a>";
			
			var wrapRE = new Regex(patternWrap);
			var parsed = wrapRE.Replace(text, replaceWrap);
			
			return new HtmlString(parsed);
		}

		/// <summary>
		/// Strips the characters that are used with the Linkify() helper
		/// (to show the string unlinked, somewhere).
		/// </summary>
		/// <param name="text">The string to clean characters from</param>
		public static string UnLinkify(string text) {
			return text.Replace("*", "");
		}

		/// <summary>
		/// Format a date value for use in frontend
		/// </summary>
		public static string PrettifyDate(DateTime date, string format = "DEFAULT") {
			string prettyDate = date.ToString("d. MMMM yyyy");
			
			if (format != "DEFAULT") {
				prettyDate = date.ToString(format);
			}
			
			return prettyDate;
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
		
		public static HtmlString GetInjections(string position) {
			XmlDocument settingsDoc = GetSettings();
			XmlNode settings = settingsDoc.DocumentElement;
			
			string searchPath = string.Empty;
			string settingValue = string.Empty;

			string xpath = "Injections/inject[@position = '" + position + "']";
			
			string environment = GetCurrentEnvironment();
			searchPath = string.Format("Settings[@for = '{0}']/{1}", environment, xpath);
			
			var injections = settings.SelectNodes(searchPath);
			
			foreach (XmlNode injection in injections) { 
				if (injection.FirstChild != null && injection.FirstChild.Name == "script") {
					settingValue += "<script>" + injection.InnerText + "</script>";
				} else {
					settingValue += injection.InnerXml;
				}
			}
			
			// Return a HtmlString
			return new HtmlString(settingValue);
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
		/// Returns a string identifying which Umbraco Cloud environment the site is running,
		/// e.g., `live` or `development`.
		/// </summary>
		public static string GetCurrentEnvironment() {
			return GetSiteSetting("@environment", true);
		}
		
		// TODO: Do we need this?
		public static string GetBodyClass(string doctypeAlias) {
			string classes = "";
			switch (doctypeAlias) {
				case "One":
					classes += " one";
					break;
				case "Two":
					classes += " two";
					break;
				case "Other":
					classes += " other";
					break;
				default:
					classes += "";
					break;
			}
			
			return classes.Trim();
		}
		
		#endregion
	}
}
