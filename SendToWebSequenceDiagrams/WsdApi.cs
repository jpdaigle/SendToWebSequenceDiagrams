using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.IO;
using System.Net;

namespace SendToWebSequenceDiagrams
{

    public enum WsdFormat
    {
        PNG,
        SVG,
        PDF
    }

    public class WsdStyle
    {
        public const string VS2010 = "vs2010";
    }


    class WsdRequest
    {
        public string MSC { get; set; }
        public Stream Result { get; private set; }
        public WsdFormat Format { get; set; }
        public string Style { get; set; }
        public string BaseUrl { get; set; }

        public WsdRequest()
        {
            Format = WsdFormat.PNG;
            Style = WsdStyle.VS2010;
            BaseUrl = "http://www.websequencediagrams.com/";
        }

        public void PerformRequest()
        {
            MemoryStream ms = new MemoryStream();
            WsdApi.GrabSequenceDiagram(MSC, Enum.GetName(typeof(WsdFormat), Format).ToLower(), Style, BaseUrl, ms);
            this.Result = ms;
        }
    }

    class WsdApi
    {

        /// <summary>
        /// Given a WSD description, produces a sequence diagram PNG.
        /// </summary>
        /// This method uses the WebSequenceDiagrams.com public API to query an image and stored in a local
        /// temporary directory on the file system.
        /// 
        /// You can easily change it to return the stream to the image requested instead of a file.
        /// 
        /// To invoke it:
        ///   ..
        ///   using System.Web;
        ///   ...
        ///   
        ///   string fileName = grabSequenceDiagram("a->b: Hello", "qsd", "png");
        ///   ..
        ///   
        /// You need to add the assembly "System.Web" to your reference list (that by default is not
        /// added to new projects)
        /// 
        /// Questions / suggestions: fabriziobertocci@gmail.com
        /// 
        /// <param name="wsd">The web sequence diagram description text</param>
        /// <param name="style">One of the valid styles for the diagram</param>
        /// <param name="format">The output format requested. Must be one of the valid format supported</param>
        /// <returns>The full path of the downloaded image</returns>
        /// <exception cref="Exception">If an error occurred during the request</exception>
        public static void GrabSequenceDiagram(String wsd, String format, string style, string baseUrl, Stream outStream)
        {
            // Websequence diagram API:
            // prepare a POST body containing the required properties
            string post = string.Format("style={0}&apiVersion=1&format={1}&message={2}", 
                style, 
                format, 
                HttpUtility.UrlEncode(wsd));
            byte[] postBytes = Encoding.ASCII.GetBytes(post);

            // Typical Microsoft crap here: the HttpWebRequest by default always append the header
            //          "Expect: 100-Continue"
            // to every request. Some web servers (including www.websequencediagrams.com) chockes on that
            // and respond with a 417 error.
            // Disable it permanently:
            System.Net.ServicePointManager.Expect100Continue = false;

            // set up request object
            System.Net.HttpWebRequest request;
            baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
            // The following command might throw UriFormatException
            request = System.Net.WebRequest.Create(baseUrl + "index.php") as System.Net.HttpWebRequest;

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            System.Net.HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse;
            ValidateStatusOk(response);

            StreamReader stream = new StreamReader(response.GetResponseStream());
            String jsonObject = stream.ReadToEnd();
            stream.Close();

            // Expect response like this one: {"img": "?png=mscKTO107", "errors": []}
            var responseBag = DeserializeJsonObject(jsonObject) as Dictionary<string, object>;
            if (responseBag == null) 
                throw new Exception("Unable to parse response: not a dictionary.");
            if (!responseBag.ContainsKey("img")) 
                throw new Exception("Unable to parse response: no img key.");

            // Now download the image
            string uri = responseBag["img"].ToString();
            request = System.Net.WebRequest.Create(baseUrl + uri) as System.Net.HttpWebRequest;
            request.Method = "GET";
            response = request.GetResponse() as System.Net.HttpWebResponse;
            ValidateStatusOk(response);
            using (Stream src = response.GetResponseStream())
            {
                src.CopyTo(outStream);
            }
        }

        public static object DeserializeJsonObject(string json)
        {
            var jss = new JavaScriptSerializer();
            return jss.DeserializeObject(json);
        }

        public static void ValidateStatusOk(HttpWebResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Did not get OK response: " + response.StatusDescription);
            }
        }

    }
}
