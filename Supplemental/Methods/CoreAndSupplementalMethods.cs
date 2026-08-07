using Email_Automation.Engines;
using Email_Automation.PrimaryUser;
using Email_Automation_Update.Supplemental.Methods;
using MimeKit;
using OrchestraInformation;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;


namespace Email_Automation_Update.Supplemental.Methods
{
    internal class LoadingAndDisplay
    {
        public static string divider = new string('_', Console.WindowWidth);

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
            int iterator = 0;

            PrimaryUser.AllOrchestrasFromRecord.ForEach(o => Console.WriteLine("{0, 3: ##0}. {1, -45} : {2, -45}", ++iterator, o.OrchestraName, o.Email));

            PrimaryUser.LoadingAndDisplayEngine.Warning = delegate (User PrimaryUser)
            {
                string notice = $"\aYOU ARE ABOUT TO EMAIL THIS NUMBER OF RECIPIENTS: {iterator}";

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
        public string EmailMessage { get; set; } = "";
        public string Transcript { get; set; } = "";
        public string UrlInfoAsString { get; set; } = "";
        public static StringBuilder ModifiedUrl = new StringBuilder();
        public static string MessageDirectory { get; set; } = @"";

        //Methods
        [Obsolete("This method was used in the previous version, but may be reimplemented later")]
        public static async Task BuildURL(User PrimaryUser)
        {
            //URL for CSV task is built here, and this method will continue until
            //(1) an appropriately formatted URL is entered, and (2) data is pulled from the URL
            string url;
            bool success = false;

            while (!success)
            {
                Console.Write("Enter URL of your Google Sheet: ");
                url = Console.ReadLine();

                ModifiedUrl.Clear();

                if (url.Contains("#gid="))
                {
                    ModifiedUrl.Append(url.Substring(0, url.IndexOf("/edit?")));
                    ModifiedUrl.Append("/export?format=csv&gid=");
                    ModifiedUrl.Append(url.Substring(url.IndexOf("#gid=") + new string("#gid=").Length));

                    try
                    {
                        PrimaryUser.MailingMethodsEngine.UrlInfoAsString = await PrimaryUser.AccountInformationEngine.UserHttpClient.GetStringAsync(ModifiedUrl.ToString());

                        if (!string.IsNullOrWhiteSpace(PrimaryUser.MailingMethodsEngine.UrlInfoAsString))
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

        public static void LoadEmailMessage(User PrimaryUser)
        {
            try
            {
                if (File.Exists(MessageDirectory))
                {
                    using (StreamReader readingMessage = new StreamReader(MessageDirectory))
                    {
                        while (readingMessage.EndOfStream == false)
                        {
                            string currentLine = readingMessage.ReadLine();
                            if (string.IsNullOrWhiteSpace(currentLine))
                            {
                                PrimaryUser.MailingMethodsEngine.EmailMessage += "<br>";
                                continue;
                            }
                            PrimaryUser.MailingMethodsEngine.EmailMessage += currentLine + "<br>";
                        }
                        PrimaryUser.MailingMethodsEngine.EmailMessage += PrimaryUser.HtmlSignatureProperty;

                        //2 locations for email??
                        PrimaryUser.AccountInformationEngine.EmailMessage = PrimaryUser.MailingMethodsEngine.EmailMessage;
                    }
                }
                else
                {
                    throw new FileNotFoundException("Could not locate your file");
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine($"\nError processing request: {e.Message}");
                Environment.Exit(1);
            }
        }
        public static void EmailSendingSequence(User PrimaryUser)
        {
            //Displays all emails and requests user confirmation prior to send -- this confirmation can probably be removed eventually (unittest for C#? Maybe integrate Python?)
            foreach (OrchestraRecord o in PrimaryUser)
            {
                PrimaryUser.AccountInformationEngine.UserMimeMessage = new MimeMessage();
                PrimaryUser.AccountInformationEngine.MimeMessageInitialization();

                //At this point, 'emailMessage' is already created from the async method, and the HTML signature is generated and attached to the emailMessage
                //Maybe call a new method here? Pass in o, generate new message, and return
                string updatedMailMessage = PrimaryUser.AccountInformationEngine.EmailMessage.Replace("#ORCHESTRA#", o.OrchestraName.Trim()); // Replace keyword with orchestra's name to add personalization to email
                string previewText = updatedMailMessage
                    .Replace("<br>", "\n")
                    .Replace("<BR>", "\n")
                    .Replace("&nbsp;", " ")
                    .Trim();

                previewText = Regex.Replace(previewText, "<[^>]*>", "");
                previewText = Regex.Replace(previewText, @"(\r?\n\s*){3,}", "\n\n");

                PrimaryUser.AccountInformationEngine.UserMimeMessage.Body = new TextPart("html")
                {
                    Text = updatedMailMessage // The actual complete email message -- modified for personalization and with HTML signature 
                };

                try
                {
                    //This needs to be updated prior to send 
                    PrimaryUser.AccountInformationEngine.UserMimeMessage.To.Add(new MailboxAddress(null, "")); // test email
                    //PrimaryUser.AccountInformationEngine.UserMimeMessage.To.Add(new MailboxAddress(null, o.OrchestraEmail)); // primary send email

                    Console.WriteLine($"EMAIL PREVIEW TO: {o.OrchestraName} : {o.Email}\n");
                    Console.WriteLine(previewText);
                    Console.Write("Press Y to send email: ");

                    ConsoleKeyInfo sendEmail = Console.ReadKey();
                    if (sendEmail.Key == ConsoleKey.Y)
                    {
                        PrimaryUser.AccountInformationEngine.UserSmtpClient.Send(PrimaryUser.AccountInformationEngine.UserMimeMessage);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine($"\nSuccessfully sent email to: {o.Email}");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"\n{LoadingAndDisplay.divider}\n");


                        PrimaryUser.SuccessfulEmails.Add(o);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"\nEmail not sent to: {o.Email}");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"\n{LoadingAndDisplay.divider}\n");

                        PrimaryUser.FailedEmails.Add(o);
                        continue;
                    }

                    //2 transcript locations??
                    PrimaryUser.MailingMethodsEngine.Transcript += $"Sent to: {o.OrchestraName}  :  {o.Email}\n\n" + previewText
                        + "\n" + $"ORCHESTRA keyword should be replaced with: {o.OrchestraName}" + $"\n{LoadingAndDisplay.divider}\n";
                }
                catch (SmtpException ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"\nSMTP Error sending to: {o.Email}: {ex.StatusCode} - {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                    PrimaryUser.FailedEmails.Add(o);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"\nGeneral error sending to: {o.Email}: {ex.Message}\n");
                    Console.ForegroundColor = ConsoleColor.White;
                    PrimaryUser.FailedEmails.Add(o);
                }

                PrimaryUser.TranscriptEngine = new TranscriptEngine(PrimaryUser.MailingMethodsEngine.Transcript);
            }
        }
    }
}