using Abot.Crawler;
using Abot.Poco;
using System;

namespace Abot.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            PrintDisclaimer();

            Uri uriToCrawl = GetSiteToCrawl(args);

			IWebCrawler crawler;

            //Uncomment only one of the following to see that instance in action
            crawler = GetDefaultWebCrawler();
            //crawler = GetManuallyConfiguredWebCrawler();
            //crawler = GetCustomBehaviorUsingLambdaWebCrawler();

            //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
            //This is where you process data about specific events of the crawl
            //crawler.PageCrawlStartingAsync += Crawler_ProcessPageCrawlStarting;
            //crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompleted;
            //crawler.PageCrawlDisallowedAsync += Crawler_PageCrawlDisallowed;
            //crawler.PageLinksCrawlDisallowedAsync += Crawler_PageLinksCrawlDisallowed;

            //Start the crawl
            //This is a synchronous call
            CrawlResult result = crawler.Crawl(uriToCrawl);

            //Now go view the log.txt file that is in the same directory as this executable. It has
            //all the statements that you were trying to read in the console window :).
            //Not enough data being logged? Change the app.config file's log4net log level from "INFO" TO "DEBUG"

            //PrintDisclaimer();
        }

        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new GoogleWebCrawler();
        }

        private static IWebCrawler GetManuallyConfiguredWebCrawler()
        {
			//Create a config object manually
			CrawlConfiguration config = new CrawlConfiguration
			{
				CrawlTimeoutSeconds = 0,
				DownloadableContentTypes = "text/html, text/plain",
				IsExternalPageCrawlingEnabled = false,
				IsExternalPageLinksCrawlingEnabled = false,
				IsRespectRobotsDotTextEnabled = false,
				IsUriRecrawlingEnabled = false,
				MaxConcurrentThreads = 10,
				MaxPagesToCrawl = 10,
				MaxPagesToCrawlPerDomain = 0,
				MinCrawlDelayPerDomainMilliSeconds = 1000
			};

			//Add you own values without modifying Abot's source code.
			//These are accessible in CrawlContext.CrawlConfuration.ConfigurationException object throughout the crawl
			config.ConfigurationExtensions.Add("Somekey1", "SomeValue1");
            config.ConfigurationExtensions.Add("Somekey2", "SomeValue2");

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new PoliteWebCrawler(config, null, null, null, null, null, null, null, null);
        }

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetDefaultWebCrawler();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPageDecisionMaker = (pageToCrawl, crawlContext) =>
            {
                if (pageToCrawl.Uri.AbsoluteUri.Contains("ghost"))
                    return new CrawlDecision { Allow = false, Reason = "Scared of ghosts" };

                return new CrawlDecision { Allow = true };
            };

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            crawler.ShouldDownloadPageContentDecisionMaker = (crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                return new CrawlDecision { Allow = true };
            };

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinksDecisionMaker = (crawledPage, crawlContext) =>
            {
                if (!crawledPage.IsInternal)
                    return new CrawlDecision { Allow = false, Reason = "We don't crawl links of external pages" };

                return new CrawlDecision { Allow = true };
            };

            return crawler;
        }

        private static Uri GetSiteToCrawl(string[] args)
        {
            string userInputUrl = string.Empty;
            if (args.Length < 1)
            {
                System.Console.WriteLine("Please, enter ABSOLUTE url to crawl (for ex.: https://github.com ):");
                userInputUrl = System.Console.ReadLine();
            }
            else
            {
                userInputUrl = args[0];
            }

			var isAbsoluteUri = Uri.TryCreate(userInputUrl, UriKind.Absolute, out Uri result);

			if (string.IsNullOrWhiteSpace(userInputUrl) || !isAbsoluteUri)
                throw new ApplicationException("Requare absolute url, without white spaces and not empty");

            return result;
        }

		private static void PrintDisclaimer()
        {
            PrintAttentionText("The demo is configured to only crawl a total of 10 pages and will wait 1 second in between http requests. This is to avoid getting you blocked by your isp or the sites you are trying to crawl. You can change these values in the app.config or Abot.Console.exe.config file.");
        }

        private static void PrintAttentionText(string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
        }

        static void Crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            //Process data
        }

        static void Crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            //Process data
        }

        static void Crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }

        static void Crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }
    }
}
