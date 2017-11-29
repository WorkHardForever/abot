using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Abot.Poco;
using log4net;

namespace Abot.Core
{
	/// <summary>
	/// Extractor for generating info from requested page
	/// </summary>
	[Serializable]
	public class WebContentExtractor : IWebContentExtractor
	{
		#region Const

		/// <summary>
		/// Find expression from : http://stackoverflow.com/questions/3458217/how-to-use-regular-expression-to-match-the-charset-string-in-html
		/// </summary>
		protected const string CRegularCharset = @"<meta(?!\s*(?:name|value)\s*=)(?:[^>]*?content\s*=[\s""']*)?([^>]*?)[\s""';]*charset\s*=[\s""']*([^\s""'/>]*)";

		#endregion

		#region Protected Field

		/// <summary>
		/// Logger
		/// </summary>
		protected ILog Logger = LogManager.GetLogger(CrawlConfiguration.LoggerName);

		#endregion

		#region Public Methods

		/// <summary>
		/// Collect data from responsed page
		/// </summary>
		/// <param name="response">Web response</param>
		/// <returns>Extract page</returns>
		public virtual PageContent GetContent(WebResponse response)
		{
			using (MemoryStream memoryStream = GetRawData(response))
			{
				String charset = GetCharsetFromHeaders(response);

				if (charset == null)
				{
					memoryStream.Seek(0, SeekOrigin.Begin);

					// Do not wrap in closing statement to prevent closing of this stream.
					StreamReader reader = new StreamReader(memoryStream, Encoding.ASCII);
					String body = reader.ReadToEnd();
					charset = GetCharsetFromBody(body);
				}

				charset = CleanCharset(charset);
				Encoding encoding = GetEncoding(charset);

				string content = string.Empty;

				memoryStream.Seek(0, SeekOrigin.Begin);
				using (StreamReader reader = new StreamReader(memoryStream, encoding))
				{
					content = reader.ReadToEnd();
				}

				PageContent pageContent = new PageContent
				{
					Bytes = memoryStream.ToArray(),
					Charset = charset,
					Encoding = encoding,
					Text = content
				};

				return pageContent;
			}
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Getting charset using response header
		/// </summary>
		/// <param name="webResponse">Web response</param>
		/// <returns>Encoding</returns>
		protected virtual string GetCharsetFromHeaders(WebResponse webResponse)
		{
			string charset = null;

			String ctype = webResponse.Headers["content-type"];
			if (ctype != null)
			{
				int ind = ctype.IndexOf("charset=");
				if (ind != -1)
					charset = ctype.Substring(ind + 8);
			}

			return charset;
		}

		/// <summary>
		/// Try get charset using regex match
		/// </summary>
		/// <param name="body">Content</param>
		/// <returns>Encoding</returns>
		protected virtual string GetCharsetFromBody(string body)
		{
			String charset = null;

			if (body != null)
			{
				Match match = Regex.Match(body, CRegularCharset, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					charset = string.IsNullOrWhiteSpace(match.Groups[2].Value) ?
						null :
						match.Groups[2].Value;
				}
			}

			return charset;
		}

		/// <summary>
		/// Get encoding or if charset = null set UTF-8
		/// </summary>
		/// <param name="charset">Charset of page</param>
		/// <returns>Encoding</returns>
		protected virtual Encoding GetEncoding(string charset)
		{
			Encoding encoding = Encoding.UTF8;

			if (charset != null)
			{
				try
				{
					encoding = Encoding.GetEncoding(charset);
				}
				catch (Exception e)
				{
					Logger.Error(e);
					throw e;
				}
			}

			return encoding;
		}

		/// <summary>
		/// Wrap browser charset value to .Net charsets
		/// </summary>
		/// <param name="charset"></param>
		/// <returns></returns>
		protected virtual string CleanCharset(string charset)
		{
			// TODO temporary hack, this needs to be a configurable value
			// to do dictionary
			if (charset == "cp1251") //Russian, Bulgarian, Serbian cyrillic
				charset = "windows-1251";

			return charset;
		}

		#endregion

		#region Private Method

		private MemoryStream GetRawData(WebResponse response)
		{
			MemoryStream rawData = new MemoryStream();

			try
			{
				using (Stream rs = response.GetResponseStream())
				{
					byte[] buffer = new byte[1024];
					int read = rs.Read(buffer, 0, buffer.Length);
					while (read > 0)
					{
						rawData.Write(buffer, 0, read);
						read = rs.Read(buffer, 0, buffer.Length);
					}
				}
			}
			catch (Exception e)
			{
				Logger.WarnFormat("Error occurred while downloading content of url {0}", response.ResponseUri.AbsoluteUri);
				Logger.Warn(e);
			}

			return rawData;
		}

		#endregion
	}
}
