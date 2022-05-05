using EmailSender;
using ReportService.Core;
using ReportService.Core.Doamins;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportService.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var emailReceiver = "zaczyk.l@gmail.com";
            var _generateHtml = new GenerateHtmlEmail();


            var email = new Email(new EmailParams
            {
                HostSmtp = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                SenderName = "ŁZ",
                SenderEmail = "reportservice85@gmail.com",
                SenderEmailPassword = "poardkxbjubgxcbd"
            });

            var errors = new List<Error>
            {
                new Error { Message = "Błąd testowy 1", Date= DateTime.Now, Id = 1 },
                new Error { Message = "Błąd testowy 2", Date= DateTime.Now, Id = 2 }
            };

            var report = new Report
            {
                Id = 1,
                Title = "R/1/2022",
                Date = new DateTime(2022, 1, 1, 12, 0, 0),
                Positions = new List<ReportPosition>
                {
                  new ReportPosition
                  {
                       Id=1,
                       ReportId=1,
                       Title="Position 1",
                       Description="Description 1",
                       Value=43.01m
                  },
                  new ReportPosition
                  {
                       Id=2,
                       ReportId=1,
                       Title="Position 2",
                       Description="Description 2",
                       Value=13.01m
                  },
                  new ReportPosition
                  {
                       Id=3,
                       ReportId=1,
                       Title="Position 3",
                       Description="Description 3",
                       Value=12.99m
                  }
                }
            };

            Console.WriteLine("Wysyłanie e-mail (Raport dobowy)");
            await email.Send("Raport dobowy", _generateHtml.GenerateReport(report), emailReceiver);
            Console.WriteLine("Wysłano e-mail (Raport dobowy)");



            Console.WriteLine("Wysyłanie e-mail (Błędy w aplikacji)");

            await email.Send("Błędy w aplikacji",
                _generateHtml.GenerateErrors(errors,10),
                emailReceiver);
            Console.WriteLine("Wysłano e-mail (Błędy w aplikacji)");

            



        }
    }
}
