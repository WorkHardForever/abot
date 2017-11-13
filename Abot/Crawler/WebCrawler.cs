using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.Threading;
using System.Timers;
using Abot.Core;
using Abot.Poco;
using Abot.Util;
using log4net;
using Timer = System.Timers.Timer;

namespace Abot.Crawler
{
	/// <summary>
	/// Base crawler of the library
	/// </summary>
	[Serializable]
	public abstract class WebCrawler : IWebCrawler
	{
		#region Const

		/// <summary>
		/// Many config values start work, when its more then zero
		/// </summary>
		public const int c_NOT_PAY_ATTENTION = 0;

		/// <summary>
		/// Value for translation seconds to milliseconds
		/// </summary>
		public const int c_MILLISECOND_TRANSLATION = 1000;

		#endregion

		#region Protected Fields

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger { get { return _logger.Value; } }
		private Lazy<ILog> _logger = new Lazy<ILog>(() => LogManager.GetLogger(CrawlConfiguration.LoggerName));

		/// <summary>
		/// Config for crawling
		/// </summary>
		protected CrawlContext _crawlContext;

		/// <summary>
		/// Trigger that fire, when crawl is over
		/// </summary>
		protected bool _crawlComplete;

		/// <summary>
		/// Trigger that fire, when crawl should stop working
		/// </summary>
		protected bool _crawlStopReported;

		/// <summary>
		/// Trigger that fire, when was cancellation request
		/// </summary>
		protected bool _crawlCancellationReported;

		/// <summary>
		/// Trigger that fire, when count of crawl pages out of limit pages
		/// </summary>
		protected bool _maxPagesToCrawlLimitReachedOrScheduled;

		/// <summary>
		/// Time for waiting between 2 crawling operations.
		/// Requare when site block fast crawling pages
		/// </summary>
		protected Timer _timeoutTimer;

		/// <summary>
		/// Decides whether or not to crawl a page or that page's links
		/// </summary>
		protected ICrawlDecisionMaker _crawlDecisionMaker;

		/// <summary>
		/// Distributes http requests over multiple threads
		/// </summary>
		protected IThreadManager _threadManager;

		/// <summary>
		/// Decides what link should be crawled next
		/// </summary>
		protected IScheduler _scheduler;

		/// <summary>
		/// Makes the raw http requests
		/// </summary>
		protected IPageRequester _pageRequester;

		/// <summary>
		/// Parses a crawled page for it's hyperlinks
		/// </summary>
		protected IHyperLinkParser _hyperLinkParser;

		/// <summary>
		/// Checks the memory usage of the host process
		/// </summary>
		protected IMemoryManager _memoryManager;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		protected Func<PageToCrawl, CrawlContext, CrawlDecision> _shouldCrawlPageDecisionMaker;
		protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldDownloadPageContentDecisionMaker;
		protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldCrawlPageLinksDecisionMaker;
		protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldRecrawlPageDecisionMaker;
		protected Func<Uri, CrawledPage, CrawlContext, bool> _shouldScheduleLinkDecisionMaker;

		protected Func<Uri, Uri, bool> _isInternalDecisionMaker =
			(uriInQuestion, rootUri) => uriInQuestion.Authority == rootUri.Authority;

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Dynamic object that can hold any value that needs to be available in the crawl context
		/// </summary>
		public dynamic CrawlBag { get; set; }

		#endregion

		#region Ctors

		static WebCrawler()
		{
			// This is a workaround for dealing with periods in urls
			// http://stackoverflow.com/questions/856885/httpwebrequest-to-url-with-dot-at-the-end
			// Will not be needed when this project is upgraded to 4.5

			MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax", BindingFlags.Static | BindingFlags.NonPublic);
			FieldInfo flagsField = typeof(UriParser).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);

			if (getSyntax != null && flagsField != null)
			{
				foreach (string scheme in new[] { "http", "https" })
				{
					UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
					if (parser != null)
					{
						int flagsValue = (int)flagsField.GetValue(parser);
						// Clear the CanonicalizeAsFilePath attribute
						if ((flagsValue & 0x1000000) != 0)
							flagsField.SetValue(parser, flagsValue & ~0x1000000);
					}
				}
			}
		}

