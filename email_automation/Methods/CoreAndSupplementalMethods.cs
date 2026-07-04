using CsvHelper;
using Email_Automation.PrimaryUser;
using Email_Automation.SupplementalMethods;
using Email_Automation.Engines;
using OrchestraInformation;

using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

/* add readingmessage location for ReadCsvAndLogOrchestraInfo (...1. Primary Documents\MailMessage_Prompt.txt)
    PrimaryUser.AccountInformationEngine.UserMailMessage.To.Add(""); // test email
 */

namespace Email_Automation.SupplementalMethods
{
    internal class LoadingAndDisplay
    {
        public static String divider = new String('_', Console.WindowWidth);

        //Delegates
        public Action<User> DisplayOrchestrasAndWarning => DisplayAndWarning;
        public Action<User> Warning;

        //Methods
        public void LoadingImage(Task awaitingCsvProcessing)
        {
            //Loading image that will only display as the csv processing task is running

            int iterator = 1;
            Console.ForegroundColor = ConsoleColor.Green;
            char[] loading = { '|', '/', '-', '\\' };

            while (!awaitingCsvProcessing.IsCompleted)
            {
                Console.Write("\rLoading information... {0}", loading[iterator % 4]);
                iterator++;
                Thread.Sleep(100);
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"\r{divider}\n");
        }

        private static void DisplayAndWarning(User PrimaryUser)
        {
            //A warning for each email that is about to send, and obtains user confirmation to send or exit program

            int iterator = 1;

            foreach (Orchestra o in PrimaryUser)
            {
                Console.WriteLine("{0, 3: ##0}. {1, -45} : {2, -45}", iterator++, o.OrchestraName, o.OrchestraEmail);
            }

            PrimaryUser.LoadingAndDisplayEngine.Warning = delegate (User PrimaryUser)
            {
                String notice = $"\aYOU ARE ABOUT TO EMAIL THIS NUMBER OF RECIPIENTS: {PrimaryUser.AllOrchestras.Count()}";

                Console.WriteLine($"{divider}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n{notice}");
                Console.Write("ENTER 'Y' TO CONTINUE: ");

                ConsoleKeyInfo keyPressed = Console.ReadKey();
                if (keyPressed.Key == ConsoleKey.Y)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n\nContinuing with send...");
                }
                else
                {
                    Console.WriteLine($"\n\nYou pressed: {keyPressed.KeyChar}");
                    Console.WriteLine("Terminating program");
                    Environment.Exit(0);
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{divider}\n");
                Thread.Sleep(3000);
            };

            PrimaryUser.LoadingAndDisplayEngine.Warning?.Invoke(PrimaryUser);
        }
    }
}


namespace Email_Automation.CoreMethods
{
    internal class MailingMethods
    {
        private String _emailMessage = "";
        private String _transcript = "";
        private String _urlInfoAsString = "";
        private static StringBuilder _modifiedUrl = new StringBuilder();

        //Delegate
        public static Action<User> SendEmail => EmailSendingSequence;

        //Properties
        public static String ModifiedUrl => _modifiedUrl.ToString();

