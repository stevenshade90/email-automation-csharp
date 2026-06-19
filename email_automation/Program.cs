using System.Net.Mail;


namespace email_automation
{
    public class MassEmailProgram
    {
        static void Main(string[] args)
        {
            //Retrive password from Environment Variables
            String? myPassword = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD");
            if (String.IsNullOrEmpty(myPassword))
            {
                Console.WriteLine("Could not find the environment variable. Exiting program");
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("Credentials Confirmed\n");
            }

            //1. Client and mailing objects initializing
            int smtpPort = 587;
            String smtpHost = "smtp.gmail.com";
            String senderEmail = "";
            String senderPassword = myPassword;
            String senderDisplayName = "";

            SmtpClient client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(senderEmail, senderPassword),
                Timeout = 10000
            };

            MailAddress fromAddress = new MailAddress(senderEmail, senderDisplayName);
            MailMessage mailMessage = new MailMessage()
            {
                From = fromAddress,
                Subject = "Orchestral Performance Collaboration",
                Body = "", //Filled later prior to send
                IsBodyHtml = true
            };


            //2. Stored and cleaned-up emails
            Dictionary<String, String> testEmails = new Dictionary<String, String>();
            List<String> failedEmails = new List<String>();

            //Location of email list, processing orchestra names and emails stored as CSV exported from SQL
            String emailPath = @"";
            using (StreamReader reader = new StreamReader(emailPath))
            {
                //All names and emails read from 'path' and appended to dictionary
                while (reader.EndOfStream == false)
                {
                    char delimiter = ',';
                    String orchestraAndEmails = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(orchestraAndEmails))
                    {
                        continue;
                    }

                    String[] keysAndValues = orchestraAndEmails.Split(delimiter);
                    String orchestraName = keysAndValues[0].Trim();
                    String orchestraEmail = keysAndValues[1].Trim();

                    //Redundancy check
                    if (testEmails.ContainsValue(orchestraEmail) == true) { }
                    //Only add emails with the appropriate parts
                    else if (orchestraEmail.Contains("@") && (orchestraEmail.Contains(".com", StringComparison.OrdinalIgnoreCase) 
                        || orchestraEmail.Contains(".net", StringComparison.OrdinalIgnoreCase) 
                        || orchestraEmail.Contains(".org", StringComparison.OrdinalIgnoreCase)
                        || orchestraEmail.Contains(".edu", StringComparison.OrdinalIgnoreCase)))
                    {
                        testEmails.Add(orchestraName, orchestraEmail);
                    }
                }
            }


            //3. Email send notice
            int enumerate = 1;
            int numberOfRecipients = 0;
            foreach (KeyValuePair<String, String> email in testEmails)
            {
                Console.WriteLine("{0, 3: ##0}. {1, -45} : {2, -45}", enumerate, email.Key, email.Value);
                enumerate++;
                numberOfRecipients++;
            }

            String notice = $"\aYOU ARE ABOUT TO SEND AN EMAIL TO THIS NUMBER OF RECIPIENTS: {numberOfRecipients}";
            Console.WriteLine($"{new String('_', notice.Length)}\n");
            Console.WriteLine($"\n{notice}");
            Console.Write("ENTER 'Y' TO CONTINUE: ");
            ConsoleKeyInfo keyPressed = Console.ReadKey();
            if (keyPressed.Key == ConsoleKey.Y)
            { 
                Console.WriteLine("\n\nContinuing with send...\n");
                Console.WriteLine($"{new String('_', notice.Length)}\n");
            }
            else
            {
                Console.WriteLine($"\n\nYou pressed: {keyPressed.KeyChar}");
                Console.WriteLine("Terminating program");
                Environment.Exit(0);
            }


            //4. Email sending sequence, process message to be sent
            String transcript = "";
            String emailMessage = "";

            //This is the location of the draft message that will be sent to all orchestras 
            using (StreamReader readingMessage = new StreamReader(@""))
            {
                while (readingMessage.EndOfStream == false)
                {
                    String currentLine = readingMessage.ReadLine();
                    if (String.IsNullOrWhiteSpace(currentLine))
                    {
                        emailMessage = emailMessage + "<br>";
                        continue;
                    }
                    emailMessage = emailMessage + currentLine + "<br>";
                }
            }

            //Email signature
            String youtubePageUrl = "";
            String youtubeIconUrl = "";

            // Construct the HTML Signature
            String htmlSignature = $@"
<br>
<div style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>
<strong style='color: darkblue;'> <NAME> </strong><br>
<Subtitle> <br>
__________________<br>
<span style='color: darkblue;'>c</span> <PHONE #> <br><br>
<a href='{youtubePageUrl}' style='text-decoration: none; color: #1a0dab;'>
<img src='{youtubeIconUrl}' alt='YouTube' width='40' height='28' style='border:0; vertical-align: middle;'>
</a>
</div>
<br>";

            // Append the signature to main message
            emailMessage = emailMessage + htmlSignature;