		/// <summary>
		/// Creates a crawler instance with the default settings and implementations.
		/// </summary>
		public WebCrawler()
			: this(null, null, null, null, null, null, null)
		{ }

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
		public WebCrawler(
			CrawlConfiguration crawlConfiguration,
			ICrawlDecisionMaker crawlDecisionMaker,
			IThreadManager threadManager,
			IScheduler scheduler,
			IPageRequester pageRequester,
			IHyperLinkParser hyperLinkParser,
			IMemoryManager memoryManager)
		{
			// Initialize configuration with logger
			CrawlConfiguration configuration = crawlConfiguration ??
					GetCrawlConfigurationFromConfigFile() ??
					GenerateDefaultCrawlConfiguration();

			// If crawl configuration wasn't implemented, that try
			// to take it from app config or get default
			_crawlContext = new CrawlContext
			{
				CrawlConfiguration = configuration
			};

			CrawlBag = _crawlContext.CrawlBag;

			_threadManager = threadManager ?? new TaskThreadManager(
				IsPayAttention(_crawlContext.CrawlConfiguration.MaxConcurrentThreads) ?
					_crawlContext.CrawlConfiguration.MaxConcurrentThreads :
					Environment.ProcessorCount
			);
			_scheduler = scheduler ?? new Scheduler(_crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled, null, null);
			_pageRequester = pageRequester ?? new PageRequester(_crawlContext.CrawlConfiguration);
			_crawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();

			if (IsPayAttention(_crawlContext.CrawlConfiguration.MaxMemoryUsageInMb) ||
				IsPayAttention(_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
				_memoryManager = memoryManager ?? new MemoryManager(
					new CachedMemoryMonitor(
						new GcMemoryMonitor(),
						_crawlContext.CrawlConfiguration.MaxMemoryUsageCacheTimeInSeconds
					)
				);

			_hyperLinkParser = hyperLinkParser ?? new HapHyperLinkParser(_crawlContext.CrawlConfiguration, null);

			_crawlContext.Scheduler = _scheduler;
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
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));

			_crawlContext.RootUri = _crawlContext.OriginalRootUri = uri;

			if (cancellationTokenSource != null)
				_crawlContext.CancellationTokenSource = cancellationTokenSource;

			CrawlResult crawlResult = new CrawlResult
			{
				RootUri = _crawlContext.RootUri,
				CrawlContext = _crawlContext
			};

			_crawlComplete = false;

			// Print config
			Logger.InfoFormat("About to crawl site [{0}]", uri.AbsoluteUri);
			PrintConfigValues(_crawlContext.CrawlConfiguration);

			if (_memoryManager != null)
			{
				_crawlContext.MemoryUsageBeforeCrawlInMb = _memoryManager.GetCurrentUsageInMb();
				Logger.InfoFormat("Starting memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageBeforeCrawlInMb);
			}

			_crawlContext.CrawlStartDate = DateTime.Now;
			Stopwatch timer = Stopwatch.StartNew();

			if (IsPayAttention(_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds))
			{
				_timeoutTimer = new Timer(_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds * c_MILLISECOND_TRANSLATION);
				_timeoutTimer.Elapsed += HandleCrawlTimeout;
				_timeoutTimer.Start();
			}

			try
			{
				PageToCrawl rootPage = new PageToCrawl(uri)
				{
					ParentUri = uri,
					IsInternal = true,
					IsRoot = true
				};

				// Check, can we crawl this page. If true, then collect to queue
				if (ShouldSchedulePageLink(rootPage))
					_scheduler.Add(rootPage);

				VerifyRequiredAvailableMemory();

				// Starting crawl root page
				CrawlSite(crawlResult);
			}
			catch (Exception e)
			{
				crawlResult.ErrorException = e;
				Logger.FatalFormat("An error occurred while crawling site [{0}]", uri);
				Logger.Fatal(e);
			}
			finally
			{
				if (_threadManager != null)
					_threadManager.Dispose();
			}

			if (_timeoutTimer != null)
				_timeoutTimer.Stop();

			timer.Stop();

			if (_memoryManager != null)
			{
				_crawlContext.MemoryUsageAfterCrawlInMb = _memoryManager.GetCurrentUsageInMb();
				Logger.InfoFormat("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageAfterCrawlInMb);
			}

			crawlResult.Elapsed = timer.Elapsed;
			Logger.InfoFormat("Crawl complete for site [{0}]: Crawled [{1}] pages in [{2}]", crawlResult.RootUri.AbsoluteUri, crawlResult.CrawlContext.CrawledCount, crawlResult.Elapsed);

			return crawlResult;
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
				PageCrawlStarting?.Invoke(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl));
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
				PageCrawlCompleted?.Invoke(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
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
				PageCrawlDisallowed?.Invoke(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason));
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
				PageLinksCrawlDisallowed?.Invoke(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason));
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
					del.BeginInvoke(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl), null, null);
				}
			}
		}

		protected virtual void FirePageCrawlCompletedEventAsync(CrawledPage crawledPage)
		{
			EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompletedAsync;

			if (threadSafeEvent == null)
				return;

			if (_scheduler.Count == 0)
			{
				//Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
				try
				{
					threadSafeEvent(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
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
					del.BeginInvoke(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage), null, null);
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
					del.BeginInvoke(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason), null, null);
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
					del.BeginInvoke(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason), null, null);
				}
			}
		}

		#endregion
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a page should be crawled or not
		/// </summary>
		public void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker)
		{
			_shouldCrawlPageDecisionMaker = decisionMaker;
		}

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether the page's content should be dowloaded
		/// </summary>
		/// <param name="decisionMaker"></param>
		public void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
		{
			_shouldDownloadPageContentDecisionMaker = decisionMaker;
		}

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a page's links should be crawled or not
		/// </summary>
		/// <param name="decisionMaker"></param>
		public void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
		{
			_shouldCrawlPageLinksDecisionMaker = decisionMaker;
		}

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a cerain link on a page should be scheduled to be crawled
		/// </summary>
		public void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker)
		{
			_shouldScheduleLinkDecisionMaker = decisionMaker;
		}

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether a page should be recrawled or not
		/// </summary>
		public void ShouldRecrawlPage(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
		{
			_shouldRecrawlPageDecisionMaker = decisionMaker;
		}

		/// <summary>
		/// Synchronous method that registers a delegate to be called to determine whether the 1st uri param is considered an internal uri to the second uri param
		/// </summary>
		/// <param name="decisionMaker delegate"></param>     
		public void IsInternalUri(Func<Uri, Uri, bool> decisionMaker)
		{
			_isInternalDecisionMaker = decisionMaker;
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Main crawl method, where we run our spider to discover this site
		/// in several threads if it available
		/// </summary>
		/// <param name="crawlResult"></param>
		protected virtual void CrawlSite(CrawlResult crawlResult)
		{
			while (!_crawlComplete)
			{
				// Check all exceptions and limits
				RunPreWorkChecks(crawlResult);

				if (_scheduler.Count > 0)
				{
					// Run crawling method multi-thread
					_threadManager.DoWork(() => ProcessPage(_scheduler.GetNext(), crawlResult));
				}
				else if (!_threadManager.HasRunningThreads())
				{
					_crawlComplete = true;
				}
				else
				{
					Logger.DebugFormat("Waiting for links to be scheduled...");
					Thread.Sleep(2500);
				}
			}
		}

		/// <summary>
		/// Check memory for crawl result object
		/// </summary>
		protected virtual void VerifyRequiredAvailableMemory()
		{
			if (_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb < 1)
				return;

			if (!_memoryManager.IsSpaceAvailable(_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
				throw new InsufficientMemoryException(
					string.Format("Process does not have the configured [{0}mb] of available memory to crawl site [{1}]. " +
								  "This is configurable through the minAvailableMemoryRequiredInMb " +
								  "in app.conf or CrawlConfiguration.MinAvailableMemoryRequiredInMb.",
									  _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb,
									  _crawlContext.RootUri)
					);
		}

		/// <summary>
		/// Check all setting limits and finded exceptions
		/// </summary>
		/// <param name="crawlResult">Crawl result</param>
		protected virtual void RunPreWorkChecks(CrawlResult crawlResult)
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
			if (_memoryManager == null ||
				_crawlContext.IsCrawlHardStopRequested ||
				_crawlContext.CrawlConfiguration.MaxMemoryUsageInMb < 1)
				return;

			int currentMemoryUsage = _memoryManager.GetCurrentUsageInMb();
			if (Logger.IsDebugEnabled)
				Logger.DebugFormat("Current memory usage for site [{0}] is [{1}mb]", _crawlContext.RootUri, currentMemoryUsage);

			if (currentMemoryUsage > _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
			{
				_memoryManager.Dispose();
				_memoryManager = null;

				string message = string.Format("Process is using [{0}mb] of memory which is above the max configured of [{1}mb] for site [{2}]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", currentMemoryUsage, _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb, _crawlContext.RootUri);
				crawlResult.ErrorException = new InsufficientMemoryException(message);

				Logger.Fatal(crawlResult.ErrorException);
				_crawlContext.IsCrawlHardStopRequested = true;
			}
		}

		/// <summary>
		/// Check cancellation token request
		/// </summary>
		/// <param name="crawlResult">Crawl result</param>
		protected virtual void CheckForCancellationRequest(CrawlResult crawlResult)
		{
			if (_crawlContext.CancellationTokenSource.IsCancellationRequested)
			{
				if (!_crawlCancellationReported)
				{
					string message = string.Format("Crawl cancellation requested for site [{0}]!", _crawlContext.RootUri);
					Logger.Fatal(message);
					crawlResult.ErrorException = new OperationCanceledException(message, _crawlContext.CancellationTokenSource.Token);
					_crawlContext.IsCrawlHardStopRequested = true;
					_crawlCancellationReported = true;
				}
			}
		}

		/// <summary>
		/// Check and run hard stop if needed
		/// </summary>
		protected virtual void CheckForHardStopRequest()
		{
			if (_crawlContext.IsCrawlHardStopRequested)
			{
				if (!_crawlStopReported)
				{
					Logger.InfoFormat("Hard crawl stop requested for site [{0}]!", _crawlContext.RootUri);
					_crawlStopReported = true;
				}

				_scheduler.Clear();

				_threadManager.AbortAll();
				// To be sure nothing was scheduled since first call to clear()
				_scheduler.Clear();

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
		}

		/// <summary>
		/// Check and run stop if needed
		/// </summary>
		protected virtual void CheckForStopRequest()
		{
			if (_crawlContext.IsCrawlStopRequested)
			{
				if (!_crawlStopReported)
				{
					Logger.InfoFormat("Crawl stop requested for site [{0}]!", _crawlContext.RootUri);
					_crawlStopReported = true;
				}

				_scheduler.Clear();
			}
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

			Logger.InfoFormat("Crawl timeout of [{0}] seconds has been reached for [{1}]", _crawlContext.CrawlConfiguration.CrawlTimeoutSeconds, _crawlContext.RootUri);
			_crawlContext.IsCrawlHardStopRequested = true;
		}

		//protected virtual async Task ProcessPage(PageToCrawl pageToCrawl)

		/// <summary>
		/// Process for crawling page
		/// </summary>
		/// <param name="pageToCrawl"></param>
		/// <param name="crawlResult"></param>
		protected virtual void ProcessPage(PageToCrawl pageToCrawl, CrawlResult crawlResult)
		{
			try
			{
				if (pageToCrawl == null)
					return;

				ThrowIfCancellationRequested();

				AddPageToContext(pageToCrawl);

				//CrawledPage crawledPage = await CrawlThePage(pageToCrawl);
				CrawledPage crawledPage = CrawlThePage(pageToCrawl);

				// Validate the root uri in case of a redirection.
				if (crawledPage.IsRoot)
					ValidateRootUriForRedirection(crawledPage);

				if (IsRedirect(crawledPage) && !_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
					ProcessRedirect(crawledPage);

				if (PageSizeIsAboveMax(crawledPage))
					return;

				ThrowIfCancellationRequested();

				// Parse crawled page
				bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
				if (shouldCrawlPageLinks || _crawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
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
					_scheduler.Add(crawledPage);
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

				_crawlContext.IsCrawlHardStopRequested = true;
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
					_scheduler.Add(page);
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
			return _isInternalDecisionMaker(uri, _crawlContext.RootUri) ||
				   _isInternalDecisionMaker(uri, _crawlContext.OriginalRootUri);
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
				isRedirect = (_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
							  crawledPage.HttpWebResponse.ResponseUri != null &&
							  crawledPage.HttpWebResponse.ResponseUri.AbsoluteUri != crawledPage.Uri.AbsoluteUri) ||
							  (!_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
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
			if (_crawlContext.CancellationTokenSource != null &&
			   _crawlContext.CancellationTokenSource.IsCancellationRequested)
			{
				_crawlContext.CancellationTokenSource.Token.ThrowIfCancellationRequested();
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

			if (_crawlContext.CrawlConfiguration.MaxPageSizeInBytes > 0 &&
				crawledPage.Content.Bytes != null &&
				crawledPage.Content.Bytes.Length > _crawlContext.CrawlConfiguration.MaxPageSizeInBytes)
			{
				isAboveMax = true;
				Logger.InfoFormat("Page [{0}] has a page size of [{1}] bytes which is above the [{2}] " +
								  "byte max, no further processing will occur for this page",
								  crawledPage.Uri,
								  crawledPage.Content.Bytes.Length,
								  _crawlContext.CrawlConfiguration.MaxPageSizeInBytes);
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
			CrawlDecision shouldCrawlPageLinksDecision = _crawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, _crawlContext);

			if (shouldCrawlPageLinksDecision.Allow)
				shouldCrawlPageLinksDecision = _shouldCrawlPageLinksDecisionMaker != null ?
					_shouldCrawlPageLinksDecisionMaker.Invoke(crawledPage, _crawlContext) :
					new CrawlDecision { Allow = true };

			if (!shouldCrawlPageLinksDecision.Allow)
			{
				Logger.DebugFormat("Links on page [{0}] not crawled, [{1}]",
					crawledPage.Uri.AbsoluteUri, shouldCrawlPageLinksDecision.Reason);

				FirePageLinksCrawlDisallowedEventAsync(crawledPage, shouldCrawlPageLinksDecision.Reason);
				FirePageLinksCrawlDisallowedEvent(crawledPage, shouldCrawlPageLinksDecision.Reason);
			}

			SignalCrawlStopIfNeeded(shouldCrawlPageLinksDecision);
			return shouldCrawlPageLinksDecision.Allow;
		}

		/// <summary>
		/// Get access to schedule the page
		/// </summary>
		/// <param name="pageToCrawl">Page to crawl</param>
		/// <returns>Bool</returns>
		protected virtual bool ShouldSchedulePageLink(PageToCrawl pageToCrawl)
		{
			if ((pageToCrawl.IsInternal || _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled) &&
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
			if (_maxPagesToCrawlLimitReachedOrScheduled)
				return false;

			CrawlDecision shouldCrawlPageDecision = _crawlDecisionMaker.ShouldCrawlPage(pageToCrawl, _crawlContext);
			if (!shouldCrawlPageDecision.Allow &&
				shouldCrawlPageDecision.Reason.Contains("MaxPagesToCrawl limit of"))
			{
				_maxPagesToCrawlLimitReachedOrScheduled = true;
				Logger.Info("MaxPagesToCrawlLimit has been reached or scheduled. No more pages will be scheduled.");
				return false;
			}

			if (shouldCrawlPageDecision.Allow)
				shouldCrawlPageDecision = (_shouldCrawlPageDecisionMaker != null) ?
					_shouldCrawlPageDecisionMaker.Invoke(pageToCrawl, _crawlContext) :
					new CrawlDecision { Allow = true };

			if (!shouldCrawlPageDecision.Allow)
			{
				Logger.DebugFormat("Page [{0}] not crawled, [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);
				FirePageCrawlDisallowedEventAsync(pageToCrawl, shouldCrawlPageDecision.Reason);
				FirePageCrawlDisallowedEvent(pageToCrawl, shouldCrawlPageDecision.Reason);
			}

			SignalCrawlStopIfNeeded(shouldCrawlPageDecision);
			return shouldCrawlPageDecision.Allow;
		}

		/// <summary>
		/// Recrawl page taking "Retry-After" from header if it possible
		/// </summary>
		/// <param name="crawledPage">Not crawled page</param>
		/// <returns>Can recrawl after "Retry-After" or config time?</returns>
		protected virtual bool ShouldRecrawlPage(CrawledPage crawledPage)
		{
			//TODO No unit tests cover these lines
			CrawlDecision shouldRecrawlPageDecision = _crawlDecisionMaker.ShouldRecrawlPage(crawledPage, _crawlContext);

			if (shouldRecrawlPageDecision.Allow)
			{
				shouldRecrawlPageDecision = _shouldRecrawlPageDecisionMaker != null ?
					_shouldRecrawlPageDecisionMaker.Invoke(crawledPage, _crawlContext) :
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

			SignalCrawlStopIfNeeded(shouldRecrawlPageDecision);
			return shouldRecrawlPageDecision.Allow;
		}

		//protected virtual async Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl)

		/// <summary>
		/// Transform <see cref="PageToCrawl"/> to <see cref="CrawledPage"/>
		/// by creating request
		/// </summary>
		/// <param name="pageToCrawl"></param>
		/// <returns></returns>
		protected virtual CrawledPage CrawlThePage(PageToCrawl pageToCrawl)
		{
			Logger.DebugFormat("About to crawl page [{0}]", pageToCrawl.Uri.AbsoluteUri);

			FirePageCrawlStartingEventAsync(pageToCrawl);
			FirePageCrawlStartingEvent(pageToCrawl);

			if (pageToCrawl.IsRetry)
				WaitMinimumRetryDelay(pageToCrawl);

			pageToCrawl.LastRequestTime = DateTime.Now;

			CrawledPage crawledPage = _pageRequester.MakeRequest(pageToCrawl.Uri, ShouldDownloadPageContent);
			//CrawledPage crawledPage = await _pageRequester.MakeRequestAsync(pageToCrawl.Uri, ShouldDownloadPageContent);

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
		/// Collect crawling pages to <see cref="CrawlContext.CrawlCountByDomain"/>
		/// </summary>
		/// <param name="pageToCrawl"></param>
		protected virtual void AddPageToContext(PageToCrawl pageToCrawl)
		{
			if (pageToCrawl.IsRetry)
			{
				pageToCrawl.RetryCount++;
				return;
			}

			Interlocked.Increment(ref _crawlContext.CrawledCount);
			_crawlContext.CrawlCountByDomain.AddOrUpdate(pageToCrawl.Uri.Authority, 1, (key, oldValue) => oldValue + 1);
		}

		/// <summary>
		/// Parse page to links by hyperlink parser
		/// </summary>
		/// <param name="crawledPage"></param>
		protected virtual void ParsePageLinks(CrawledPage crawledPage)
		{
			crawledPage.ParsedLinks = _hyperLinkParser.GetLinks(crawledPage);
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
				if (!_scheduler.IsUriKnown(uri) &&
					(_shouldScheduleLinkDecisionMaker == null || _shouldScheduleLinkDecisionMaker.Invoke(uri, crawledPage, _crawlContext)))
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
							_scheduler.Add(page);
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
				_scheduler.AddKnownUri(uri);
			}
		}

		/// <summary>
		/// Should schedule more page link
		/// </summary>
		/// <param name="linksAdded"></param>
		/// <returns></returns>
		protected virtual bool ShouldScheduleMorePageLink(int linksAdded)
		{
			return _crawlContext.CrawlConfiguration.MaxLinksPerPage == 0 ||
				   _crawlContext.CrawlConfiguration.MaxLinksPerPage > linksAdded;
		}

		/// <summary>
		/// Check need download page content
		/// </summary>
		/// <param name="crawledPage">Crawled page</param>
		/// <returns></returns>
		protected virtual CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage)
		{
			CrawlDecision decision = _crawlDecisionMaker.ShouldDownloadPageContent(crawledPage, _crawlContext);

			if (decision.Allow)
				decision = _shouldDownloadPageContentDecisionMaker != null ?
					_shouldDownloadPageContentDecisionMaker.Invoke(crawledPage, _crawlContext) :
					new CrawlDecision { Allow = true };

			SignalCrawlStopIfNeeded(decision);
			return decision;
		}

		/// <summary>
		/// Log all info from crawl config
		/// </summary>
		/// <param name="config"></param>
		protected virtual void PrintConfigValues(CrawlConfiguration config)
		{
			Logger.Info("Configuration Values:");

			string indentString = new string(' ', 2);
			string abotVersion = Assembly.GetAssembly(this.GetType()).GetName().Version.ToString();
			Logger.InfoFormat("{0}Abot Version: {1}", indentString, abotVersion);

			foreach (PropertyInfo property in config.GetType().GetProperties())
			{
				if (property.Name != "ConfigurationExtensions")
					Logger.InfoFormat("{0}{1}: {2}", indentString, property.Name, property.GetValue(config, null));
			}

			foreach (string key in config.ConfigurationExtensions.Keys)
			{
				Logger.InfoFormat("{0}{1}: {2}", indentString, key, config.ConfigurationExtensions[key]);
			}
		}

		/// <summary>
		/// Check decision for stop signals
		/// </summary>
		/// <param name="decision"></param>
		protected virtual void SignalCrawlStopIfNeeded(CrawlDecision decision)
		{
			if (decision.ShouldHardStopCrawl)
			{
				Logger.InfoFormat("Decision marked crawl [Hard Stop] for site [{0}], [{1}]",
					_crawlContext.RootUri, decision.Reason);

				_crawlContext.IsCrawlHardStopRequested = decision.ShouldHardStopCrawl;
			}
			else if (decision.ShouldStopCrawl)
			{
				Logger.InfoFormat("Decision marked crawl [Stop] for site [{0}], [{1}]",
					_crawlContext.RootUri, decision.Reason);

				_crawlContext.IsCrawlStopRequested = decision.ShouldStopCrawl;
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
				milliToWait = pageToCrawl.RetryAfter.Value * c_MILLISECOND_TRANSLATION - milliSinceLastRequest;
			}
			else
			{
				if (milliSinceLastRequest > _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds)
					return;

				milliToWait = _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds - milliSinceLastRequest;
			}

			Logger.InfoFormat("Waiting [{0}] milliseconds before retrying Url:[{1}] LastRequest:[{2}] SoonestNextRequest:[{3}]",
				milliToWait,
				pageToCrawl.Uri.AbsoluteUri,
				pageToCrawl.LastRequestTime,
				pageToCrawl.LastRequestTime.Value.AddMilliseconds(_crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds));

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
				_crawlContext.RootUri = ExtractRedirectUri(crawledRootPage);
				Logger.InfoFormat("The root URI [{0}] was redirected to [{1}]. [{1}] is the new root.",
					_crawlContext.OriginalRootUri,
					_crawlContext.RootUri);
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

			if (_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
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
			if (_threadManager != null) _threadManager.Dispose();
			if (_scheduler != null) _scheduler.Dispose();
			if (_pageRequester != null) _pageRequester.Dispose();
			if (_memoryManager != null) _memoryManager.Dispose();
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

		/// <summary>
		/// Compare Value > notPayAttention. Many config values start work, when its more then zero
		/// </summary>
		/// <param name="value">Config argument</param>
		/// <param name="notPayAttention">Min value that method get false. Usual it's zero</param>
		/// <returns>Bool</returns>
		private bool IsPayAttention(int value, int notPayAttention = c_NOT_PAY_ATTENTION)
		{
			return value > notPayAttention;
		}

		#endregion
	}
}