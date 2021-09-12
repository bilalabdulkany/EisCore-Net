using EisCore.Application.Interfaces;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;

namespace EisCore.Infrastructure.Persistence
{
    public class DatabaseBootstrap : IDatabaseBootstrap
    {

        private string _databaseName;
        private IConfiguration _configuration;
        public DatabaseBootstrap(IConfiguration configuration)
        {
            this._configuration = configuration;
            _databaseName = this._configuration["DatabaseSource"];
        }
        public void Setup()
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                var table = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Event';");
                var tableName = table.FirstOrDefault();
                if (!string.IsNullOrEmpty(tableName) && tableName == "Event")
                    return;

                connection.Execute("Create Table Event (" +
                    "Id VARCHAR(100) NOT NULL," +
                    "Code VARCHAR(100) NOT NULL," +
                    "Name VARCHAR(100) NOT NULL," +
                    "Description VARCHAR(1000) NULL);");
                Console.WriteLine($"Created Dabatase {_databaseName}");

            }

        }
    }
}