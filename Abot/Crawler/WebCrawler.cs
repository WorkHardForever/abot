using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Abot.Core;
using Abot.Core.Config;
using Abot.Core.Parsers;
using Abot.Core.Repositories;
using Abot.Poco;
using Abot.Util;
using Abot.Util.Threads;
using CefSharp.Internals;
using log4net;
using Timer = System.Timers.Timer;
using static Abot.Poco.CrawlConfiguration;

namespace Abot.Crawler
{
	/// <summary>
	/// Base crawler of the library
	/// </summary>
	[Serializable]
	public abstract class WebCrawler : IWebCrawler
	{
		// TODO converter
		#region Const

		/// <summary>
		/// Value for translation seconds to milliseconds
		/// </summary>
		public const int MillisecondTranslation = 1000;

		#endregion

		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger => _logger.Value;
		private readonly Lazy<ILog> _logger = new Lazy<ILog>(() => LogManager.GetLogger(CrawlConfiguration.LoggerName));

		/// <summary>
		/// Context for crawling
		/// </summary>
		protected CrawlContext CrawlContext;

		/// <summary>
		/// Time for waiting between 2 crawling operations.
		/// Requare when site block fast crawling pages
		/// </summary>
		protected Timer TimeoutTimer;

		/// <summary>
		/// Decides whether or not to crawl a page or that page's links
		/// </summary>
		protected ICrawlDecisionMaker CrawlDecisionMaker;

		/// <summary>
		/// Distributes http requests over multiple threads
		/// </summary>
		protected IThreadManager ThreadManager;

		/// <summary>
		/// Makes the raw http requests
		/// </summary>
		protected IPageRequester PageRequester;

		/// <summary>
		/// Parses a crawled page for it's hyperlinks
		/// </summary>
		protected IHyperLinkParser HyperLinkParser;

		/// <summary>
		/// Checks the memory usage of the host process
		/// </summary>
		protected IMemoryManager MemoryManager;

		#region Triggers

		/// <summary>
		/// Trigger that fire, when crawl is over
		/// </summary>
		protected bool CrawlComplete;

		/// <summary>
		/// Trigger that fire, when crawl should stop working
		/// </summary>
		protected bool CrawlStopReported;

		/// <summary>
		/// Trigger that fire, when was cancellation request
		/// </summary>
		protected bool CrawlCancellationReported;

		/// <summary>
		/// Trigger that fire, when count of crawl pages out of limit pages
		/// </summary>
		protected bool MaxPagesToCrawlLimitReachedOrScheduled;

		#endregion

		/// <summary>
		/// Client-side delegate, which run when crawl decide crawl or not
		/// if all config conditions are succeeded
		/// </summary>
		public Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPageDecisionMaker { get; set; }
		public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldDownloadPageContentDecisionMaker { get; set; }
		public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldCrawlPageLinksDecisionMaker { get; set; }
		public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldRecrawlPageDecisionMaker { get; set; }
		public Func<Uri, CrawledPage, CrawlContext, bool> ShouldScheduleLinkDecisionMaker { get; set; }

		public Func<Uri, Uri, bool> IsInternalDecisionMaker { get; set; } =
			(uriInQuestion, rootUri) => uriInQuestion.Authority == rootUri.Authority;

		#endregion

		#region Ctors

