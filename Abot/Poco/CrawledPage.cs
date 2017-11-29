﻿using System;
using System.Collections.Generic;
using System.Net;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CsQuery;
using HtmlAgilityPack;

namespace Abot.Poco
{
	/// <summary>
	/// Collect info about crawled page
	/// </summary>
	[Serializable]
	public class CrawledPage : PageToCrawl
	{
		#region Private Fields

		private readonly Lazy<HtmlDocument> _htmlDocument;
		private readonly Lazy<CQ> _csQueryDocument;
		private readonly Lazy<IHtmlDocument> _angleSharpHtmlDocument;

		private HtmlParser _angleSharpHtmlParser;

		#endregion

		#region Ctor

		/// <summary>
		/// Set parsers configuration
		/// </summary>
		/// <param name="uri"></param>
		public CrawledPage(Uri uri)
			: base(uri)
		{
			_htmlDocument = new Lazy<HtmlDocument>(InitializeHtmlAgilityPackDocument);
			_csQueryDocument = new Lazy<CQ>(InitializeCsQueryDocument);
			_angleSharpHtmlDocument = new Lazy<IHtmlDocument>(InitializeAngleSharpHtmlParser);

			Content = new PageContent();
		}

		#endregion

		#region Public Variables

		/// <summary>
		/// The raw content of the request
		/// </summary>
		[Obsolete("Please use CrawledPage.Content.Text instead", true)]
		public string RawContent { get; set; }

		/// <summary>
		/// Lazy loaded Html Agility Pack (http://htmlagilitypack.codeplex.com/)
		/// document that can be used to retrieve/modify html elements on the crawled page.
		/// </summary>
		public HtmlDocument HtmlDocument => _htmlDocument.Value;

		/// <summary>
		/// Lazy loaded CsQuery (https://github.com/jamietre/CsQuery)
		/// document that can be used to retrieve/modify html elements on the crawled page.
		/// </summary>
		[Obsolete("CSQuery is no longer actively maintained. Use AngleSharpHyperlinkParser for similar usage/functionality")]
		public CQ CsQueryDocument => _csQueryDocument.Value;

		/// <summary>
		/// Lazy loaded AngleSharp IHtmlDocument (https://github.com/AngleSharp/AngleSharp)
		/// that can be used to retrieve/modify html elements on the crawled page.
		/// </summary>
		public IHtmlDocument AngleSharpHtmlDocument => _angleSharpHtmlDocument.Value;

		/// <summary>
		/// Web request sent to the server
		/// </summary>
		public HttpWebRequest HttpWebRequest { get; set; }

		/// <summary>
		/// Web response from the server.
		/// NOTE: The Close() method has been called before setting this property.
		/// </summary>
		public HttpWebResponseWrapper HttpWebResponse { get; set; }

		/// <summary>
		/// The web exception that occurred during the crawl
		/// </summary>
		public WebException WebException { get; set; }

		/// <summary>
		/// The actual byte size of the page's raw content.
		/// This property is due to the Content-length header being untrustable.
		/// </summary>
		[Obsolete("Please use CrawledPage.Content.Bytes.Length instead", true)]
		public long PageSizeInBytes { get; set; }

		/// <summary>
		/// Links parsed from page. This value is set by the WebCrawler.SchedulePageLinks() method only.
		/// If the "ShouldCrawlPageLinks" rules return true or
		/// if the IsForcedLinkParsingEnabled config value is set to true.
		/// </summary>
		public IEnumerable<Uri> ParsedLinks { get; set; }

		/// <summary>
		/// The content of page request
		/// </summary>
		public PageContent Content { get; set; }

		/// <summary>
		/// A datetime of when the http request started
		/// </summary>
		public DateTime RequestStarted { get; set; }

		/// <summary>
		/// A datetime of when the http request completed
		/// </summary>
		public DateTime RequestCompleted { get; set; }

		/// <summary>
		/// A datetime of when the page content download started, this may be null
		/// if downloading the content was disallowed by the CrawlDecisionMaker or
		/// the inline delegate ShouldDownloadPageContent
		/// </summary>
		public DateTime? DownloadContentStarted { get; set; }

		/// <summary>
		/// A datetime of when the page content download completed, this may be null
		/// if downloading the content was disallowed by the CrawlDecisionMaker or
		/// the inline delegate ShouldDownloadPageContent
		/// </summary>
		public DateTime? DownloadContentCompleted { get; set; }

		/// <summary>
		/// The page that this pagee was redirected to
		/// </summary>
		public PageToCrawl RedirectedTo { get; set; }

		/// <summary>
		/// Time it took from RequestStarted to RequestCompleted in milliseconds
		/// </summary>
		public double Elapsed => (RequestCompleted - RequestStarted).TotalMilliseconds;

		#endregion

		#region Public Override Method

		/// <summary>
		/// Get absolute uri of the page
		/// </summary>
		/// <returns>String</returns>
		public override string ToString()
		{
			return HttpWebResponse == null ?
				Uri.AbsoluteUri :
				string.Format("{0}[{1}]", Uri.AbsoluteUri, (int)HttpWebResponse.StatusCode);
		}

		#endregion

		#region Private Methods

		private CQ InitializeCsQueryDocument()
		{
			CQ csQueryObject;

			try
			{
				csQueryObject = CQ.Create(Content.Text);
			}
			catch (Exception e)
			{
				csQueryObject = CQ.Create("");

				Logger.ErrorFormat("Error occurred while loading CsQuery object for Url [{0}]", Uri);
				Logger.Error(e);
			}

			return csQueryObject;
		}

		private HtmlDocument InitializeHtmlAgilityPackDocument()
		{
			HtmlDocument hapDoc = new HtmlDocument
			{
				OptionMaxNestedChildNodes = 5000 // did not make this an externally configurable property
												 // since it is really an internal issue to hap
			};

			try
			{
				hapDoc.LoadHtml(Content.Text);
			}
			catch (Exception e)
			{
				hapDoc.LoadHtml("");

				Logger.ErrorFormat("Error occurred while loading HtmlAgilityPack object for Url [{0}]", Uri);
				Logger.Error(e);
			}

			return hapDoc;
		}

		private IHtmlDocument InitializeAngleSharpHtmlParser()
		{
			if (_angleSharpHtmlParser == null)
				_angleSharpHtmlParser = new HtmlParser();

			IHtmlDocument document;
			try
			{
				document = _angleSharpHtmlParser.Parse(Content.Text);
			}
			catch (Exception e)
			{
				document = _angleSharpHtmlParser.Parse("");

				Logger.ErrorFormat("Error occurred while loading AngularSharp object for Url [{0}]", Uri);
				Logger.Error(e);
			}

			return document;
		}

		#endregion
	}
}
