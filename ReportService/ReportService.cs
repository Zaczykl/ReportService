using EmailSender;
using ReportService.Core;
using ReportService.Core.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ReportService
{
    
    public partial class ReportService : ServiceBase
    {
        private const int SEND_HOUR = 13;
        private const int INTERVAL_IN_MINUTES = 30;
        private const int MILISECONDS_IN_ONE_MINUTE= 60 * 1000;
        private Timer _timer = new Timer(INTERVAL_IN_MINUTES * MILISECONDS_IN_ONE_MINUTE);
        private ErrorRepository _errorRepository=new ErrorRepository();
        private ReportRepository _reportRepository=new ReportRepository();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Email _email;
        private GenerateHtmlEmail _generateHtml = new GenerateHtmlEmail();
        private const string _emailReceiver = "zaczyk.l@gmail.com";

        public ReportService()
        {
            InitializeComponent();
            _email = new Email(new EmailParams
            {
                HostSmtp = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                SenderName = "ReportService",
                SenderEmail = "reportservice85@gmail.com",
                SenderEmailPassword = "poardkxbjubgxcbd"
            });
        }

        protected override void OnStart(string[] args)
        {

            _timer.Elapsed += DoWork;
            _timer.Start();
            Logger.Info("Service started...");
        }

        private async void DoWork(object sender, ElapsedEventArgs e)
        {            
            try
            {
                await SendError();
                await SendReport();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private async Task SendError()
        {
            var errors=_errorRepository.GetLastErrors(INTERVAL_IN_MINUTES);
            if (errors == null || !errors.Any())
                return;

            await _email.Send("Błędy w aplikacji", _generateHtml.GenerateErrors(errors, INTERVAL_IN_MINUTES),_emailReceiver);
            Logger.Info("Error sent.");
        }

        private async Task SendReport()
        {
            var actualHour = DateTime.Now.Hour;
            if (actualHour < SEND_HOUR)
                return;

            var report = _reportRepository.GetLastNotSendReport();
            if (report == null)
                return;

            await _email.Send("Raport dobowy", _generateHtml.GenerateReport(report), _emailReceiver);
            _reportRepository.ReportSent(report);
            Logger.Info("Report sent.");
        }

        protected override void OnStop()
        {
            Logger.Info("Service stopped...");
        }
    }
}
