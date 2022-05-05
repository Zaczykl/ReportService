using EmailSender;
using ReportService.Core;
using ReportService.Core.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
        private string emailReceiver;

        public ReportService()
        {
            InitializeComponent();            
            
            try
            {
                emailReceiver = ConfigurationManager.AppSettings["ReceiverEmail"];

                _email = new Email(new EmailParams
                {
                    HostSmtp = ConfigurationManager.AppSettings["HostSmtp"],
                    Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]),
                    EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSsl"]),
                    SenderName = ConfigurationManager.AppSettings["SenderName"],
                    SenderEmail = ConfigurationManager.AppSettings["SenderEmail"],
                    SenderEmailPassword = ConfigurationManager.AppSettings["SenderEmailPassword"],
                });
            }
            catch (Exception ex)
            {

                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
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

            await _email.Send("Błędy w aplikacji", _generateHtml.GenerateErrors(errors, INTERVAL_IN_MINUTES),emailReceiver);
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

            await _email.Send("Raport dobowy", _generateHtml.GenerateReport(report), emailReceiver);
            _reportRepository.ReportSent(report);
            Logger.Info("Report sent.");
        }

        protected override void OnStop()
        {
            Logger.Info("Service stopped...");
        }
    }
}
