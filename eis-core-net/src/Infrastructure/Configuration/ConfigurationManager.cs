using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using EisCore.Application.Constants;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using EisCore.Infrastructure.Persistence;

namespace EisCore.Infrastructure.Configuration
{
    public class ConfigurationManager : IConfigurationManager
    {
       
        private bool isDisposed = false;
        private ILogger<ConfigurationManager> _log;
        private BrokerConfiguration _brokerConfiguration;
        private ApplicationSettings _appSettings;
        private IConfiguration _configuration;
       

        public ConfigurationManager(ILogger<ConfigurationManager> log, IConfiguration configuration)
        {

            this._log = log;
            //this._brokerConfiguration = brokerConfig;
            this._configuration=configuration;

            _log.LogInformation("ConfigurationManager constructor");
            BindAppSettingsToObjects();           
        }

        private void BindAppSettingsToObjects()
        {
            _log.LogInformation("Loading application configurations");
            var assembly = Assembly.GetExecutingAssembly();
            var configurationBuilder = new ConfigurationBuilder();
            string name = "EisCore.eissettings.json";
            string assemblyName = assembly.ManifestModule.Name.Replace(".dll", string.Empty);
            string resourcePath = assembly.GetManifestResourceNames()[0];
            Stream stream = assembly.GetManifestResourceStream(name);
            configurationBuilder.AddJsonStream(stream);
            var brokerConfigSection = configurationBuilder.Build();
            var brokerConfig=new BrokerConfiguration();
            brokerConfigSection.GetSection("BrokerConfiguration").Bind(brokerConfig);
            _brokerConfiguration=brokerConfig;
            var AppSettingsList = new List<ApplicationSettings>();
            brokerConfigSection.GetSection("ApplicationSettings").Bind(AppSettingsList);
            _appSettings = GetAppSettingsFromList(AppSettingsList);
            var environment = this._configuration["environment:profile"];            
            if(environment != null) {
                name = assemblyName + ".eissettings."+ environment + ".json";
                _log.LogInformation("loading : {n}" ,name);
                stream = assembly.GetManifestResourceStream(name);
                configurationBuilder.AddJsonStream(stream);            
            }
            

        }
       
        public string GetBrokerUrl()
        {

            if (this._brokerConfiguration != null)
            {
                // + _brokerConfiguration.Username + ":" + _brokerConfiguration.Password + "@"
                return this._brokerConfiguration.Protocol + this._brokerConfiguration.Url;
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

     



        #region IDisposable Members

        public void Dispose()
        {
            if (!this.isDisposed)
            {               
                this.isDisposed = true;
            }
        }


        #endregion
    }



}