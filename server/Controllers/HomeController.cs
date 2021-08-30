using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using ServiceStack.Redis;

namespace server.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        private readonly NpgsqlConnection connection;
        private readonly RedisClient redisClient;
        private readonly ILogger<HomeController> logger;

        public HomeController(
            NpgsqlConnection connection,
            RedisClient redisClient,
            ILogger<HomeController> logger)
        {
            this.connection = connection;
            this.redisClient = redisClient;
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok("Hello Gabriel Castillo "+DateTime.Now); 
        }

        [HttpGet]
        [Route("values/all")]
        public IActionResult GetAllValues()
        {
            using var command = this.connection.CreateCommandText("select * from values");

            using var reader = command.ExecuteReader();

            var values = new List<Value>();

            while (reader.Read())
            {
                values.Add(new Value { Number = int.Parse(reader["number"].ToString()) });
            }

            return this.Ok(values);
        }

        [HttpGet]
        [Route("values/current")]
        public IActionResult GetCurrentValues()
        {
            var values = this.redisClient.GetAllEntriesFromHash("values");

            return this.Ok(values);
        }

        [HttpPost]
        [Route("values")]
        public IActionResult CreateValue([FromBody] CreateValue value)
        {
            if(value.Index > 40)
            {
                return this.BadRequest();   
            }

            this.logger.LogInformation("Before adding "+ value.Index);
            this.redisClient.SetEntryInHashIfNotExists("values", value.Index.ToString(), "Nothing yet!");
            this.logger.LogInformation("Before publishing "+ value.Index);
            this.redisClient.PublishMessage("default", value.Index.ToString());


            this.logger.LogInformation("Before inserting to postgress value "+ value.Index);
            using var command = this.connection.CreateCommandText($"INSERT INTO values (number) VALUES ({value.Index})");
            command.ExecuteNonQuery();
            this.logger.LogInformation("After inserting to postgress value "+ value.Index);

            return this.Ok(new { Working = true });
        }
    }

    public class CreateValue
    {
        public int Index { get; set; }
    }

    public static class CommandExtensions
    {
        public static NpgsqlCommand CreateCommandText(this NpgsqlConnection connection, string query)
        {
            if(connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            var command =  connection.CreateCommand();
            command.Connection = connection;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = query;
            return command;
        }
    }
}
