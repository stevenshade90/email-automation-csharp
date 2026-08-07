using System.Net;
using System.Text.Json;


namespace OAuthImplementation
{
    internal class Authentication
    {
        private static readonly string clientId = "";
        private static readonly string clientSecret = "";
        private static readonly string redirectUri = "";
        private static readonly string tokenEndpoint = "";
        private static readonly string grantType = "";

        public static string requestUri = "";
        public static string authorizationEndpoint = "";
        public static string responseType = ""; 
        public static string scope = ""; 
        public static string authorizationUrl = $"{authorizationEndpoint}?response_type={responseType}&client_id={clientId}&redirect_uri={redirectUri}&scope={scope}";

        public string AuthorizationCode { get; set; }
        public Task<string> AccessToken {  get; set; }

        public Authentication() { }

        public void OpenInternetWindow()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = authorizationUrl,
                UseShellExecute = true
            });
        }

        public string HttpListenerForAuthorizationCode()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);

            listener.Start();
            Console.WriteLine("Authorizing user...");

            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            var authorizationCode = request.QueryString["code"];

            string responseString = "Authorization complete! You can now close this window.";
            ReadOnlySpan<byte> buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            System.IO.Stream output = response.OutputStream;
            output.Write(buffer);

            output.Close();
            listener.Stop();

            return authorizationCode;
        }

        public async Task<string> ExchangeAuthorizationCodeForAccessToken(string authorizationCode)
        {
            string postData = $"grant_type=authorization_code&code={authorizationCode}&redirect_uri={redirectUri}&client_id={clientId}&client_secret={clientSecret}";

            HttpClient client = new HttpClient();

            StringContent stringCon = new StringContent(postData);
            stringCon.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", stringCon);

            string jsonResponse = await response.Content.ReadAsStringAsync();

            JsonDocument doc = JsonDocument.Parse(jsonResponse);
            string? accessToken = doc.RootElement.GetProperty("access_token").GetString();

            return accessToken;
        }

        public async Task<string> MakeAuthorizedRequest(string accessToken, string apiUrl)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage apiResponse = await client.GetAsync(apiUrl);
            apiResponse.EnsureSuccessStatusCode();

            var responseString = await apiResponse.Content.ReadAsStringAsync();
            return responseString;
        }
    }
}