            //4.1. Main message body prepared as "emailMessage," ready to be modified prior to send 
            foreach (KeyValuePair<String, String> orchestraNameAndEmail in testEmails)
            {
                try
                { 
                    String updatedMailMessage = emailMessage.Replace("#ORCHESTRA#", orchestraNameAndEmail.Key); // Replace keyword with orchestra's name to add personalization to email
                    String previewText = updatedMailMessage;

                    mailMessage.To.Clear();

                    //mailMessage.To.Add("TEST EMAIL"); // test email
                    mailMessage.To.Add(orchestraNameAndEmail.Value); // primary send email

                    //Cleanup and strip HTML formatting
                    previewText = previewText.Replace("<br>", "\n");
                    previewText = previewText.Replace("<BR>", "\n");
                    previewText = previewText.Replace("&nbsp;", " ");
                    previewText = previewText.Trim();
                    previewText = System.Text.RegularExpressions.Regex.Replace(previewText, "<[^>]*>", "");
                    previewText = System.Text.RegularExpressions.Regex.Replace(previewText, @"(\r?\n\s*){3,}", "\n\n");

                    mailMessage.Body =  updatedMailMessage;

                    Console.WriteLine($"EMAIL PREVIEW TO: {orchestraNameAndEmail.Key} : {orchestraNameAndEmail.Value}\n");
                    Console.WriteLine(previewText);
                    Console.Write("Press Y to send email: ");

                    ConsoleKeyInfo sendEmail = Console.ReadKey();
                    if (sendEmail.Key == ConsoleKey.Y)
                    {
                        client.Send(mailMessage);
                        Console.WriteLine($"\nSuccessfully sent email to: {orchestraNameAndEmail.Value}");
                        Console.WriteLine($"{new String('_', Console.WindowWidth)}\n");
                    }
                    else
                    {
                        Console.WriteLine($"\nEmail not sent to: {orchestraNameAndEmail.Value}");
                        Console.WriteLine($"{new String('_', Console.WindowWidth)}\n");

                        testEmails.Remove(orchestraNameAndEmail.Key);
                        failedEmails.Add(orchestraNameAndEmail.Value); 
                        continue;
                    }

                    transcript += $"Sent to: {orchestraNameAndEmail.Key} : {orchestraNameAndEmail.Value}\n\n" + previewText
                        + "\n" + $"ORCHESTRA keyword should be replaced with: {orchestraNameAndEmail.Key}\n"
                        + new String('-', Console.WindowWidth) + "\n";
                }
                catch (SmtpException ex)
                {
                    Console.WriteLine($"SMTP Error sending to: {orchestraNameAndEmail.Value}: {ex.StatusCode} - {ex.Message}");
                    testEmails.Remove(orchestraNameAndEmail.Key);
                    failedEmails.Add(orchestraNameAndEmail.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General error sending to: {orchestraNameAndEmail.Value}: {ex.Message}");
                    testEmails.Remove(orchestraNameAndEmail.Key);
                    failedEmails.Add(orchestraNameAndEmail.Value);
                }
                Thread.Sleep(1000);
            }


            //4.2. Email sending results 
            Console.WriteLine("\n--- Sending Complete ---");
            if (failedEmails.Count > 0)
            {
                Console.WriteLine($"Emails failed to send ({failedEmails.Count}):");
                foreach (String email in failedEmails)
                {
                    Console.WriteLine($"- {email}");
                }
            }
            else
            {
                Console.WriteLine("All emails sent successfully");
            }


            //5. Export results to desktop
            String dateAndTime = DateTime.Now.ToLongDateString();
            String desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            String fullPath = Path.Combine(desktopPath, "results.html");

            try
            {
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    //Document header
                    writer.WriteLine("<html><body>");
                    writer.WriteLine($"<H1>Emailing Results</H1>");
                    writer.WriteLine($"<H3>Date: {dateAndTime}</H3>");
                    writer.WriteLine($"<H3><B><u><span style=\"color: green;\">Successful Emails: {testEmails.Count()} of {testEmails.Count() + failedEmails.Count()}</span></u></B></H3>");

                    //List successful emails
                    foreach (KeyValuePair<String,String> nameAndEmail in testEmails)
                    {
                        writer.WriteLine("&nbsp;&nbsp;&nbsp;&nbsp;[+] {0} : {1}<br>", nameAndEmail.Key, nameAndEmail.Value);
                    }

                    //List failed emails
                    writer.WriteLine($"<H3><B><u><span style=\"color: red;\">Failed Emails: {failedEmails.Count()}</span></u></B></H3>");
                    foreach (String email in failedEmails)
                    {
                        writer.WriteLine($"&nbsp;&nbsp;&nbsp;&nbsp;[-] {email}<br>");
                    }
                    writer.WriteLine("</body></html>");
                }
                Console.WriteLine(@$"Results saved to: {fullPath}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            //5.1 Write test emails to desktop for testing verification, should write all email messages added to transcript string
            String testResultsPath = Path.Combine(desktopPath, "testEmailResults.txt");
            using (StreamWriter testWriter = new StreamWriter(testResultsPath))
            {
                testWriter.WriteLine(transcript);
            }


            //6. Resource management
            if (client != null)
            {
                client.Dispose();
            }
            if (mailMessage != null)
            {
                mailMessage.Dispose();
            }
        }
    } 
}