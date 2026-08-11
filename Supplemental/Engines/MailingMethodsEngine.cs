using Email_Automation.PrimaryUser;
using OrchestraInformation;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Email_Automation_Update.Supplemental.Engines
{
    internal class MailingMethodsEngine
    {
        private readonly User _user;
        public User PrimaryUser => _user;

        public static StringBuilder ModifiedUrl = new StringBuilder();

        public static IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<MailingMethodsEngine>()
            .Build();

        public string Transcript { get; set; } = "";
        public string UrlInfoAsString { get; set; } = "";
        public static string EmailMessageText { get; set; } = config["User:emailMessageText"];

        public MailingMethodsEngine(User user)
        {
            this._user = user;
        }

        public void LoadEmailMessage()
        {
            try
            {
                if (EmailMessageText is not null)
                {
                    PrimaryUser.AccountInformationEngine.EmailMessage += EmailMessageText += PrimaryUser.HtmlSignatureProperty;
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
        public void EmailSendingSequence()
        {
            //Maybe add another method for reconneciton logic if the conneciton is dropped?
            PrimaryUser.AccountInformationEngine.ClientInitialization(); //smtp client initialization

            //Displays all emails and requests user confirmation prior to send -- this confirmation can probably be removed eventually (unittest for C#? Maybe integrate Python?)
            foreach (OrchestraRecord o in PrimaryUser)
            {
                PrimaryUser.AccountInformationEngine.UserMimeMessage = new MimeMessage();
                PrimaryUser.AccountInformationEngine.MimeMessageInitialization();

                PrimaryUser.AccountInformationEngine.UserMimeMessage.Body = new TextPart("html")
                {
                    // The actual complete email message -- modified for personalization and with HTML signature
                    Text = PrimaryUser.AccountInformationEngine.EmailMessage.Replace("#ORCHESTRA#", o.OrchestraName.Trim()) 
                };

                string previewText = PrimaryUser.AccountInformationEngine.UserMimeMessage.GetTextBody(MimeKit.Text.TextFormat.Html).Replace("<br>", "\n");
                previewText = Regex.Replace(previewText, "<[^>]*>", "");
                previewText = Regex.Replace(previewText, @"(\r?\n\s*){3,}", "\n\n");

                try
                {
                    //This needs to be updated prior to send 
                    PrimaryUser.AccountInformationEngine.UserMimeMessage.To.Add(new MailboxAddress(null, config["User:email"])); // test email
                    //PrimaryUser.AccountInformationEngine.UserMimeMessage.To.Add(new MailboxAddress(null, o.Email)); // primary send email

                    string recipientEmail = TestOrLiveEmail();

                    Console.WriteLine($"EMAIL PREVIEW TO: {o.OrchestraName} : {recipientEmail}\n");
                    Console.WriteLine(previewText);
                    Console.Write("Press Y to send email: ");

                    ConsoleKeyInfo sendEmail = Console.ReadKey();
                    if (sendEmail.Key == ConsoleKey.Y)
                    {
                        PrimaryUser.AccountInformationEngine.UserSmtpClient.Send(PrimaryUser.AccountInformationEngine.UserMimeMessage);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine($"\nSuccessfully sent email to: {o.Email}");
                        Console.ResetColor();
                        Console.WriteLine($"\n{LoadingAndDisplayEngine.divider}\n");


                        PrimaryUser.SuccessfulEmails.Add(o);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"\nEmail not sent to: {o.Email}");
                        Console.ResetColor();
                        Console.WriteLine($"\n{LoadingAndDisplayEngine.divider}\n");

                        PrimaryUser.FailedEmails.Add(o);
                        continue;
                    }

                    PrimaryUser.TranscriptEngine.Value.Transcript += $"Sent to: {o.OrchestraName}  :  {o.Email}\n\n" + previewText
                        + "\n" + $"ORCHESTRA keyword should be replaced with: {o.OrchestraName}" + $"\n{LoadingAndDisplayEngine.divider}\n";
                }
                catch (SmtpException ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"\nSMTP Error sending to: {o.Email}: {ex.StatusCode} - {ex.Message}");
                    Console.ResetColor();
                    PrimaryUser.FailedEmails.Add(o);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"\nGeneral error sending to: {o.Email}: {ex.Message}\n");
                    Console.ResetColor();
                    PrimaryUser.FailedEmails.Add(o);
                }
            }
        }

        public string TestOrLiveEmail()
        {
            IList<MimeKit.MailboxAddress> recip = PrimaryUser.AccountInformationEngine.UserMimeMessage.GetRecipients();
            string? recipientEmail = recip.FirstOrDefault()?.Address;

            var ColorAndEmail = (recipientEmail == config["User:email"])
                ? (Color: ConsoleColor.Green, Text: $"In test mode ({recipientEmail})")
                : (Color: ConsoleColor.Red, Text: $"Sending live emails ({recipientEmail})");

            Console.ForegroundColor = ColorAndEmail.Color;
            Console.WriteLine(ColorAndEmail.Text);
            Console.ResetColor();

            return recipientEmail;
        }
    }
}