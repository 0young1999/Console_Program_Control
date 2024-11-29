using System.Net;
using System.Text;

namespace Young
{
	public class csWikiParse
    {
        private static csWikiParse instance;
        public static csWikiParse GetInstance()
        {
            if (instance == null) instance = new csWikiParse();
            return instance;
        }
        private csWikiParse() { }

		public bool tryParseTodayPage(out string result)
		{
			try
            {
				string sURL;
				sURL = "https://ko.wikipedia.org/wiki/%EC%9C%84%ED%82%A4%EB%B0%B1%EA%B3%BC:%EC%95%8C%EC%B0%AC_%EA%B8%80";

				WebRequest wrGETURL;
				wrGETURL = WebRequest.Create(sURL);

				Stream objStream;
				objStream = wrGETURL.GetResponse().GetResponseStream();

				StreamReader objReader = new StreamReader(objStream);

				StringBuilder sb = new StringBuilder();

				while (objReader.EndOfStream == false)
				{
					sb.AppendLine(objReader.ReadLine());
				}

				string body = sb.ToString();

				body = body[body.IndexOf("<b>오늘의 알찬 글</b>")..];
				body = body[(body.IndexOf("</figure>") + "</figure>".Length)..];
				body = body[..(body.IndexOf("</div>") + "</div>".Length)];

				while (body.Contains('<'))
				{
					int stx = body.IndexOf('<');
					int etx = body.IndexOf('>');

					string body1 = body[..stx];
					string body2 = body[(etx + 1)..];

					body = body1 + body2;
				}

				body = "오늘의 알찬 글\n" + body;

				result = body;

				return true;
			}
			catch (Exception e)
			{
				result = e.Message;
				return false;
			}
        }
    }
}
