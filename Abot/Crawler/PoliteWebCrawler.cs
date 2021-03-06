﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Abot.Core;
using Abot.Core.Decisions;
using Abot.Core.Limiters;
using Abot.Core.Repositories;
using Abot.Core.Requests;
using Abot.Core.Robots;
using Abot.Crawler.EventArgs;
using Abot.Crawler.Interfaces;
using Abot.Poco;
using Abot.Utils;
using Abot.Utils.Memory;
using Abot.Utils.Time;
using Robots;

namespace Abot.Crawler
{
	/// <summary>
	/// Extends the WebCrawler class and added politeness features like crawl delays and respecting robots.txt files. 
	/// </summary>
	[Serializable]
	public class PoliteWebCrawler : WebCrawler, IPoliteWebCrawler
	{
		#region Protected Fields

		/// <summary>
		/// Rate limmits
		/// </summary>
		protected IDomainRateLimiter DomainRateLimiter;

		/// <summary>
		/// Wrapper for IRobots. try to find robots.txt page
		/// </summary>
		protected IRobotsDotTextFinder RobotsDotTextFinder;

		/// <summary>
		/// Collect content from robots.txt page
		/// </summary>
		protected IRobotsDotText RobotsDotText;

		#endregion

		#region Ctors

		/// <summary>
		/// Creates a crawler instance with the default settings and implementations.
		/// </summary>
		public PoliteWebCrawler()
			: this(null, null, null, null, null, null, null, null, null)
		{ }

		/// <summary>
		/// Creates a crawler instance with custom settings or implementation.
		/// </summary>
		/// <param name="crawlConfiguration"></param>
		public PoliteWebCrawler(CrawlConfiguration crawlConfiguration)
			: this(crawlConfiguration, null, null, null, null, null, null, null, null)
		{ }

		/// <summary>
		/// Creates a crawler instance with custom settings or implementation.
		/// Passing in null for all params is the equivalent of the empty constructor
		/// </summary>
		/// <param name="threadManager">Distributes http requests over multiple threads</param>
		/// <param name="scheduler">Decides what link should be crawled next</param>
		/// <param name="pageRequester">Makes the raw http requests</param>
		/// <param name="hyperLinkParser">Parses a crawled page for it's hyperlinks</param>
		/// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
		/// <param name="crawlConfiguration">Configurable crawl values</param>
		/// <param name="memoryManager">Checks the memory usage of the host process</param>
		/// <param name="domainRateLimiter"></param>
		/// <param name="robotsDotTextFinder"></param>
		public PoliteWebCrawler(
			CrawlConfiguration crawlConfiguration,
			ICrawlDecisionMaker crawlDecisionMaker,
			IThreadManager threadManager,
			IScheduler scheduler,
			IPageRequester pageRequester,
			IHyperLinkParser hyperLinkParser,
			IMemoryManager memoryManager,
			IDomainRateLimiter domainRateLimiter,
			IRobotsDotTextFinder robotsDotTextFinder)
			: base(crawlConfiguration, crawlDecisionMaker, threadManager, scheduler, pageRequester, hyperLinkParser, memoryManager)
		{
			DomainRateLimiter = domainRateLimiter ??
				new DomainRateLimiter(CrawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds);

			RobotsDotTextFinder = robotsDotTextFinder ??
				new RobotsDotTextFinder(new PageRequester(CrawlContext.CrawlConfiguration));
		}

		#endregion

		#region Public Override Method

		/// <summary>
		/// Begins a synchronous crawl using the uri param,
		/// subscribe to events to process data as it becomes available
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="cancellationTokenSource"></param>
		/// <returns></returns>
		public override CrawlResult Crawl(Uri uri, CancellationTokenSource cancellationTokenSource)
		{
			TryLoadRobotsTxt(uri);

			PageCrawlStarting += (s, e) => DomainRateLimiter.RateLimit(e.PageToCrawl.Uri);

			return base.Crawl(uri, cancellationTokenSource);
		}

