using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApiGateway
{
    public class Destination
    {
        public string Path { get; set; }
        public bool IsAuth { get; set; }
        static HttpClient client = new HttpClient();
        public Destination(string uri, bool requiresAuthentication, bool isauth)
        {
            Path = uri;
            IsAuth = isauth;
        }

        public Destination(string path)
            : this(path, false, false)
        {
        }

        private Destination()
        {
            Path = "/";
            IsAuth = false;
        }

        public async Task<HttpResponseMessage> SendRequest(HttpRequest request)
        {
            string requestContent = "";

            if (!IsAuth)
            {
                using (Stream receiveStream = request.Body)
                {
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                    {
                        requestContent = readStream.ReadToEnd();
                    }
                }
            }

            using (var newRequest = new HttpRequestMessage(new HttpMethod(IsAuth ? "POST" : request.Method), CreateDestinationUri(request)))
            {
                string include = "Content-";
                foreach (var req in request.Headers.Where(m => !m.Key.Contains(include)))
                {
                    newRequest.Headers.Add(req.Key, req.Value.ToString());
                }

                newRequest.Headers.Host = newRequest.RequestUri.Host;
                newRequest.Content = new StringContent(requestContent, Encoding.UTF8, "application/json");
                //using (var response = await client.SendAsync(newRequest))
                //{
                //    return response;
                //}

                var response = await client.SendAsync(newRequest);
                //var response = client.PostAsync(newRequest.RequestUri, newRequest.Content).Result;
                return response;
            }
        }

        private string CreateDestinationUri(HttpRequest request)
        {
            string requestPath = request.Path.ToString();
            string queryString = request.QueryString.ToString();

            string endpoint = "";
            string[] endpointSplit = requestPath.Substring(1).Split('/');
            string endpointPath = GetAfterFirstFromSplit(requestPath, '/');

            if (endpointSplit.Length > 1)
                endpoint = endpointPath;


            if (IsAuth)
            {
                return Path + queryString;
            }

            return Path + endpoint + queryString;
        }

        private string GetFirstFromSplit(string input, char delimiter)
        {
            var i = input.IndexOf(delimiter);

            return i == -1 ? input : input.Substring(0, i);
        }

        private string GetAfterFirstFromSplit(string input, char delimiter)
        {
            var i = input.IndexOf(delimiter, 1);

            return i == -1 ? input : input.Substring(i + 1);
        }
    }
}