        //Methods
        public static async Task BuildURL(User PrimaryUser)
        {
            //URL for CSV task is built here, and this method will continue until
            //(1) an appropriately formatted URL is entered, and (2) data is pulled from the URL

            String url;
            bool success = false;

            while (!success)
            {
                Console.Write("Enter URL of your Google Sheet: ");
                url = Console.ReadLine();

                _modifiedUrl.Clear();

                if (url.Contains("#gid="))
                {
                    _modifiedUrl.Append(url.Substring(0, url.IndexOf("/edit?")));
                    _modifiedUrl.Append("/export?format=csv&gid=");
                    _modifiedUrl.Append(url.Substring(url.IndexOf("#gid=") + (new String("#gid=").Length)));

                    try
                    {
                        PrimaryUser.MailingMethodsEngine._urlInfoAsString = await PrimaryUser.AccountInformationEngine.UserHttpClient.GetStringAsync(ModifiedUrl);

                        if (!String.IsNullOrWhiteSpace(PrimaryUser.MailingMethodsEngine._urlInfoAsString))
                        {
                            success = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to process information ({0})", e.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid URL\n");
                }
            }
        }

        public static async Task<bool> ReadCsvAndLogOrchestraInformation(User PrimaryUser)
        {
            //Data from the URL is processed and added to the PrimaryUser object
            //This method is part of a While loop in the Main method -- false/true is returned to break the loop dependent upon successful parsing

            String[] emailFilters = { ".net", ".com", ".edu", ".org" };

            try
            {
                await Task.Run(() =>
                {
                    //Gathers all orchestra names and emails 
                    using var reader = new StringReader(PrimaryUser.MailingMethodsEngine._urlInfoAsString);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                    var orchestraRecords = csv.GetRecords<Orchestra>()
                        .Where(orch => !String.IsNullOrWhiteSpace(orch.OrchestraEmail) && orch.OrchestraEmail.Contains('@'))
                        .Where(orch => emailFilters.Any(f => orch.OrchestraEmail.Contains(f, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(orch => orch.OrchestraName)
                        .DistinctBy(orch => orch.OrchestraEmail.ToLower().Trim())
                        .Select(orch =>
                        {
                            orch.OrchestraName = orch.OrchestraName.Trim();
                            return orch;
                        });

                    foreach (var record in orchestraRecords)
                    {
                        PrimaryUser.AllOrchestras.Add(record);
                    }
                });

                await Task.Run(() =>
                {
                    //Could use a try/catch here to handle any exceptions that may arise from reading the file, such as file not found or access denied (directory.exists?)
                    //Generating email message
                    using (StreamReader? readingMessage = new StreamReader(@""))
                    {
                        while (readingMessage.EndOfStream == false)
                        {
                            String? currentLine = readingMessage.ReadLine();
                            if (String.IsNullOrWhiteSpace(currentLine))
                            {
                                PrimaryUser.MailingMethodsEngine._emailMessage += "<br>";
                                continue;
                            }
                            PrimaryUser.MailingMethodsEngine._emailMessage += currentLine + "<br>";
                        }
                        PrimaryUser.MailingMethodsEngine._emailMessage += PrimaryUser.HtmlSignatureProperty;
                    }
                });
                //2 locations for email??
                PrimaryUser.AccountInformationEngine.EmailMessage = PrimaryUser.MailingMethodsEngine._emailMessage;
                return false;
            }
            catch
            {
                Console.WriteLine("Error processing emails");
                return true;
            }
        }

        public static void EmailSendingSequence(User PrimaryUser)
        {
            //Displays all emails and requests user confirmation prior to send -- this confirmation can probably be removed eventually (unittest for C#? Maybe integrate Python?)

            foreach (Orchestra o in PrimaryUser)
            {
                //At this point, 'emailMessage' is already created from the async method, and the HTML signature is generated and attached to the emailMessage
                //Maybe call a new method here? Pass in o, generate new message, and return
                String updatedMailMessage = PrimaryUser.AccountInformationEngine.EmailMessage.Replace("#ORCHESTRA#", o.OrchestraName.Trim()); // Replace keyword with orchestra's name to add personalization to email
                String previewText = updatedMailMessage
                    .Replace("<br>", "\n")
                    .Replace("<BR>", "\n")
                    .Replace("&nbsp;", " ")
                    .Trim();

                previewText = Regex.Replace(previewText, "<[^>]*>", "");
                previewText = Regex.Replace(previewText, @"(\r?\n\s*){3,}", "\n\n");

                PrimaryUser.AccountInformationEngine.UserMailMessage.Body = updatedMailMessage; // The actual complete email message -- modified for personalization and with HTML signature 

                try
                {
                    //This needs to be updated prior to send 
                    PrimaryUser.AccountInformationEngine.UserMailMessage.To.Clear();
                    PrimaryUser.AccountInformationEngine.UserMailMessage.To.Add(""); // test email
                    //mainUser.MailMessageProperty.To.Add(o.OrchestraEmail); // primary send email

                    Console.WriteLine($"EMAIL PREVIEW TO: {o.OrchestraName} : {o.OrchestraEmail}\n");
                    Console.WriteLine(previewText);
                    Console.Write("Press Y to send email: ");

                    ConsoleKeyInfo sendEmail = Console.ReadKey();
                    if (sendEmail.Key == ConsoleKey.Y)
                    {
                        PrimaryUser.AccountInformationEngine.UserSmtpClient.Send(PrimaryUser.AccountInformationEngine.UserMailMessage);
                        Console.WriteLine($"\nSuccessfully sent email to: {o.OrchestraEmail}" + $"\n{LoadingAndDisplay.divider}\n");

                        PrimaryUser.SuccessfulEmails.Add(o);
                    }
                    else
                    {
                        Console.WriteLine($"\nEmail not sent to: {o.OrchestraEmail}" + $"\n{LoadingAndDisplay.divider}\n");

                        PrimaryUser.FailedEmails.Add(o);
                        continue;
                    }

                    //2 transcript locations??
                    PrimaryUser.MailingMethodsEngine._transcript += $"Sent to: {o.OrchestraName}  :  {o.OrchestraEmail}\n\n" + previewText
                        + "\n" + $"ORCHESTRA keyword should be replaced with: {o.OrchestraName}" + $"\n{LoadingAndDisplay.divider}\n";
                }
                catch (SmtpException ex)
                {
                    Console.WriteLine($"SMTP Error sending to: {o.OrchestraEmail}: {ex.StatusCode} - {ex.Message}");
                    PrimaryUser.FailedEmails.Add(o);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General error sending to: {o.OrchestraEmail}: {ex.Message}");
                    PrimaryUser.FailedEmails.Add(o);
                }

                PrimaryUser.TranscriptEngine = new TranscriptEngine(PrimaryUser.MailingMethodsEngine._transcript);
            }
        }
    }
}
