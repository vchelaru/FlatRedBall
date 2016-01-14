using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace BuildServerUploaderConsole.Processes
{
    public class EmailResults : IResults
    {
        private readonly StringBuilder _bldr = new StringBuilder();

        public void WriteMessage(string message)
        {
            _bldr.AppendLine(message);
            System.Console.WriteLine(message);
        }

        public void Send()
        {
            var message = new MailMessage
                              {
                                  From = new MailAddress("frbbuildserver@gmail.com"), 
                                  Subject = "Daily Build", 
                                  BodyEncoding = Encoding.UTF8,
                                  Body = _bldr.ToString()
                              };

            //message.To.Add(new MailAddress("dancer.scott@gmail.com"));
            message.To.Add(new MailAddress("vicchelaru@gmail.com"));

            var credential = new NetworkCredential("frbbuildserver@gmail.com", "@Fr2232B@", "");
            var client = new SmtpClient("smtp.gmail.com", 587)
                             {
                                 EnableSsl = true,
                                 UseDefaultCredentials = false,
                                 Timeout = 20000,
                                 DeliveryMethod = SmtpDeliveryMethod.Network,
                                 Credentials = credential
                             };

            try
            {
#if EMAIL
                client.Send(message);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine(_bldr.ToString());
                Console.WriteLine(@"Failed to send email: " + ex.ToString());
            }
        }
    }
}
