using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;

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
        public static string GrabSequenceDiagram(String wsd, String format, string style, string baseUrl, Stream outStream)
        {
            // Websequence diagram API:
            // prepare a POST body containing the required properties
            StringBuilder sb = new StringBuilder("style=");
            sb.Append(style).Append("&apiVersion=1&format=").Append(format).Append("&message=");
            sb.Append(System.Web.HttpUtility.UrlEncode(wsd));
            byte[] postBytes = Encoding.ASCII.GetBytes(sb.ToString());

            // Typical Microsoft crap here: the HttpWebRequest by default always append the header
            //          "Expect: 100-Continue"
            // to every request. Some web servers (including www.websequencediagrams.com) chockes on that
            // and respond with a 417 error.
            // Disable it permanently:
            System.Net.ServicePointManager.Expect100Continue = false;

            // set up request object
            System.Net.HttpWebRequest request;
            // The following command might throw UriFormatException
            baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
            request = System.Net.WebRequest.Create(baseUrl + "index.php") as System.Net.HttpWebRequest;

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;

            // add post data to request
            System.IO.Stream postStream = request.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            System.Net.HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse;

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Unexpected HTTP status from server: " + response.StatusCode + ": " + response.StatusDescription);
            }

            System.IO.StreamReader stream = new System.IO.StreamReader(response.GetResponseStream());
            String jsonObject = stream.ReadToEnd();
            stream.Close();

            // Expect response like this one: {"img": "?png=mscKTO107", "errors": []}
            // Instead of using a full JSON parser, do a simple parsing of the response
            String[] components = jsonObject.Split('"');
            // Ensure component #1 is 'img':
            if (components[1].Equals("img") == false)
            {
                throw new Exception("Error parsing response from server: " + jsonObject);
            }

            String uri = components[3];

            // Now download the image
            request = System.Net.WebRequest.Create(baseUrl + uri) as System.Net.HttpWebRequest;
            request.Method = "GET";

            response = request.GetResponse() as System.Net.HttpWebResponse;

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Server reported HTTP error during image fetch: " + response.StatusCode + ": " + response.StatusDescription);
            }
            try
            {
                System.IO.Stream srcStream = response.GetResponseStream();
                string fileName = System.IO.Path.GetTempFileName();
                //System.IO.FileStream dstStream = new System.IO.FileStream(fileName, System.IO.FileMode.Create);

                // Copy streams
                byte[] buffer = new byte[1024];
                int read;
                while ((read = srcStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outStream.Write(buffer, 0, read);
                }
                //dstStream.Close();
                srcStream.Close();
                return fileName;
            }
            catch (Exception e)
            {
                throw new Exception("Exception while saving image to temp file: " + e.Message);
            }
        }



    }
}
