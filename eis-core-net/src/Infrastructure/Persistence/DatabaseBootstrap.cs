using EisCore.Application.Interfaces;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using EisCore.Application.Constants;
using System.Collections.Generic;
using EisCore.Domain.Entities;

namespace EisCore.Infrastructure.Persistence
{
    public class DatabaseBootstrap : IDatabaseBootstrap
    {

        private string _databaseName;
        private readonly IConfiguration _configuration;
        private IEventInboxOutboxDbContext _eventINOUTDbContext;

        public DatabaseBootstrap(IConfiguration configuration, IEventInboxOutboxDbContext eventINOUTDbContext)
        {
            this._configuration = configuration;
            this._eventINOUTDbContext = eventINOUTDbContext;
            _databaseName = this._configuration["DatabaseSource"];
            Setup();
        }
        public void Setup()
        {
            Console.WriteLine("Setup...");
            if (_databaseName == null)
            {
                _databaseName = "Data Source=TestCore.sqlite";
                //throw new Exception("Datbase Source is null");
            }
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {


                    //  var table = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'TEST_COMPETING_CONSUMER_GROUP';");
                    //var tableName = table.FirstOrDefault();
                    // if (!string.IsNullOrEmpty(tableName) && tableName == "TEST_COMPETING_CONSUMER_GROUP")
                    //     return;


                    Console.WriteLine("before executing schema.sql ");

                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "EisCore.schema.sql";

                    string result = "";

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                        //Console.WriteLine(result);	
                    }

                    //string Sqlscript = File.ReadAllText("schema.sql");
                    connection.Execute(result);

                    Console.WriteLine($"Created Dabatase {_databaseName}");

                }
                catch (System.Exception e)
                {
                    Console.WriteLine("Exception connecting to database: " + e.StackTrace);

                }

            }

        }

        public async void InitiateUnprocessedINOUTMessages()
        {
            List<EisEventInboxOutbox> inboxEventList = await _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.IN);
            List<EisEventInboxOutbox> outboxEventList = await _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.OUT);
            if (inboxEventList.Count > 0)
            {
                GlobalVariables.IsUnprocessedInMessagePresent = true;
            }
            if (outboxEventList.Count > 0)
            {
                GlobalVariables.IsUnprocessedOutMessagePresent = true;
            }

        }


    }
}