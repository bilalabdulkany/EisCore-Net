using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using EisCore.Application.Interfaces;
using EisCore.Application.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EisCore.Infrastructure.Persistence
{
    public class ApplicationDbContext : IApplicationDbContext
    {


        private string _databaseName;
        private ILogger<ApplicationDbContext> _log;
         private IConfiguration _configuration;
        

        public ApplicationDbContext(ILogger<ApplicationDbContext> log,IConfiguration configuration)
        {            
            this._log = log;
            this._configuration=configuration;
            _databaseName = this._configuration["DatabaseSource"];                       
        }

        public async Task Create(Event eisevent)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    await connection.ExecuteAsync("INSERT INTO Event (Id,Code, Name,Description)" +
                        " VALUES (@Id,@Code,@Name,@Description)", new {eisevent.Id,eisevent.Code,eisevent.Name,eisevent.Description});
                    _log.LogInformation("Inserted into db {eisevent}", eisevent);
                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);
                }
            }
        }

        public async Task<IEnumerable<Event>> Get()
        {

            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    _log.LogInformation($"Querying from database...");
                    return await connection.QueryAsync<Event>("SELECT Id AS Id, Name, Description FROM Event;");
                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);
                }
                return null;

            }


        }
    }
}