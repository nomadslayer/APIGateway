using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiGateway
{
    public class Route
    {
        public string Endpoint { get; set; }
        public Destination Destination { get; set; }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
    }

    public class authz_rules
    {
        public Rules_Details rules { get; set; }
    }

    public class Rules_Details
    {
        public string[] pass { get; set; }
        public string[] block { get; set; }
    }
}
