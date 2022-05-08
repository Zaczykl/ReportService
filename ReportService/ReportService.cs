using Cipher;
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
        private int _sendHour;
        private int _intervalInMinutes;
        private bool _toSend;
        private const int MILISECONDS_IN_ONE_MINUTE = 60 * 1000;
        private Timer _timer;
        private ErrorRepository _errorRepository = new ErrorRepository();
        private ReportRepository _reportRepository = new ReportRepository();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Email _email;
        private GenerateHtmlEmail _generateHtml = new GenerateHtmlEmail();
        private string emailReceiver;
        private StringCipher _stringCipher = new StringCipher("CABE99B7-721A-4934-B7C4-001E2B3DA53E");
        private const string NOT_ENCRYPTED_PASSWORD_PREFIX = "encrypt:";

        public ReportService()
        {
            InitializeComponent();

            try
            {
                _toSend = Convert.ToBoolean(ConfigurationManager.AppSettings["toSend"]);
                _sendHour = Convert.ToInt32(ConfigurationManager.AppSettings["sendHour"]);
                emailReceiver = ConfigurationManager.AppSettings["ReceiverEmail"];
                _intervalInMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalInMinutesForSendingErrors"]);
                _timer = new Timer(_intervalInMinutes * MILISECONDS_IN_ONE_MINUTE);
                _email = new Email(new EmailParams
                {
                    HostSmtp = ConfigurationManager.AppSettings["HostSmtp"],
                    Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]),
                    EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSsl"]),
                    SenderName = ConfigurationManager.AppSettings["SenderName"],
                    SenderEmail = ConfigurationManager.AppSettings["SenderEmail"],
                    SenderEmailPassword = DecryptSenderEmailPassword()
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private string DecryptSenderEmailPassword()
        {
            var encryptedPassword = ConfigurationManager.AppSettings["SenderEmailPassword"];

            if (encryptedPassword.StartsWith(NOT_ENCRYPTED_PASSWORD_PREFIX))
            {
                encryptedPassword = _stringCipher.Encrypt(encryptedPassword.Replace(NOT_ENCRYPTED_PASSWORD_PREFIX, ""));
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configFile.AppSettings.Settings["SenderEmailPassword"].Value = encryptedPassword;
                configFile.Save();
            }
            return _stringCipher.Decrypt(encryptedPassword);
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
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private async Task SendError()
        {
            var errors = _errorRepository.GetLastErrors(_intervalInMinutes);
            if (errors == null || !errors.Any())
                return;

            await _email.Send("Błędy w aplikacji", _generateHtml.GenerateErrors(errors, _intervalInMinutes), emailReceiver);
            Logger.Info("Error sent.");
        }

        private async Task SendReport()
        {
            if (!_toSend)
                return;

            var actualHour = DateTime.Now.Hour;
            if (actualHour < _sendHour)
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
