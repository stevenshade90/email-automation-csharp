using Email_Automation.PrimaryUser;
using System.Net;
using System.Text.Json;


namespace OAuthImplementation
{
    internal class Authentication
    {
        private readonly IConfigurationRoot config;

        private readonly User _user;
        public User PrimaryUser => _user;

        private readonly string clientId;
        private readonly string clientSecret;

        private static readonly string redirectUri = "http://127.0.0.1:5000/";
        private static readonly string tokenEndpoint = "https://oauth2.googleapis.com/token";
        private static readonly string grantType = "authorization_code";

        public static string requestUri = "https://gmail.googleapis.com/gmail/v1/users/me/profile";
        public static string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        public static string responseType = "code";
        public static string scope = "https://mail.google.com/";
        public static string authorizationUrl;

        public string AuthorizationCode { get; set; }
        public Task<string> AccessToken {  get; set; }

        public Authentication(User user)
        {
            _user = user;

             config = new ConfigurationBuilder()
                .AddUserSecrets<Authentication>()
                .Build();

            clientId = config["Auth:clientId"];
            clientSecret = config["Auth:clientSecret"];
            authorizationUrl = $"{authorizationEndpoint}?response_type={responseType}&client_id={clientId}&redirect_uri={redirectUri}&scope={scope}";
        }

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
            //Something here to check for failed login?
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

            HttpClient client = PrimaryUser.UserHttpClient;

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
            HttpClient client = PrimaryUser.UserHttpClient;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage apiResponse = await client.GetAsync(apiUrl);
            apiResponse.EnsureSuccessStatusCode();

            var responseString = await apiResponse.Content.ReadAsStringAsync();
            return responseString;
        }
    }
}