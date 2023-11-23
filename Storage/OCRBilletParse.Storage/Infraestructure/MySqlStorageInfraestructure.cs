using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using OCRBilletParse.Common;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCRBilletParse.Storage.Infraestructure
{
    public class MySqlStorageInfraestructure
    {
        public StringBuilder _sbMainTableScript { get; } = new StringBuilder();
        private ILogger<MySqlStorageInfraestructure> Logger { get; }
        private IConfiguration Config { get; }
        public MySqlStorageInfraestructure(ILogger<MySqlStorageInfraestructure> logger, IConfiguration config)
        {
            Logger = logger;
            Config = config;

            _sbMainTableScript.AppendLine("CREATE TABLE db.Item");
            _sbMainTableScript.AppendLine("(");
            _sbMainTableScript.AppendLine("Key VARCHAR(500) NOT NULL,");
            _sbMainTableScript.AppendLine("Value VARCHAR(MAX) NOT NULL,");
            _sbMainTableScript.AppendLine(");");

            ExecuteScript("DROP TABLE IF EXISTS db.Item");
            ExecuteScript(_sbMainTableScript.ToString());
        }
        public void GetItem(string id) 
        {
            using var mySqlClient = new MySqlConnection();
            mySqlClient.Open();

            string query = "SELECT KEY, VALUE FROM db.Item WHERE KEY = @ID";

            using var cmd = new MySqlCommand();
            cmd.Connection = mySqlClient;
            cmd.CommandText = query;

            cmd.Parameters.AddWithValue("@KEY", id);
            cmd.Prepare();

            using MySqlDataReader reader = cmd.ExecuteReader();
            List<ItemDb> lstItem = new List<ItemDb>();

            while (reader.Read())
            {
                lstItem.Add(new ItemDb()
                {
                    Key = reader["KEY"].ToString(),
                    Value = reader["VALUE"].ToString(),
                });
            }
        }
        public bool Insert(ItemDb itemDb)
        {
            string insertScript = "INSERT INTO db.Item VALUES(@KEY, @VALUE)";

            using var mySqlClient = new MySqlConnection(Config["Services:redis:ConnectionString"]);
            mySqlClient.Open();

            using var cmd = new MySqlCommand();
            cmd.Connection = mySqlClient;
            cmd.CommandText = insertScript;

            cmd.Parameters.AddWithValue("@KEY", itemDb.Key);
            cmd.Parameters.AddWithValue("@VALUE", itemDb.Value);

            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        public bool Remove(string id) 
        {
            string insertScript = "DELETE FROM db.Item WHERE KEY = @KEY";

            using var mySqlClient = new MySqlConnection(Config["Services:redis:ConnectionString"]);
            mySqlClient.Open();

            using var cmd = new MySqlCommand();
            cmd.Connection = mySqlClient;
            cmd.CommandText = insertScript;

            cmd.Parameters.AddWithValue("@KEY", id);

            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        private int ExecuteScript(string script)
        {
            var mySqlClient = new MySqlConnection(Config["Services:redis:ConnectionString"]);
            mySqlClient.Open();
            var cmd = new MySqlCommand();
            cmd.Connection = mySqlClient;

            cmd.CommandText = script;
            return cmd.ExecuteNonQuery();
        }
    }
}
