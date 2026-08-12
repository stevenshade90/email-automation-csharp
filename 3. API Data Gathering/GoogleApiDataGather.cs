using Email_Automation.PrimaryUser;
using Orchestra_Finder_Web_Crawler;
using OrchestraInformation;
using SqlData;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace Orchestra_Finder_API_Query
{
    internal class CoreProgram
    {
        private static IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<CoreProgram>()
            .Build();

        internal static async Task GoogleSearchForOrchestraInformation(User PrimaryUser)
        {
            HttpClient Client = PrimaryUser.UserHttpClient;

            string serpApiKey = config["Api:serpApiKey"];
            string CountyFileLocation = @"Supplemental\.txt Files\counties.txt";
            string State = "Wyoming";

            // 1. First, read all the counties in the file, and have them stored locally
            try
            {
                if (File.Exists(CountyFileLocation))
                {
                    Console.WriteLine($"File found! Beginning to gather orchestra information...");
                }
                else
                {
                    throw new FileNotFoundException("Could not locate the file.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error -- Exiting program ({ex.Message})");
                Environment.Exit(1);
            }

            List<string> Counties = GetAllCounties(CountyFileLocation);

            /* 2. Second, iterate over all of the counties stored in "counties.txt" and generate a query to do web searches to gather all pages that are returned
             * Each page object is stored, and then each page is parsed to pull out the data
             * Create a new OrchestraRecord type and store it in a List that contains all orchestras
             */
            using OrchestraRecordContext context = new OrchestraRecordContext();
            context.OrchestraInformation.ExecuteDelete();
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('OrchestraInformation', RESEED, 0)");

            foreach (string county in Counties)
            {
                Console.WriteLine($"\t=> Beginning search for {county} county...");

                string query = $"Orchestras in {county} county {State}";
                string engine = "google";
                string urlCall = @$"https://serpapi.com/search?engine={engine}&q={Uri.EscapeDataString(query)}&api_key={serpApiKey}";

                //Polly repeats will occur here as needed and as defined in the ResiliencePipeline class
                await PrimaryUser.Pipeline.ExecuteAsync(async (token) => await SearchWebAndGetOrchestraRecordsAsync(PrimaryUser, context, urlCall, Client, State, county));
            }
            context.SaveChanges();
        }

        private static List<string> GetAllCounties(string filename)
        {
            List<string> AllCounties = new List<string>();

            //Now, something to read all of the counties in the Counties variable, store each line as an individual string in the AlLCounties, then return it 
            return File.ReadLines(filename)
                .Where(county => !string.IsNullOrWhiteSpace(county))
                .Select(county => county.Trim())
                .ToList();
        }

        private static async Task SearchWebAndGetOrchestraRecordsAsync(User PrimaryUser, OrchestraRecordContext context, string urlCall, HttpClient Client, string state, string county)
        {
            string[] restrictedWebsites = { "wikipedia", "facebook", "instagram", "tiktok", "linkedin", "youtube", "causeiq" };

            var data = await Client.GetAsync(urlCall);

            data.EnsureSuccessStatusCode();

            if (state == "Wyoming")
            {
                state = "WY";
            }

            string json = await data.Content.ReadAsStringAsync(); 

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("organic_results", out var resultsArray))
                {
                    foreach (var result in resultsArray.EnumerateArray())
                    {
                        string name = result.TryGetProperty("title", out var t)
                            ? t.GetString() ?? ""
                            : "";

                        name = SanitizeOrchestraName(name);
                        // edit name method?

                        string website = result.TryGetProperty("link", out var ws)
                            ? ws.GetString() ?? ""
                            : "";

                        if (restrictedWebsites.Any(word => website.Contains(word, StringComparison.OrdinalIgnoreCase))) { continue; }

                        // Attempts to get the email from the website by using the WebCrawler helper class
                        string email = await new WebCrawler().GetEmailFromWebsiteHtml(website);

                        OrchestraRecord newOrchestra = new OrchestraRecord
                        {
                            State = state,
                            County = county,
                            OrchestraName = name,
                            Website = website,
                            Email = email //await GetEmailFromWebsiteAsync(Client, website)
                        };

                        /* Maybe add all records to a list first, then add them to the database after using some basic logic
                            * (like checking for duplicates, or checking if the email is valid, etc.) 
                            * that way there aren't numerous duplicates of the same orchestra name,
                            * this way orchestras arent updated for no reason, especially when viewing large amounts of data
                            * maybe just move the logic from the other page (The LoadEmailsToUsers method on Program.cs)
                            * 
                            * Rather than: database => Parse info => add to user
                            * becomes: websearch => Parse info => Add to adatabase (This way the Read() method in CRUD will already eliminiate duplicate Orchestra names,
                            * but be careful not to remove NO-NAME/NO-EMAIL values 
                            * 
                            * Records also need to be loaded into memory -- loading larger datasets takes too long during CRUD
                            * When in CRUD, quick display of the current record to reduce scrolling on larger data sets
                            */

                        context.OrchestraInformation.Add(newOrchestra);
                    }
                }
            }
        }

        private static string SanitizeOrchestraName(string name)
        {
            string updatedName = name;

            string[] unavailableText = { "home", "about", "brings", "brass", "band", ":"};
            string[] requiredText = { "orchestra", "philharmonic", "symphony", "college" };
            char[] dashTypes = { '\u002D', '\u2013', '\u2014' };


            if (string.IsNullOrEmpty(updatedName) || !requiredText.Any(text => updatedName.Contains(text, StringComparison.OrdinalIgnoreCase))) 
            { 
                return "NO-NAME";
            }

            if (updatedName.Contains('|'))
            {
                updatedName = updatedName[0..((updatedName.IndexOf('|')))];
            }

            foreach (char dash in dashTypes)
            {
                if (updatedName.Contains($" {dash} ", StringComparison.OrdinalIgnoreCase))
                {
                    updatedName = updatedName[0..(updatedName.IndexOf(dash))];
                }
            }

            foreach (string restrictedWord in unavailableText)
            {
                updatedName = Regex.Replace(updatedName, $@"\b{restrictedWord}\b\s?", "", RegexOptions.IgnoreCase);
            }

            return updatedName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(updatedName.ToLower());
        }
    }
}