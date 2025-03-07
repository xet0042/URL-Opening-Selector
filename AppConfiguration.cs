using System;
using System.IO;
using System.Data;
using Windows.Storage;
using System.Text.Json;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Threading;

namespace URL_Opening_Selector
{
    public class AppConfiguration
    {
        private StorageFile _file;
        public SQLiteConnection _db;
        public Configuration Configuration { get; set; }

        public AppConfiguration()
        {
            try
            {
                var folder = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path);
                var dbFile = new FileInfo(Path.Join(folder.FullName, "database.db"));
                // if (!dbFile.Exists)
                //     dbFile.Create();
                _db = new SQLiteConnection(new SQLiteConnectionStringBuilder
                {
                    DataSource = dbFile.FullName,
                    FailIfMissing = false,
                    ForeignKeys = true,
                    JournalMode = SQLiteJournalModeEnum.Wal,
                    Version = 3
                }.ToString());
                _db.Open();
                var command = _db.CreateCommand();
                const string createTableQuery = @"CREATE TABLE IF NOT EXISTS UrlPatterns
                (
                    Pattern TEXT PRIMARY KEY                  NOT NULL,
                    Browser TEXT                              NOT NULL,
                    Methods INTEGER                           NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS UrlPatterns_Pattern_index ON UrlPatterns (Pattern);";
                command.CommandText = createTableQuery;
                command.ExecuteNonQuery();
                command.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task InitJson()
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                _file = await folder.CreateFileAsync("config.json", CreationCollisionOption.OpenIfExists);

                var json = await FileIO.ReadTextAsync(_file);
                if (string.IsNullOrEmpty(json))
                {
                    Configuration = new Configuration();
                    await SaveJson();
                    return;
                }


                Configuration = JsonSerializer.Deserialize<Configuration>(json) ?? new Configuration();
            }
            catch (JsonException ex)
            {
                Debug.WriteLine(ex);
                Configuration = new Configuration();
                await SaveJson();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Configuration = new Configuration();
            }
        }

        public async Task SaveJson()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(Configuration, options);
                await FileIO.WriteTextAsync(_file, json, UnicodeEncoding.Utf8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task<List<UrlPattern>> GetUrlPatterns(string pattern, bool withLike = true)
        {
            try
            {
                await EnsureConnectionOpen();
                var list = new List<UrlPattern>();
                await using var command = _db.CreateCommand();
                command.CommandText =
                    $"SELECT Pattern, Browser, Methods FROM UrlPatterns WHERE @pattern {(withLike ? "LIKE" : "GLOB")} Pattern";
                command.Parameters.AddWithValue("@pattern", pattern);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var p = reader.GetString(0);
                    var browser = reader.GetString(1);
                    var methods = reader.GetInt32(2);
                    var urlPattern = new UrlPattern
                    {
                        Pattern = p,
                        Browser = browser,
                        Method = (UrlPatternMethod)methods
                    };
                    list.Add(urlPattern);
                }

                return list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return [];
        }

        public async Task GetAllUrlPatterns(ObservableCollection<PatternSettingItem> items)
        {
            try
            {
                await EnsureConnectionOpen();
                await using var command = _db.CreateCommand();
                command.CommandText = "SELECT Pattern, Browser, Methods FROM UrlPatterns";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var p = reader.GetString(0);
                    var browser = reader.GetString(1);
                    var methods = reader.GetInt32(2);
                    items.Add(new PatternSettingItem
                    {
                        Pattern = p,
                        Browser = browser,
                        Method = (UrlPatternMethod)methods,
                        Icon = "\uE71B"
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task AddUrlPattern(string pattern, string browser, UrlPatternMethod methods)
        {
            try
            {
                await EnsureConnectionOpen();
                const string commandString = @"INSERT INTO UrlPatterns (Pattern, Browser, Methods) 
                VALUES (@pattern, @browser, @methods)
                ON CONFLICT(Pattern) DO UPDATE SET 
                    Browser = @browser, 
                    Methods = @methods;";
                await using var command = _db.CreateCommand();
                command.CommandText = commandString;
                command.Parameters.AddWithValue("@pattern", pattern);
                command.Parameters.AddWithValue("@browser", browser);
                command.Parameters.AddWithValue("@methods", (int)methods);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task RemoveUrlPattern(string pattern)
        {
            await using var command = _db.CreateCommand();
            command.CommandText = "DELETE FROM UrlPatterns WHERE Pattern = @pattern";
            command.Parameters.AddWithValue("@pattern", pattern);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateUrlPattern(string pattern, string browser, UrlPatternMethod methods)
        {
            await using var command = new SQLiteCommand(
                "UPDATE UrlPatterns SET Browser = @browser, Methods = @methods WHERE Pattern = @pattern", _db);
            command.Parameters.AddWithValue("@pattern", pattern);
            command.Parameters.AddWithValue("@browser", browser);
            command.Parameters.AddWithValue("@methods", (int)methods);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> HasUrlPattern(string pattern)
        {
            await using var command =
                new SQLiteCommand("SELECT COUNT(*) FROM UrlPatterns WHERE Pattern = @pattern", _db);
            command.Parameters.AddWithValue("@pattern", pattern);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var count = reader.GetInt32(0);
            return count > 0;
        }

        private async Task EnsureConnectionOpen()
        {
            if (_db?.State != ConnectionState.Open && _db is not null)
            {
                await _db.OpenAsync();
            }
        }

        public async Task Close()
        {
            if (_db?.State != ConnectionState.Closed && _db is not null)
                await _db.CloseAsync();
            _db?.Dispose();
            _db = null;
        }
    }
}