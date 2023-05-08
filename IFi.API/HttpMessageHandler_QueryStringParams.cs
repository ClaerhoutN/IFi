using System.Web;

namespace IFi.API
{
    internal class HttpMessageHandler_QueryStringParams : HttpClientHandler
    {
        private readonly Dictionary<string, string> _parameters;
        public HttpMessageHandler_QueryStringParams(Dictionary<string, string> parameters)
        {
            _parameters = parameters;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder(request.RequestUri.AbsoluteUri);
            var paramValues = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach(var parameter in _parameters)
                paramValues.Add(parameter.Key, parameter.Value);          
            uriBuilder.Query = paramValues.ToString();
            request.RequestUri = uriBuilder.Uri;

            return base.SendAsync(request, cancellationToken);
        }
    }
}