		/// <summary>
		/// Try to find and load site robots.txt
		/// </summary>
		/// <param name="uri"></param>
		protected bool TryLoadRobotsTxt(Uri uri)
		{
			int robotsDotTextCrawlDelayInSecs = 0;
			long robotsDotTextCrawlDelayInMillisecs = 0;

			// Load robots.txt
			if (CrawlContext.CrawlConfiguration.IsRespectRobotsDotTextEnabled)
			{
				RobotsDotText = RobotsDotTextFinder.Find(uri);

				if (RobotsDotText != null)
				{
					Logger.InfoFormat("Robots.txt was found!");

					FireRobotsDotTextParseCompletedAsync(RobotsDotText.Robots);
					FireRobotsDotTextParseCompleted(RobotsDotText.Robots);

					robotsDotTextCrawlDelayInSecs = RobotsDotText.GetCrawlDelay(CrawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);
					robotsDotTextCrawlDelayInMillisecs = TimeConverter.SecondsToMilliseconds(robotsDotTextCrawlDelayInSecs);
				}
				else
				{
					Logger.InfoFormat("Robots.txt was NOT found!");
				}
			}

			// Use whichever value is greater between the actual crawl delay value found,
			// the max allowed crawl delay value or the minimum crawl delay required for every domain
			if (robotsDotTextCrawlDelayInSecs > 0 &&
			    robotsDotTextCrawlDelayInMillisecs > CrawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds)
			{
				if (robotsDotTextCrawlDelayInSecs > CrawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds)
				{
					Logger.WarnFormat("[{0}] robot.txt file directive [Crawl-delay: {1}] is above the value set " +
					                  "in the config value MaxRobotsDotTextCrawlDelay, will use MaxRobotsDotTextCrawlDelay value instead.",
									  uri,
									  CrawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds);

					robotsDotTextCrawlDelayInSecs = CrawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds;
					robotsDotTextCrawlDelayInMillisecs = TimeConverter.SecondsToMilliseconds(robotsDotTextCrawlDelayInSecs);
				}

				Logger.WarnFormat("[{0}] robot.txt file directive [Crawl-delay: {1}] will be respected.",
					uri,
					robotsDotTextCrawlDelayInSecs);

				DomainRateLimiter.AddDomain(uri, robotsDotTextCrawlDelayInMillisecs);
			}

			return RobotsDotText != null;
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Check settings from config to get access crawl the page
		/// </summary>
		/// <param name="pageToCrawl">Page to crawl</param>
		/// <returns>Bool</returns>
		protected override bool ShouldCrawlPage(PageToCrawl pageToCrawl)
		{
			// Check is RobotsDotTextUserAgentString contain in robots.txt
			bool allowedByRobots = true;
			if (RobotsDotText != null)
				allowedByRobots = RobotsDotText.IsUrlAllowed(pageToCrawl.Uri.AbsoluteUri,
															  CrawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);

			// https://github.com/sjdirect/abot/issues/96 Handle scenario where the root is allowed
			// but all the paths below are disallowed like "disallow: /*"
			var allPathsBelowRootAllowedByRobots = false;
			if (RobotsDotText != null &&
				pageToCrawl.IsRoot &&
				allowedByRobots)
			{
				var anyPathOffRoot = pageToCrawl.Uri.AbsoluteUri.EndsWith("/") ?
					pageToCrawl.Uri.AbsoluteUri + "aaaaa" :
					pageToCrawl.Uri.AbsoluteUri + "/aaaaa";

				allPathsBelowRootAllowedByRobots = RobotsDotText.IsUrlAllowed(
					anyPathOffRoot,
					CrawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);
			}

			if (CrawlContext.CrawlConfiguration.IsIgnoreRobotsDotTextIfRootDisallowedEnabled &&
				pageToCrawl.IsRoot)
			{
				if (!allowedByRobots)
				{
					Logger.DebugFormat("Page [{0}] [Disallowed by robots.txt file], however since " +
									   "IsIgnoreRobotsDotTextIfRootDisallowedEnabled is set to true " +
									   "the robots.txt file will be ignored for this site.",
									   pageToCrawl.Uri.AbsoluteUri);

					RobotsDotText = null;
				}
				else if (!allPathsBelowRootAllowedByRobots)
				{
					Logger.DebugFormat("All Pages below [{0}] [Disallowed by robots.txt file], however since " +
									   "IsIgnoreRobotsDotTextIfRootDisallowedEnabled is set to true the robots.txt " +
									   "file will be ignored for this site.",
									   pageToCrawl.Uri.AbsoluteUri);

					RobotsDotText = null;
				}

			}
			else if (!allowedByRobots)
			{
				string message =
					$"Page [{pageToCrawl.Uri.AbsoluteUri}] not crawled, [Disallowed by robots.txt file], set " +
					"IsRespectRobotsDotText=false in config file if you would like to ignore robots.txt files.";
				Logger.DebugFormat(message);

				FirePageCrawlDisallowedEventAsync(pageToCrawl, message);
				FirePageCrawlDisallowedEvent(pageToCrawl, message);

				return false;
			}

			return base.ShouldCrawlPage(pageToCrawl);
		}

		/// <summary>
		/// Fire robots txt parsed completed async
		/// </summary>
		/// <param name="robots"></param>
		protected virtual void FireRobotsDotTextParseCompletedAsync(IRobots robots)
		{
			var threadSafeEvent = RobotsDotTextParseCompletedAsync;
			if (threadSafeEvent == null)
				return;

			//Fire each subscribers delegate async
			foreach (var subscriber in threadSafeEvent.GetInvocationList().Select(x => (EventHandler<RobotsDotTextParseCompletedEventArgs>)x))
			{
				subscriber.BeginInvoke(this, new RobotsDotTextParseCompletedEventArgs(CrawlContext, robots), null, null);
			}
		}

		/// <summary>
		/// Fire robots txt parsed completed
		/// </summary>
		/// <param name="robots"></param>
		protected virtual void FireRobotsDotTextParseCompleted(IRobots robots)
		{
			try
			{
				if (RobotsDotTextParseCompleted == null)
					return;

				RobotsDotTextParseCompleted.Invoke(this, new RobotsDotTextParseCompletedEventArgs(CrawlContext, robots));
			}
			catch (Exception e)
			{
				Logger.Error("An unhandled exception was thrown by a subscriber of the PageLinksCrawlDisallowed event for robots.txt");
				Logger.Error(e);
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// Event occur after robots txt is parsed asynchroniously
		/// </summary>
		public event EventHandler<RobotsDotTextParseCompletedEventArgs> RobotsDotTextParseCompletedAsync;

		/// <summary>
		/// Event occur after robots txt is parsed synchroniously
		/// </summary>
		public event EventHandler<RobotsDotTextParseCompletedEventArgs> RobotsDotTextParseCompleted;

		#endregion
	}
}