		/// <summary>
		/// Creates a crawler instance with custom settings or implementation.
		/// Passing in null for all params is the equivalent of the empty constructor
		/// </summary>
		/// <param name="crawlConfiguration">Configurable crawl values</param>
		/// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
		/// <param name="threadManager">Distributes http requests over multiple threads</param>
		/// <param name="scheduler">Decides what link should be crawled next</param>
		/// <param name="pageRequester">Makes the raw http requests</param>
		/// <param name="hyperLinkParser">Parses a crawled page for it's hyperlinks</param>
		/// <param name="memoryManager">Checks the memory usage of the host process</param>
		protected WebCrawler(
			CrawlConfiguration crawlConfiguration = null,
			ICrawlDecisionMaker crawlDecisionMaker = null,
			IThreadManager threadManager = null,
			IScheduler scheduler = null,
			IPageRequester pageRequester = null,
			IHyperLinkParser hyperLinkParser = null,
			IMemoryManager memoryManager = null)
		{
			// If crawl configuration wasn't implemented, that try
			// to take it from app config or get default
			CrawlConfiguration config = crawlConfiguration ??
										GetCrawlConfigurationFromConfigFile() ??
										GenerateDefaultCrawlConfiguration();

			// Context with full settings which will used for crawling
			CrawlContext = new CrawlContext
			{
				CrawlConfiguration = config,
				Scheduler = scheduler ??
							new Scheduler(config.IsUriRecrawlingEnabled, null, null),
			};

			// TODO task factory
			//ThreadManager = threadManager ?? new TaskThreadManager(
			//    IsPayAttention(CrawlContext.CrawlConfiguration.MaxConcurrentThreads) ?
			//        CrawlContext.CrawlConfiguration.MaxConcurrentThreads :
			//        Environment.ProcessorCount
			//);

			// Set default if custom is null
			CrawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();
			PageRequester = pageRequester ?? new PageRequester(CrawlContext.CrawlConfiguration);
			HyperLinkParser = hyperLinkParser ?? new HapHyperLinkParser(CrawlContext.CrawlConfiguration, null);

			// TODO this is bad, because if "memoryManager" is not null, so ... what to do?
			if (IsPayAttention(config.MaxMemoryUsageInMb) ||
				IsPayAttention(config.MinAvailableMemoryRequiredInMb))
			{
				MemoryManager = memoryManager ?? new MemoryManager(
									new CachedMemoryMonitor(
										new GcMemoryMonitor(),
										config.MaxMemoryUsageCacheTimeInSeconds
									)
								);
			}
			else if (memoryManager != null)
			{
				Logger.DebugFormat("{0} was found, but Max memory usage or Min available memory not found from {1}", nameof(memoryManager), nameof(config));
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Begins a synchronous crawl using the uri param,
		/// subscribe to events to process data as it becomes available
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public virtual CrawlResult Crawl(Uri uri) => Crawl(uri, null);

		/// <summary>
		/// Begins a synchronous crawl using the uri param,
		/// subscribe to events to process data as it becomes available
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="cancellationTokenSource"></param>
		/// <returns></returns>
		public virtual CrawlResult Crawl(Uri uri, CancellationTokenSource cancellationTokenSource)
		{
			return CrawlSiteUsingUriAsStartPoint(uri, cancellationTokenSource);
		}
		
		#region Synchronous Events

		/// <summary>
		/// Synchronous event that is fired before a page is crawled.
		/// </summary>
		public event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

		/// <summary>
		/// Synchronous event that is fired when an individual page has been crawled.
		/// </summary>
		public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

		/// <summary>
		/// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
		/// </summary>
		public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

		/// <summary>
		/// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
		/// </summary>
		public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

		protected virtual void FirePageCrawlStartingEvent(PageToCrawl pageToCrawl)
		{
			try
			{
				PageCrawlStarting?.Invoke(this, new PageCrawlStartingArgs(CrawlContext, pageToCrawl));
			}
			catch (Exception e)
			{
				Logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlStarting event for url:" + pageToCrawl.Uri.AbsoluteUri);
				Logger.Error(e);
			}
		}

		protected virtual void FirePageCrawlCompletedEvent(CrawledPage crawledPage)
		{
			try
			{
				PageCrawlCompleted?.Invoke(this, new PageCrawlCompletedArgs(CrawlContext, crawledPage));
			}
			catch (Exception e)
			{
				Logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
				Logger.Error(e);
			}
		}

		protected virtual void FirePageCrawlDisallowedEvent(PageToCrawl pageToCrawl, string reason)
		{
			try
			{
				PageCrawlDisallowed?.Invoke(this, new PageCrawlDisallowedArgs(CrawlContext, pageToCrawl, reason));
			}
			catch (Exception e)
			{
				Logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlDisallowed event for url:" + pageToCrawl.Uri.AbsoluteUri);
				Logger.Error(e);
			}
		}

		protected virtual void FirePageLinksCrawlDisallowedEvent(CrawledPage crawledPage, string reason)
		{
			try
			{
				PageLinksCrawlDisallowed?.Invoke(this, new PageLinksCrawlDisallowedArgs(CrawlContext, crawledPage, reason));
			}
			catch (Exception e)
			{
				Logger.Error("An unhandled exception was thrown by a subscriber of the PageLinksCrawlDisallowed event for url:" + crawledPage.Uri.AbsoluteUri);
				Logger.Error(e);
			}
		}

		#endregion

		#region Asynchronous Events

		/// <summary>
		/// Asynchronous event that is fired before a page is crawled.
		/// </summary>
		public event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

		/// <summary>
		/// Asynchronous event that is fired when an individual page has been crawled.
		/// </summary>
		public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

		/// <summary>
		/// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
		/// </summary>
		public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

		/// <summary>
		/// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
		/// </summary>
		public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

		protected virtual void FirePageCrawlStartingEventAsync(PageToCrawl pageToCrawl)
		{
			EventHandler<PageCrawlStartingArgs> threadSafeEvent = PageCrawlStartingAsync;
			if (threadSafeEvent != null)
			{
				//Fire each subscribers delegate async
				foreach (EventHandler<PageCrawlStartingArgs> del in threadSafeEvent.GetInvocationList())
				{
					del.BeginInvoke(this, new PageCrawlStartingArgs(CrawlContext, pageToCrawl), null, null);
				}
			}
		}

		protected virtual void FirePageCrawlCompletedEventAsync(CrawledPage crawledPage)
		{
			EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompletedAsync;

			if (threadSafeEvent == null)
				return;

			if (CrawlContext.Scheduler.Count == 0)
			{
				//Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
				try
				{
					threadSafeEvent(this, new PageCrawlCompletedArgs(CrawlContext, crawledPage));
				}
				catch (Exception e)
				{
					Logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
					Logger.Error(e);
				}
			}
			else
			{
				//Fire each subscribers delegate async
				foreach (EventHandler<PageCrawlCompletedArgs> del in threadSafeEvent.GetInvocationList())
				{
					del.BeginInvoke(this, new PageCrawlCompletedArgs(CrawlContext, crawledPage), null, null);
				}
			}
		}

		protected virtual void FirePageCrawlDisallowedEventAsync(PageToCrawl pageToCrawl, string reason)
		{
			EventHandler<PageCrawlDisallowedArgs> threadSafeEvent = PageCrawlDisallowedAsync;
			if (threadSafeEvent != null)
			{
				//Fire each subscribers delegate async
				foreach (EventHandler<PageCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
				{
					del.BeginInvoke(this, new PageCrawlDisallowedArgs(CrawlContext, pageToCrawl, reason), null, null);
				}
			}
		}

		protected virtual void FirePageLinksCrawlDisallowedEventAsync(CrawledPage crawledPage, string reason)
		{
			EventHandler<PageLinksCrawlDisallowedArgs> threadSafeEvent = PageLinksCrawlDisallowedAsync;
			if (threadSafeEvent != null)
			{
				//Fire each subscribers delegate async
				foreach (EventHandler<PageLinksCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
				{
					del.BeginInvoke(this, new PageLinksCrawlDisallowedArgs(CrawlContext, crawledPage, reason), null, null);
				}
			}
		}

		#endregion

		#endregion

		#region Protected Methods

		/// <summary>
		/// Incapsulate main logic of crawling
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="cancellationTokenSource"></param>
		/// <returns></returns>
		protected virtual CrawlResult CrawlSiteUsingUriAsStartPoint(Uri uri, CancellationTokenSource cancellationTokenSource)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));

			CrawlContext.RootUri = CrawlContext.OriginalRootUri = uri;

			if (cancellationTokenSource != null)
				CrawlContext.CancellationTokenSource = cancellationTokenSource;

			CrawlResult crawlResult = new CrawlResult
			{
				CrawlContext = CrawlContext
			};

			CrawlComplete = false;

			// Print start Log info
			PrintConfigValues(uri);
			ShowStartUsageMemory(uri);

			// Start timers
			CrawlContext.CrawlStartDate = DateTime.Now;
			Stopwatch crawlingTime = Stopwatch.StartNew();

			// TODO check: is it needed here?
			WaitStopTimer();

			// Start crawl site
			TryToCrawlSite(uri, crawlResult);

			// Stop timers
			TimeoutTimer?.Stop();
			crawlingTime.Stop();
			crawlResult.Elapsed = crawlingTime.Elapsed;

			// Print finish Log info
			ShowEndUsageMemory(uri);
			Logger.InfoFormat("Crawl complete for site [{0}]: Crawled [{1}] pages in [{2}]",
				CrawlContext.RootUri.AbsoluteUri, crawlResult.CrawlContext.CrawledCount, crawlResult.Elapsed);

			return crawlResult;
		}

		private void ShowEndUsageMemory(Uri uri)
		{
			if (MemoryManager != null)
			{
				CrawlContext.MemoryUsageAfterCrawlInMb = MemoryManager.GetCurrentUsageInMb();
				Logger.InfoFormat("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, CrawlContext.MemoryUsageAfterCrawlInMb);
			}
		}

		protected virtual void TryToCrawlSite(Uri uri, CrawlResult crawlResult)
		{
			try
			{
				StartCrawlRootPage(uri, crawlResult);
			}
			catch (Exception ex)
			{
				crawlResult.ErrorException = ex;
				Logger.FatalFormat("An error occurred while crawling site [{0}]", uri);
				Logger.Fatal(ex);
			}
			finally
			{
				ThreadManager?.Dispose();
			}
		}

		protected virtual void WaitStopTimer()
		{
			if (IsPayAttention(CrawlContext.CrawlConfiguration.CrawlTimeoutSeconds))
			{
				TimeoutTimer = new Timer(CrawlContext.CrawlConfiguration.CrawlTimeoutSeconds * MillisecondTranslation);
				TimeoutTimer.Elapsed += HandleCrawlTimeout;
				TimeoutTimer.Start();
			}
		}

		protected virtual void ShowStartUsageMemory(Uri uri)
		{
			if (MemoryManager != null)
			{
				CrawlContext.MemoryUsageBeforeCrawlInMb = MemoryManager.GetCurrentUsageInMb();
				Logger.InfoFormat("Starting memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, CrawlContext.MemoryUsageBeforeCrawlInMb);
			}
		}

		protected virtual void StartCrawlRootPage(Uri uri, CrawlResult crawlResult)
		{
			PageToCrawl rootPage = new PageToCrawl(uri)
			{
				IsRoot = true
			};

			// Check, can we crawl this page. If true, then collect to queue
			if (ShouldSchedulePageLink(rootPage))
				CrawlContext.Scheduler.Add(rootPage);

			VerifyRequiredAvailableMemory();

			// Starting crawl root page
			CrawlSite(crawlResult);
		}

		/// <summary>
		/// Main crawl method, where we run our spider to discover this site
		/// in several threads if it available
		/// </summary>
		/// <param name="crawlResult"></param>
		protected virtual void CrawlSite(CrawlResult crawlResult)
		{
			QueueTask parallelTask = new QueueTask();

			parallelTask.Add(() => ProcessPage(CrawlContext.Scheduler.GetNext(), crawlResult));

			while (!CrawlComplete)
			{
				// Check all exceptions and limits
				RunLastPreWorkChecks(crawlResult);

				if (CrawlContext.Scheduler.Count == 0)
				{
					if (parallelTask.Queue.Count == 0)
					{
						Logger.DebugFormat("Crawl was ended. Queue is clear.");
						CrawlComplete = true;
						break;
					}
					else
					{
						Logger.DebugFormat("Waiting for links to be scheduled...");
						parallelTask.WaitTasksComplition();
						continue;
					}
				}

				if (parallelTask.Queue.Count == CrawlContext.Scheduler.Count)
				{
					Logger.DebugFormat("Queue of tasks equal scheduler links. Need wait...");
					parallelTask.WaitTasksComplition();
					continue;
				}

				parallelTask.Add(() => ProcessPage(CrawlContext.Scheduler.GetNext(), crawlResult));
			}
		}

		/// <summary>
		/// Check memory for crawl result object
		/// </summary>
		protected virtual void VerifyRequiredAvailableMemory()
		{
			if (!IsPayAttention(CrawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
				return;

			if (!MemoryManager.IsSpaceAvailable(CrawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
				throw new InsufficientMemoryException(
					$"Process does not have the configured [{CrawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb}mb] " +
					$"of available memory to crawl site [{CrawlContext.RootUri}]. " +
					"This is configurable through the minAvailableMemoryRequiredInMb " +
					"in app.config or CrawlConfiguration.MinAvailableMemoryRequiredInMb."
				);
		}

		/// <summary>
		/// Check all setting limits and finded exceptions
		/// </summary>
		/// <param name="crawlResult">Crawl result</param>
		protected virtual void RunLastPreWorkChecks(CrawlResult crawlResult)
		{
			CheckMemoryUsage(crawlResult);
			CheckForCancellationRequest(crawlResult);
			CheckForHardStopRequest();
			CheckForStopRequest();
		}

		/// <summary>
		/// Control memory for crawl result object
		/// </summary>
		/// <param name="crawlResult">Crawl result</param>
		protected virtual void CheckMemoryUsage(CrawlResult crawlResult)
		{
			if (MemoryManager == null ||
				CrawlContext.IsCrawlHardStopRequested ||
				!IsPayAttention(CrawlContext.CrawlConfiguration.MaxMemoryUsageInMb))
				return;

			int currentMemoryUsage = MemoryManager.GetCurrentUsageInMb();
			Logger.DebugFormat("Current memory usage for site [{0}] is [{1}mb]", CrawlContext.RootUri, currentMemoryUsage);

			if (currentMemoryUsage > CrawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
			{
				MemoryManager.Dispose();
				MemoryManager = null;

				string message =
					$"Process is using [{currentMemoryUsage}mb] of memory which is above the max configured of " +
					$"[{CrawlContext.CrawlConfiguration.MaxMemoryUsageInMb}mb] for site [{CrawlContext.RootUri}]. " +
					"This is configurable through the maxMemoryUsageInMb in app.config or " +
					"CrawlConfiguration.MaxMemoryUsageInMb.";

				crawlResult.ErrorException = new InsufficientMemoryException(message);
				Logger.Fatal(crawlResult.ErrorException);
				CrawlContext.IsCrawlHardStopRequested = true;
			}
		}

		/// <summary>
		/// Check cancellation token request
		/// </summary>
		/// <param name="crawlResult">Crawl result</param>
		protected virtual void CheckForCancellationRequest(CrawlResult crawlResult)
		{
			if (CrawlContext.CancellationTokenSource.IsCancellationRequested && !CrawlCancellationReported)
			{
				string message = $"Crawl cancellation requested for site [{CrawlContext.RootUri}]!";
				Logger.Fatal(message);
				crawlResult.ErrorException =
					new OperationCanceledException(message, CrawlContext.CancellationTokenSource.Token);
				CrawlContext.IsCrawlHardStopRequested = true;
				CrawlCancellationReported = true;
			}
		}

		/// <summary>
		/// Check and run hard stop if needed
		/// </summary>
		protected virtual void CheckForHardStopRequest()
		{
			if (!CrawlContext.IsCrawlHardStopRequested)
				return;

			if (!CrawlStopReported)
			{
				Logger.InfoFormat("Hard crawl stop requested for site [{0}]!", CrawlContext.RootUri);
				CrawlStopReported = true;
			}

			CrawlContext.Scheduler.Clear();

			ThreadManager.AbortAll();
			// To be sure nothing was scheduled since first call to clear()
			CrawlContext.Scheduler.Clear();

			// Set all events to null so no more events are fired
			PageCrawlStarting = null;
			PageCrawlCompleted = null;
			PageCrawlDisallowed = null;
			PageLinksCrawlDisallowed = null;
			PageCrawlStartingAsync = null;
			PageCrawlCompletedAsync = null;
			PageCrawlDisallowedAsync = null;
			PageLinksCrawlDisallowedAsync = null;
		}

		/// <summary>
		/// Check and run stop if needed
		/// </summary>
		protected virtual void CheckForStopRequest()
		{
			if (!CrawlContext.IsCrawlStopRequested)
				return;

			if (!CrawlStopReported)
			{
				Logger.InfoFormat("Crawl stop requested for site [{0}]!", CrawlContext.RootUri);
				CrawlStopReported = true;
			}

			CrawlContext.Scheduler.Clear();
		}

		/// <summary>
		/// Event method for timeouting between 2 crawl operations
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void HandleCrawlTimeout(object sender, ElapsedEventArgs e)
		{
			if (sender is Timer elapsedTimer)
				elapsedTimer.Stop();

			Logger.InfoFormat("Crawl timeout of [{0}] seconds has been reached for [{1}]", CrawlContext.CrawlConfiguration.CrawlTimeoutSeconds, CrawlContext.RootUri);
			CrawlContext.IsCrawlHardStopRequested = true;
		}

		//protected virtual async Task ProcessPage(PageToCrawl pageToCrawl)

		/// <summary>
		/// Process for crawling page
		/// </summary>
		/// <param name="pageToCrawl"></param>
		/// <param name="crawlResult"></param>
		protected virtual async Task ProcessPage(PageToCrawl pageToCrawl, CrawlResult crawlResult)
		{
			try
			{
				if (pageToCrawl == null)
					return;

				ThrowIfCancellationRequested();

				AddPageToContext(pageToCrawl);

				// Crawl page
				CrawledPage crawledPage = await CrawlThePage(pageToCrawl);

				// Validate the root uri in case of a redirection.
				if (crawledPage.IsRoot)
					ValidateRootUriForRedirection(crawledPage);

				if (IsRedirect(crawledPage) &&
				   !CrawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
					ProcessRedirect(crawledPage);

				if (PageSizeIsAboveMax(crawledPage))
					return;

				ThrowIfCancellationRequested();

				// Parse crawled page
				bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
				if (shouldCrawlPageLinks || CrawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
					ParsePageLinks(crawledPage);

				ThrowIfCancellationRequested();

				if (shouldCrawlPageLinks)
					SchedulePageLinks(crawledPage);

				ThrowIfCancellationRequested();

				FirePageCrawlCompletedEventAsync(crawledPage);
				FirePageCrawlCompletedEvent(crawledPage);

				if (ShouldRecrawlPage(crawledPage))
				{
					crawledPage.IsRetry = true;
					CrawlContext.Scheduler.Add(crawledPage);
				}
			}
			catch (OperationCanceledException)
			{
				Logger.DebugFormat("Thread cancelled while crawling/processing page [{0}]", pageToCrawl.Uri);
				throw;
			}
			catch (Exception e)
			{
				crawlResult.ErrorException = e;
				Logger.FatalFormat("Error occurred during processing of page [{0}]", pageToCrawl.Uri);
				Logger.Fatal(e);

				CrawlContext.IsCrawlHardStopRequested = true;
			}
		}

		/// <summary>
		/// Redirect crawl page due to request
		/// </summary>
		/// <param name="crawledPage"></param>
		protected virtual void ProcessRedirect(CrawledPage crawledPage)
		{
			if (crawledPage.RedirectPosition >= 20)
				Logger.WarnFormat("Page [{0}] is part of a chain of 20 or more consecutive " +
								  "redirects, redirects for this chain will now be aborted.",
									crawledPage.Uri);

			try
			{
				Uri uri = ExtractRedirectUri(crawledPage);

				PageToCrawl page = new PageToCrawl(uri)
				{
					ParentUri = crawledPage.ParentUri,
					CrawlDepth = crawledPage.CrawlDepth,
					IsInternal = IsInternalUri(uri),
					IsRoot = false,
					RedirectedFrom = crawledPage,
					RedirectPosition = crawledPage.RedirectPosition + 1
				};

				crawledPage.RedirectedTo = page;
				Logger.DebugFormat("Page [{0}] is requesting that it be redirect to [{1}]", crawledPage.Uri, crawledPage.RedirectedTo.Uri);

				if (ShouldSchedulePageLink(page))
				{
					Logger.InfoFormat("Page [{0}] will be redirect to [{1}]", crawledPage.Uri, crawledPage.RedirectedTo.Uri);
					CrawlContext.Scheduler.Add(page);
				}
			}
			catch (Exception e)
			{
				Logger.Error(e);
				throw e;
			}
		}

		/// <summary>
		/// Check uri is internal
		/// </summary>
		/// <param name="uri">Current uri</param>
		/// <returns>Bool</returns>
		protected virtual bool IsInternalUri(Uri uri)
		{
			return IsInternalDecisionMaker(uri, CrawlContext.RootUri) ||
				   IsInternalDecisionMaker(uri, CrawlContext.OriginalRootUri);
		}

		/// <summary>
		/// Check current page is redirect
		/// </summary>
		/// <param name="crawledPage"></param>
		/// <returns></returns>
		protected virtual bool IsRedirect(CrawledPage crawledPage)
		{
			bool isRedirect = false;

			if (crawledPage.HttpWebResponse != null)
			{
				isRedirect = (CrawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
							  crawledPage.HttpWebResponse.ResponseUri != null &&
							  crawledPage.HttpWebResponse.ResponseUri.AbsoluteUri != crawledPage.Uri.AbsoluteUri) ||
							  (!CrawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
							  (int)crawledPage.HttpWebResponse.StatusCode >= 300 &&
							  (int)crawledPage.HttpWebResponse.StatusCode <= 399);
			}

			return isRedirect;
		}

		/// <summary>
		/// Run throw if cancellation requested
		/// </summary>
		protected virtual void ThrowIfCancellationRequested()
		{
			if (CrawlContext.CancellationTokenSource != null &&
			   CrawlContext.CancellationTokenSource.IsCancellationRequested)
			{
				CrawlContext.CancellationTokenSource.Token.ThrowIfCancellationRequested();
			}
		}

		/// <summary>
		/// Check page size by config values
		/// </summary>
		/// <param name="crawledPage">Page</param>
		/// <returns>Bool</returns>
		protected virtual bool PageSizeIsAboveMax(CrawledPage crawledPage)
		{
			bool isAboveMax = false;

			if (CrawlContext.CrawlConfiguration.MaxPageSizeInBytes > 0 &&
				crawledPage.Content.Bytes != null &&
				crawledPage.Content.Bytes.Length > CrawlContext.CrawlConfiguration.MaxPageSizeInBytes)
			{
				isAboveMax = true;
				Logger.InfoFormat("Page [{0}] has a page size of [{1}] bytes which is above the [{2}] " +
								  "byte max, no further processing will occur for this page",
								  crawledPage.Uri,
								  crawledPage.Content.Bytes.Length,
								  CrawlContext.CrawlConfiguration.MaxPageSizeInBytes);
			}

			return isAboveMax;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="crawledPage"></param>
		/// <returns></returns>
		protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
		{
			CrawlDecision shouldCrawlPageLinksDecision = CrawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, CrawlContext);

			if (shouldCrawlPageLinksDecision.Allow)
				shouldCrawlPageLinksDecision = ShouldCrawlPageLinksDecisionMaker != null ?
					ShouldCrawlPageLinksDecisionMaker.Invoke(crawledPage, CrawlContext) :
					new CrawlDecision { Allow = true };

			if (!shouldCrawlPageLinksDecision.Allow)
			{
				Logger.DebugFormat("Links on page [{0}] not crawled, [{1}]",
					crawledPage.Uri.AbsoluteUri, shouldCrawlPageLinksDecision.Reason);

				FirePageLinksCrawlDisallowedEventAsync(crawledPage, shouldCrawlPageLinksDecision.Reason);
				FirePageLinksCrawlDisallowedEvent(crawledPage, shouldCrawlPageLinksDecision.Reason);
			}

			StopCrawlIfDecisionRequare(shouldCrawlPageLinksDecision);
			return shouldCrawlPageLinksDecision.Allow;
		}

		/// <summary>
		/// Get access to schedule the page
		/// </summary>
		/// <param name="pageToCrawl">Page to crawl</param>
		/// <returns>Bool</returns>
		protected virtual bool ShouldSchedulePageLink(PageToCrawl pageToCrawl)
		{
			if ((pageToCrawl.IsInternal || CrawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled) &&
				ShouldCrawlPage(pageToCrawl))
				return true;

			return false;
		}

		/// <summary>
		/// Check settings from config to get access crawl the page
		/// </summary>
		/// <param name="pageToCrawl">Page to crawl</param>
		/// <returns>Bool</returns>
		protected virtual bool ShouldCrawlPage(PageToCrawl pageToCrawl)
		{
			if (MaxPagesToCrawlLimitReachedOrScheduled)
				return false;

			// Decide crawl page or not
			CrawlDecision shouldCrawlPageDecision = CrawlDecisionMaker.ShouldCrawlPage(pageToCrawl, CrawlContext);

			if (!CheckPageCountDecisionReason(shouldCrawlPageDecision))
				return false;

			if (shouldCrawlPageDecision.Allow && ShouldCrawlPageDecisionMaker != null)
				shouldCrawlPageDecision = ShouldCrawlPageDecisionMaker(pageToCrawl, CrawlContext);

			if (!shouldCrawlPageDecision.Allow)
			{
				Logger.DebugFormat("Page [{0}] not crawled, Reason: [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);

				// TODO nice event calling
				FirePageCrawlDisallowedEventAsync(pageToCrawl, shouldCrawlPageDecision.Reason);
				FirePageCrawlDisallowedEvent(pageToCrawl, shouldCrawlPageDecision.Reason);
			}

			StopCrawlIfDecisionRequare(shouldCrawlPageDecision);

			return shouldCrawlPageDecision.Allow;
		}

		protected virtual bool CheckPageCountDecisionReason(CrawlDecision shouldCrawlPageDecision)
		{
			if (!shouldCrawlPageDecision.Allow &&
				shouldCrawlPageDecision.Reason.Contains("MaxPagesToCrawl limit of"))
			{
				Logger.Info("MaxPagesToCrawlLimit has been reached or scheduled. No more pages will be scheduled.");
				MaxPagesToCrawlLimitReachedOrScheduled = true;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Recrawl page taking "Retry-After" from header if it possible
		/// </summary>
		/// <param name="crawledPage">Not crawled page</param>
		/// <returns>Can recrawl after "Retry-After" or config time?</returns>
		protected virtual bool ShouldRecrawlPage(CrawledPage crawledPage)
		{
			CrawlDecision shouldRecrawlPageDecision = CrawlDecisionMaker.ShouldRecrawlPage(crawledPage, CrawlContext);

			if (shouldRecrawlPageDecision.Allow)
			{
				shouldRecrawlPageDecision = ShouldRecrawlPageDecisionMaker != null ?
					ShouldRecrawlPageDecisionMaker.Invoke(crawledPage, CrawlContext) :
					new CrawlDecision { Allow = true };
			}

			if (!shouldRecrawlPageDecision.Allow)
			{
				Logger.DebugFormat("Page [{0}] not recrawled, [{1}]",
					crawledPage.Uri.AbsoluteUri, shouldRecrawlPageDecision.Reason);
			}
			else
			{
				// Look for the Retry-After header in the response.
				crawledPage.RetryAfter = null;

				if (crawledPage.HttpWebResponse != null &&
					crawledPage.HttpWebResponse.Headers != null)
				{
					string value = crawledPage.HttpWebResponse.GetResponseHeader("Retry-After");

					if (!String.IsNullOrEmpty(value))
					{
						// Try to convert to DateTime first, then in double.
						if (crawledPage.LastRequestTime.HasValue &&
							DateTime.TryParse(value, out DateTime date))
						{
							crawledPage.RetryAfter = (date - crawledPage.LastRequestTime.Value).TotalSeconds;
						}
						else if (double.TryParse(value, out double seconds))
						{
							crawledPage.RetryAfter = seconds;
						}
					}
				}
			}

			StopCrawlIfDecisionRequare(shouldRecrawlPageDecision);
			return shouldRecrawlPageDecision.Allow;
		}

		//protected virtual async Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl)

		/// <summary>
		/// Transform <see cref="PageToCrawl"/> to <see cref="CrawledPage"/>
		/// by creating request
		/// </summary>
		/// <param name="pageToCrawl"></param>
		/// <returns></returns>
		protected virtual async Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl)
		{
			Logger.DebugFormat("About to crawl page [{0}]", pageToCrawl.Uri.AbsoluteUri);

			// TODO nice events
			FirePageCrawlStartingEventAsync(pageToCrawl);
			FirePageCrawlStartingEvent(pageToCrawl);

			if (pageToCrawl.IsRetry)
				WaitMinimumRetryDelay(pageToCrawl);

			pageToCrawl.LastRequestTime = DateTime.Now;

			CrawledPage crawledPage = await PageRequester.MakeRequestAsync(pageToCrawl.Uri, ShouldDownloadPageContent);

			MapPageToCrawlToCrawledPage(pageToCrawl, crawledPage);

			if (crawledPage.HttpWebResponse == null)
			{
				Logger.InfoFormat("Page crawl complete, Status:[NA] Url:[{0}] Elapsed:[{1}] Parent:[{2}] Retry:[{3}]",
					crawledPage.Uri.AbsoluteUri,
					crawledPage.Elapsed,
					crawledPage.ParentUri,
					crawledPage.RetryCount);
			}
			else
			{
				Logger.InfoFormat("Page crawl complete, Status:[{0}] Url:[{1}] Elapsed:[{2}] Parent:[{3}] Retry:[{4}]",
					Convert.ToInt32(crawledPage.HttpWebResponse.StatusCode),
					crawledPage.Uri.AbsoluteUri,
					crawledPage.Elapsed,
					crawledPage.ParentUri,
					crawledPage.RetryCount);
			}

			return crawledPage;
		}

		/// <summary>
		/// Map <see cref="PageToCrawl"/> to <see cref="CrawledPage"/>
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		protected void MapPageToCrawlToCrawledPage(PageToCrawl src, CrawledPage dest)
		{
			dest.Uri = src.Uri;
			dest.ParentUri = src.ParentUri;
			dest.IsRetry = src.IsRetry;
			dest.RetryAfter = src.RetryAfter;
			dest.RetryCount = src.RetryCount;
			dest.LastRequestTime = src.LastRequestTime;
			dest.IsRoot = src.IsRoot;
			dest.IsInternal = src.IsInternal;
			dest.PageBag = CombinePageBags(src.PageBag, dest.PageBag);
			dest.CrawlDepth = src.CrawlDepth;
			dest.RedirectedFrom = src.RedirectedFrom;
			dest.RedirectPosition = src.RedirectPosition;
		}

		/// <summary>
		/// Combiner for PageBag-s of page for crawl and crawled page
		/// </summary>
		/// <param name="pageToCrawlBag"></param>
		/// <param name="crawledPageBag"></param>
		/// <returns>Combined PageBag</returns>
		protected virtual dynamic CombinePageBags(dynamic pageToCrawlBag, dynamic crawledPageBag)
		{
			IDictionary<string, object> combinedBag = new ExpandoObject();
			var pageToCrawlBagDict = pageToCrawlBag as IDictionary<string, object>;
			var crawledPageBagDict = crawledPageBag as IDictionary<string, object>;

			foreach (KeyValuePair<string, object> entry in pageToCrawlBagDict) combinedBag[entry.Key] = entry.Value;
			foreach (KeyValuePair<string, object> entry in crawledPageBagDict) combinedBag[entry.Key] = entry.Value;

			return combinedBag;
		}

		/// <summary>
		/// Collect crawling pages to <see cref="Poco.CrawlContext.CrawlCountByDomain"/>
		/// </summary>
		/// <param name="pageToCrawl"></param>
		protected virtual void AddPageToContext(PageToCrawl pageToCrawl)
		{
			if (pageToCrawl.IsRetry)
			{
				pageToCrawl.RetryCount++;
				return;
			}

			Interlocked.Increment(ref CrawlContext.CrawledCount);
			CrawlContext.CrawlCountByDomain.AddOrUpdate(pageToCrawl.Uri.Authority, 1, (key, oldValue) => oldValue + 1);
		}

		/// <summary>
		/// Parse page to links by hyperlink parser
		/// </summary>
		/// <param name="crawledPage"></param>
		protected virtual void ParsePageLinks(CrawledPage crawledPage)
		{
			crawledPage.ParsedLinks = HyperLinkParser.GetLinks(crawledPage);
		}

		/// <summary>
		/// Validate and schedule links for non duplicate crawling pages
		/// </summary>
		/// <param name="crawledPage">Crawled page</param>
		protected virtual void SchedulePageLinks(CrawledPage crawledPage)
		{
			int linksToCrawl = 0;
			foreach (Uri uri in crawledPage.ParsedLinks)
			{
				// First validate that the link was not already visited or added
				// to the list of pages to visit, so we don't
				// make the same validation and fire the same events twice.
				if (!CrawlContext.Scheduler.IsUriKnown(uri) &&
					(ShouldScheduleLinkDecisionMaker == null || ShouldScheduleLinkDecisionMaker.Invoke(uri, crawledPage, CrawlContext)))
				{
					// Added due to a bug in the Uri class related to this
					// http://stackoverflow.com/questions/2814951/system-uriformatexception-invalid-uri-the-hostname-could-not-be-parsed
					try
					{
						PageToCrawl page = new PageToCrawl(uri)
						{
							ParentUri = crawledPage.Uri,
							CrawlDepth = crawledPage.CrawlDepth + 1,
							IsInternal = IsInternalUri(uri),
							IsRoot = false
						};

						if (ShouldSchedulePageLink(page))
						{
							CrawlContext.Scheduler.Add(page);
							linksToCrawl++;
						}

						if (!ShouldScheduleMorePageLink(linksToCrawl))
						{
							Logger.InfoFormat("MaxLinksPerPage has been reached. No more links will be " +
											  "scheduled for current page [{0}].", crawledPage.Uri);
							break;
						}
					}
					catch (UriFormatException)
					{
						Logger.WarnFormat("Bug in Uri class with System.UriFormatException was discovered. Something can go wrong");
					}
				}

				// Add this link to the list of known Urls so
				// validations are not duplicated in the future.
				CrawlContext.Scheduler.AddKnownUri(uri);
			}
		}

		/// <summary>
		/// Should schedule more page link
		/// </summary>
		/// <param name="linksAdded"></param>
		/// <returns></returns>
		protected virtual bool ShouldScheduleMorePageLink(int linksAdded)
		{
			return CrawlContext.CrawlConfiguration.MaxLinksPerPage == 0 ||
				   CrawlContext.CrawlConfiguration.MaxLinksPerPage > linksAdded;
		}

		/// <summary>
		/// Check need download page content
		/// </summary>
		/// <param name="crawledPage">Crawled page</param>
		/// <returns></returns>
		protected virtual CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage)
		{
			CrawlDecision decision = CrawlDecisionMaker.ShouldDownloadPageContent(crawledPage, CrawlContext);

			if (decision.Allow)
				decision = ShouldDownloadPageContentDecisionMaker != null ?
					ShouldDownloadPageContentDecisionMaker.Invoke(crawledPage, CrawlContext) :
					new CrawlDecision { Allow = true };

			StopCrawlIfDecisionRequare(decision);
			return decision;
		}

		/// <summary>
		/// Log all info from crawl config
		/// </summary>
		/// <param name="uri"></param>
		protected virtual void PrintConfigValues(Uri uri)
		{
			Logger.InfoFormat("About to crawl site [{0}]", uri.AbsoluteUri);
			Logger.Info("Configuration Values:");

			string indentString = new string(' ', 2);
			string abotVersion = Assembly.GetAssembly(this.GetType()).GetName().Version.ToString();
			Logger.InfoFormat("{0}Abot Version: {1}", indentString, abotVersion);

			foreach (PropertyInfo property in CrawlContext.CrawlConfiguration.GetType().GetProperties())
			{
				if (property.Name != "ConfigurationExtensions")
					Logger.InfoFormat("{0}{1}: {2}", indentString, property.Name, property.GetValue(CrawlContext.CrawlConfiguration, null));
			}

			foreach (string key in CrawlContext.CrawlConfiguration.ConfigurationExtensions.Keys)
			{
				Logger.InfoFormat("{0}{1}: {2}", indentString, key, CrawlContext.CrawlConfiguration.ConfigurationExtensions[key]);
			}
		}

		/// <summary>
		/// Check decision for stop signals
		/// </summary>
		/// <param name="decision"></param>
		protected virtual void StopCrawlIfDecisionRequare(CrawlDecision decision)
		{
			if (decision.ShouldHardStopCrawl)
			{
				Logger.InfoFormat("Decision marked crawl [Hard Stop] for site [{0}], [{1}]",
					CrawlContext.RootUri, decision.Reason);

				CrawlContext.IsCrawlHardStopRequested = decision.ShouldHardStopCrawl;
			}
			else if (decision.ShouldStopCrawl)
			{
				Logger.InfoFormat("Decision marked crawl [Stop] for site [{0}], [{1}]",
					CrawlContext.RootUri, decision.Reason);

				CrawlContext.IsCrawlStopRequested = decision.ShouldStopCrawl;
			}
		}

		/// <summary>
		/// Sleep retry delay
		/// </summary>
		/// <param name="pageToCrawl"></param>
		protected virtual void WaitMinimumRetryDelay(PageToCrawl pageToCrawl)
		{
			//TODO No unit tests cover these lines
			if (pageToCrawl.LastRequestTime == null)
			{
				Logger.WarnFormat("pageToCrawl.LastRequest value is null for Url:{0}. Cannot retry without this value.",
					pageToCrawl.Uri.AbsoluteUri);

				return;
			}

			double milliSinceLastRequest = (DateTime.Now - pageToCrawl.LastRequestTime.Value).TotalMilliseconds;
			double milliToWait;
			if (pageToCrawl.RetryAfter.HasValue)
			{
				// Use the time to wait provided by the server instead of the config, if any.
				milliToWait = pageToCrawl.RetryAfter.Value * MillisecondTranslation - milliSinceLastRequest;
			}
			else
			{
				if (milliSinceLastRequest > CrawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds)
					return;

				milliToWait = CrawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds - milliSinceLastRequest;
			}

			Logger.InfoFormat("Waiting [{0}] milliseconds before retrying Url:[{1}] LastRequest:[{2}] SoonestNextRequest:[{3}]",
				milliToWait,
				pageToCrawl.Uri.AbsoluteUri,
				pageToCrawl.LastRequestTime,
				pageToCrawl.LastRequestTime.Value.AddMilliseconds(CrawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds));

			// TODO Cannot use RateLimiter since it currently cannot handle dynamic sleep times so using Thread.Sleep in the meantime
			if (milliToWait > 0)
				Thread.Sleep(TimeSpan.FromMilliseconds(milliToWait));
		}

		/// <summary>
		/// Validate that the Root page was not redirected. If the root page is redirected, we assume that the root uri
		/// should be changed to the uri where it was redirected.
		/// </summary>
		protected virtual void ValidateRootUriForRedirection(CrawledPage crawledRootPage)
		{
			if (!crawledRootPage.IsRoot)
			{
				throw new ArgumentException("The crawled page must be the root page to be validated for redirection.");
			}

			if (IsRedirect(crawledRootPage))
			{
				CrawlContext.RootUri = ExtractRedirectUri(crawledRootPage);
				Logger.InfoFormat("The root URI [{0}] was redirected to [{1}]. [{1}] is the new root.",
					CrawlContext.OriginalRootUri,
					CrawlContext.RootUri);
			}
		}

		/// <summary>
		/// Retrieve the URI where the specified crawled page was redirected.
		/// </summary>
		/// <remarks>
		/// If HTTP auto redirections is disabled, this value is stored in the 'Location' header of the response.
		/// If auto redirections is enabled, this value is stored in the response's ResponseUri property.
		/// </remarks>
		protected virtual Uri ExtractRedirectUri(CrawledPage crawledPage)
		{
			Uri locationUri;

			if (CrawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
			{
				// For auto redirects, look for the response uri.
				locationUri = crawledPage.HttpWebResponse.ResponseUri;
			}
			else
			{
				// For manual redirects, we need to look for the location header.
				var location = crawledPage.HttpWebResponse.Headers["Location"];

				// Check if the location is absolute. If not, create an absolute uri.
				if (!Uri.TryCreate(location, UriKind.Absolute, out locationUri))
				{
					Uri baseUri = new Uri(crawledPage.Uri.GetLeftPart(UriPartial.Authority));
					locationUri = new Uri(baseUri, location);
				}
			}

			return locationUri;
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public virtual void Dispose()
		{
			ThreadManager?.Dispose();
			CrawlContext?.Dispose();
			PageRequester?.Dispose();
			MemoryManager?.Dispose();
		}

		#endregion

		#region Private Methods

		private CrawlConfiguration GetCrawlConfigurationFromConfigFile()
		{
			AbotConfigurationSectionHandler configFromFile = AbotConfigurationSectionHandler.LoadFromXml();

			if (configFromFile != null)
			{
				CrawlConfiguration configuration = configFromFile.Convert();

				Logger.DebugFormat("Crawl configuration: abot config section was found");
				return configuration;
			}

			Logger.DebugFormat("Crawl configuration: abot config section was NOT found");
			return null;
		}

		private CrawlConfiguration GenerateDefaultCrawlConfiguration()
		{
			Logger.DebugFormat("Crawl configuration: Generate default");
			return new CrawlConfiguration()
			{
				// TODO Some default values
			};
		}

		#endregion
	}
}