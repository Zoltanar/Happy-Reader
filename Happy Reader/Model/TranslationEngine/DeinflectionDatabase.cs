using Happy_Apps_Core.DataAccess;
using Happy_Apps_Core.Database;
using Happy_Apps_Core;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using Happy_Reader.TranslationEngine;
using static Happy_Reader.JMDict;

namespace Happy_Reader.Model.TranslationEngine
{
    internal class DeinflectionDatabase
    {

        private const string ReasonMapTime = @"LatestDumpUpdate";
        private const string DateFormat = @"yyyy-MM-dd";

        public SQLiteConnection Connection { get; }
        public DACollection<string, TableDetail> TableDetails { get; }

        public DeinflectionDatabase(string dbFile)
        {
            Connection = new SQLiteConnection($@"Data Source={dbFile}");
            TableDetails = new DACollection<string, TableDetail>(Connection);
            if (!File.Exists(dbFile)) Seed();
            RunUpdates(); //table details loaded here
        }

        private void RunUpdates()
        {
            try
            {
                Connection.Open();
                TableDetails.Load(false);
                var updateDetail = TableDetails["updates"];
                var latestUpdate = updateDetail == null ? 0 : Convert.ToInt32(updateDetail.Value);
                RunUpdates(latestUpdate);
            }
            catch (Exception ex)
            {
                StaticHelpers.Logger.ToFile(ex);
                throw;
            }
            finally
            {
                Connection.Close();
            }
        }

        private void RunUpdates(int currentUpdate)
        {
            bool backedUp = false;
            var assembly = Assembly.GetExecutingAssembly();
            do
            {
                currentUpdate++;
                var update = $"Update{currentUpdate:#}";
                var nextUpdateFile = assembly.GetManifestResourceStream($"Happy_Apps_Core.Database.Updates.{update}.sql");
                if (nextUpdateFile == null) return;
                if (!backedUp)
                {
                    StaticHelpers.Logger.ToFile("Backing up Happy Apps Database to run updates.");
                    var dbFile = new FileInfo(Connection.FileName);
                    var backupFile = $"{dbFile.DirectoryName}\\{Path.GetFileNameWithoutExtension(dbFile.FullName)}-UB{DateTime.Now:yyyyMMdd-HHmmss}{dbFile.Extension}";
                    dbFile.CopyTo(backupFile);
                    backedUp = true;
                }
                StaticHelpers.Logger.ToFile($"Updating Happy Apps Database with {update}");
                using var reader = new StreamReader(nextUpdateFile);
                var contents = reader.ReadToEnd();
                DatabaseTableBuilder.ExecuteSql(Connection, contents);
            } while (true);
        }
        
        public int ExecuteSqlCommand(string query, bool openNewConnection)
        {
            if (openNewConnection) Connection.Open();
            try
            {
                using var command = Connection.CreateCommand();
                command.CommandText = query;
                var result = command.ExecuteNonQuery();
                return result;
            }
            finally
            {
                if (openNewConnection) Connection.Close();
            }
        }
        
        private void Seed()
        {
            Connection.Open();
            try
            {
                DatabaseTableBuilder.CreateTableDetails(Connection);
                CreateReasonsTable();
                TableDetails.Upsert(new TableDetail { Key = "programname", Value = "Happy Reader" }, false);
                TableDetails.Upsert(new TableDetail { Key = "author", Value = "Zoltanar" }, false);
                TableDetails.Upsert(new TableDetail { Key = "projecturl", Value = StaticHelpers.ProjectURL }, false);
                TableDetails.Upsert(new TableDetail { Key = "databaseversion", Value = StaticHelpers.ClientVersion }, false);
                TableDetails.Upsert(new TableDetail { Key = "updates", Value = "0" }, false);
            }
            finally
            {
                Connection.Close();
            }
        }

        private void CreateReasonsTable()
        {
            DatabaseTableBuilder.ExecuteSql(Connection, $@"CREATE TABLE `{nameof(DeinflectedTerm)}s` (
	`Expression`	TEXT NOT NULL,
	`Text`	TEXT NOT NULL,
	`Reasons`	TEXT NOT NULL,
    PRIMARY KEY(`Expression`,`Text`)
)");
        }
        
        public DateTime? GetReasonMapTime()
        {
            var datePair = TableDetails[ReasonMapTime];
            if (datePair is null || string.IsNullOrWhiteSpace(datePair.Value)) return null;
            return DateTime.ParseExact(datePair.Value, DateFormat, CultureInfo.InvariantCulture);
        }

        public void SaveReasonMapTime(DateTime updateDate)
        {
            var tableDetail = new TableDetail
            {
                Key = ReasonMapTime,
                Value = updateDate.ToString(DateFormat, CultureInfo.InvariantCulture)
            };
            TableDetails.Upsert(tableDetail, true);
        }

        public List<DeinflectedTerm> GetDeinflections(Term term)
        {
            var list = new List<DeinflectedTerm>();
            try
            {
                Connection.Open();
                var command = Connection.CreateCommand();
                command.CommandText = $"SELECT Expression, Text, Reasons from {nameof(DeinflectedTerm)}s where Expression = @Expression order by Text;";
                command.AddParameter("@Expression", term.Expression);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var expression = Convert.ToString(reader["Expression"]);
                    var text = Convert.ToString(reader["Text"]);
                    var reasons = Convert.ToString(reader["Reasons"]);
                    var deinflectedTerm = new DeinflectedTerm(expression, text,reasons);
                    list.Add(deinflectedTerm);
                }
            }
            finally{Connection.Close();}
            return list;
        }

        public void SaveDeinflection(DeinflectedTerm term, SQLiteTransaction transaction)
        {
            //we ignore if we already have an entry with matching key (expression,text) because shorter reasons come first,
            //we ignore reasons that shorten expressions to same kana.
            string sql = $"INSERT OR IGNORE INTO {nameof(DeinflectedTerm)}s" +
                         "(Expression,Text,Reasons) VALUES " +
                         "(@Expression,@Text,@Reasons)";
            var command = Connection.CreateCommand();
            command.CommandText = sql;
            command.AddParameter("@Expression", term.Expression);
            command.AddParameter("@Text", term.Text);
            command.AddParameter("@Reasons", term.ReasonsList);
            command.Transaction = transaction;
            command.ExecuteNonQuery();
            
        }

        public bool IsPopulated()
        {
            try
            {
                Connection.Open();
                var command = Connection.CreateCommand();
                command.CommandText = $"SELECT 1 from {nameof(DeinflectedTerm)}s LIMIT 1;";
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
            finally { Connection.Close(); }
        }
    }
}
