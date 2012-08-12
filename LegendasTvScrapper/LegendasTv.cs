using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace LegendasTvScrapper {
	public class LegendasTv : IDisposable{
		private static readonly string siteUrl = "http://legendas.tv";
		private bool disposed;
		private CookieCollection cookies;

		public LegendasTv() {
		}

		public void Login(string userName, string password) {
			var result = MakePost(siteUrl + "/login_verificar.php", string.Format("txtLogin={0}&txtSenha={1}&entrar.x=18&entrar.y=14", userName, password));
			int i = 0;
		}

		public IList<Legenda> Search(string search) {
			const string searchDataFormat = "txtLegenda={0}&selTipo=1&int_idioma=1&btn_buscar.x=28&btn_buscar.y=7";
			var searchData = string.Format(searchDataFormat, HttpUtility.HtmlEncode(search).Replace(" ", "+"));
			string response = MakePost(siteUrl + "/index.php?opcao=buscarlegenda", searchData);

			return ParseHtml(response);
			/*
			var legendas = new List<Legenda>();
			int position = 0;
			int positionOdd = 0;
			int positionEven = 0;
			while (positionOdd > -1 && positionEven > -1) {
				positionOdd = response.IndexOf(".buscaDestaque", position);
				positionEven = response.IndexOf(".buscaNDestaque", position);
				if (positionOdd > -1 && positionOdd < positionEven) {
					position = positionOdd + 1;
					legendas.Add(ParseLegedentaAt(position, response));
				} else if(positionEven > -1 && positionEven < positionOdd) {
					position = positionEven + 1;
					legendas.Add(ParseLegedentaAt(position, response));
				}
			}
			return legendas;*/
		}


		private IList<Legenda> ParseHtml(string html) {
			const string startIdString = "javascript:abredown('";
			const int idLength = 32;
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var legendaTables = doc.DocumentNode.SelectNodes("//table[@class=\"buscaNDestaque\" or @class=\"buscaDestaque\"]");

			var legendas = new List<Legenda>();
			foreach (var legendaTable in legendaTables) {
				var id = legendaTable.Attributes["onclick"].Value.Replace(startIdString, "").Substring(0, idLength);
				var nameNome = legendaTable.SelectSingleNode("./tr/td/span[@class=\"brls\"]");
				var name = nameNome.InnerText;
				legendas.Add(new Legenda(id, name));
			}
			return legendas;
		}

		public string DownloadLegenda(string id) {
			var request = (HttpWebRequest) WebRequest.Create(string.Format(siteUrl + "/info.php?d={0}&c=1", id));
			request.CookieContainer = new CookieContainer();
			if (cookies != null) {
				request.CookieContainer.Add(cookies);
			}
			var response = (HttpWebResponse) request.GetResponse();
			using (var responseStream = response.GetResponseStream()) {
				var localFile = Path.GetTempFileName();
				using (var localStream = File.Create(localFile)) {
					byte[] buffer = new byte[1024];
					int bytesRead;
					do {
						bytesRead = responseStream.Read(buffer, 0, buffer.Length);
						localStream.Write(buffer, 0, bytesRead);
					} while (bytesRead > 0);
					return localFile;
				}
			}
		}

		private string MakePost(string url, string postData) {
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.CookieContainer = new CookieContainer();
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			byte[] bytes = Encoding.UTF8.GetBytes(postData);
			request.ContentLength = bytes.Length;
			if (cookies != null) {
				request.CookieContainer.Add(cookies);
			}

			Stream requestStream = request.GetRequestStream();
			requestStream.Write(bytes, 0, bytes.Length);

			var response = (HttpWebResponse)request.GetResponse();
			if (response.Cookies != null && response.Cookies.Count > 0)
				this.cookies = response.Cookies;
			using (var stream = response.GetResponseStream()) {
				using (var reader = new StreamReader(stream)) {
					return reader.ReadToEnd();
				}
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (!disposed) {
				if (disposing) {
				}
				disposed = true;
			}
		}

		~LegendasTv() {
			Dispose(false);
		}
	}
}
