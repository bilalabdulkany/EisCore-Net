using System;
using System.IO;
using EisCore.Model;
using System.Threading;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;

namespace EisCore.Configuration
{
    public class ConfigurationManager : IConfigurationManager
    {

        string queueName;
        string topicName;
        private bool isDisposed = false;

        private ILogger<EventProcessor> _log;
        private BrokerConfiguration _brokerConfiguration;
        private ApplicationSettings _appSettings;

        public ConfigurationManager(ILogger<EventProcessor> log, BrokerConfiguration brokerConfig)
        {

            this._log = log;
            this._brokerConfiguration = brokerConfig;

            _log.LogInformation("ConfigurationManager constructor");
            BindAppSettingsToObjects();
            CreateAsyncBrokerConnection();
        }

        private void BindAppSettingsToObjects()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var configurationBuilder = new ConfigurationBuilder();
            string name = "EisCore.eissettings.json";
            string resourcePath = assembly.GetManifestResourceNames()[0];
            Stream stream = assembly.GetManifestResourceStream(name);
            configurationBuilder.AddJsonStream(stream);
            var brokerConfigSection = configurationBuilder.Build();
            brokerConfigSection.GetSection("BrokerConfiguration").Bind(this._brokerConfiguration);
            var AppSettingsList = new List<ApplicationSettings>();
            brokerConfigSection.GetSection("ApplicationSettings").Bind(AppSettingsList);
            _appSettings = GetAppSettingsFromList(AppSettingsList);

        }
        public void CreateAsyncBrokerConnection()
        {

            //TODO - connection should be created as soon as the DI is injected

            IConnection TcpConnection = null;
            ISession TcpSession = null;
            try
            {
                var brokerUrl = GetBrokerUrl();

                _log.LogInformation("Broker - {brokerUrl}", brokerUrl);

                Uri connecturi = new Uri(brokerUrl);

                IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(connecturi);

                TcpConnection = factory.CreateConnection();
                if (TcpConnection.IsStarted)
                {
                    _log.LogInformation("connection started");

                }

                // TcpConnection.ConnectionInterruptedListener += new ConnectionInterruptedListener(OnConnectionInterruptedListener);
                // TcpConnection.ConnectionResumedListener += new ConnectionResumedListener(OnConnectionResumedListener);
                // TcpConnection.ExceptionListener += new ExceptionListener(OnExceptionListener);


                TcpSession = TcpConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);

                this._brokerConfiguration.connection = TcpConnection;
                this._brokerConfiguration.session = TcpSession;
                _log.LogInformation("##Created connection {con}", this._brokerConfiguration.connection.ToString());
            }
            catch (Apache.NMS.ActiveMQ.ConnectionClosedException e1)
            {
                _log.LogCritical("Connection closed exception thrown while closing a connection. {e1}", e1.StackTrace);
                try
                {
                    _log.LogCritical("Stopping Connection..");
                    TcpConnection.Stop();
                }
                catch (Apache.NMS.ActiveMQ.ConnectionClosedException e2)
                {
                    _log.LogCritical("Cannot close the connection {e2}", e2.StackTrace);
                    TcpConnection.Close();
                }
                finally
                {
                    TcpConnection.Dispose();
                }
            }
            catch (Exception e)
            {
                _log.LogError("Exception occurred {class}: {e}", this.ToString(), e.StackTrace);
            }

        }

        public string GetBrokerUrl()
        {
            if (_brokerConfiguration != null)
            {
                // + _brokerConfiguration.Username + ":" + _brokerConfiguration.Password + "@"
                return _brokerConfiguration.Protocol + _brokerConfiguration.Url;
            }
            return null;
        }
        private ApplicationSettings GetAppSettingsFromList(List<ApplicationSettings> AppSettingsList)
        {

            foreach (ApplicationSettings appSettings in AppSettingsList)
            {
                if (appSettings.Name.Equals(SourceSystemName.MDM.ToString()))
                {
                    return appSettings;
                }
            }
            return null;
        }

        public BrokerConfiguration GetBrokerConfiguration()
        {
            return this._brokerConfiguration;
        }

        public ApplicationSettings GetAppSettings()
        {
            return this._appSettings;
        }

        // protected void OnConnectionInterruptedListener()
        // {
        //     _log.LogDebug("Connection Interrupted.. Trying to reconnect");
        //     // CreateAsyncBrokerConnection();

        // }
        // protected void OnConnectionResumedListener()
        // {
        //     _log.LogDebug("Connection Resumed");
        // }
        // protected void OnExceptionListener(Exception NMSException)
        // {
        //     _log.LogError("On Exception Listener: {e}", NMSException.GetBaseException());
        //     //    CreateAsyncBrokerConnection();
        // }



        // #region IDisposable Members

        // public void Dispose()
        // {
        //     if (!this.isDisposed)
        //     {
        //         this.GetBrokerConfiguration().Dispose();
        //         this.isDisposed = true;
        //     }
        // }


        // #endregion
    }



}