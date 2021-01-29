using ApiGateway.Utils;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ApiGateway
{
    public class Router
    {
        public List<Route> Routes { get; set; }
        public Destination AuthenticationService { get; set; }

        public Router(string routeConfigFilePath)
        {
            dynamic router = JsonLoader.LoadFromFile<dynamic>(routeConfigFilePath);

            Routes = JsonLoader.Deserialize<List<Route>>(Convert.ToString(router.routes));
            AuthenticationService = JsonLoader.Deserialize<Destination>(Convert.ToString(router.authenticationService));
        }

        public async Task<HttpResponseMessage> RouteRequest(HttpRequest request)
        {
            string path = request.Path.ToString();
            string basePath = '/' + path.Split('/')[1];

            Destination destination;
            try
            {
                if (path.ToLower() == "/api-documentation")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(File.ReadAllText("./api-documentation.html"))
                    };
                }

                destination = Routes.First(r => r.Endpoint.Equals(basePath)).Destination;
            }
            catch
            {
                return ConstructErrorMessage("The path could not be found.");
            }

            //check block or pass
            if (await Restricted("block", path))
            {
                //block url
                return ConstructErrorMessage("The path is blocked.", HttpStatusCode.Unauthorized);
            }
            else if (await Restricted("pass", path) == false)
            {
                try
                {
                    HttpResponseMessage authResponse = await AuthenticationService.SendRequest(request);
                    ErrorResponse authResponseMessage = new ErrorResponse
                    {
                        StatusCode = (int)authResponse.StatusCode,
                        ReasonPhrase = authResponse.ReasonPhrase
                    };
                    if (!authResponse.IsSuccessStatusCode) return ConstructErrorMessage(JsonConvert.SerializeObject(authResponseMessage), authResponse.StatusCode);

                    request.Headers["sessionid"] = string.Concat("Bearer ", authResponse.Content.ReadAsStringAsync().Result);
                }
                catch (Exception ex)
                {
                    return ConstructErrorMessage("Error on Authorization: " + JsonConvert.SerializeObject(ex), HttpStatusCode.BadGateway);
                }
            }

            try
            {
                HttpResponseMessage Response = await destination.SendRequest(request);
                ErrorResponse ResponseMessage = new ErrorResponse
                {
                    StatusCode = (int)Response.StatusCode,
                    ReasonPhrase = Response.ReasonPhrase
                };

                return Response;
            }
            catch (Exception ex)
            {
                return ConstructErrorMessage("Error on Accessing: " + JsonConvert.SerializeObject(ex), HttpStatusCode.BadGateway);
            }
        }

        private HttpResponseMessage ConstructErrorMessage(string error, HttpStatusCode httpStatusCode = HttpStatusCode.NotFound)
        {
            HttpResponseMessage errorMessage = new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = new StringContent(error)
            };

            switch(errorMessage.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    errorMessage.Content = new StringContent(File.ReadAllText("./error_404.html"));
                    break;
            }

            return errorMessage;
        }

        public async Task<bool> Restricted(string type, string url)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{"authz_rules.json"}");
            var JSON = await System.IO.File.ReadAllTextAsync(filePath);
            authz_rules Authz = JsonConvert.DeserializeObject<authz_rules>(JSON);

            switch (type)
            {
                case "pass":
                    return Authz.rules.pass.Where(m => url.Contains(m)).Count() > 0;
                case "block":
                    return Authz.rules.block.Where(m => url.Contains(m)).Count() > 0;
                default:
                    return false;
            }
        }
    }
}
