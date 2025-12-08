using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StoreManagementAPI.Services
{
    public class DatabaseInitializerService
    {
        private readonly string _connectionString;

        public DatabaseInitializerService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                // Kiểm tra database đã tồn tại chưa
                var dbExists = await CheckDatabaseExistsAsync();

                if (!dbExists)
                {
                    Console.WriteLine("🔄 Database chưa tồn tại, đang khởi tạo...");
                    await RunInitialSetupAsync();
                    Console.WriteLine("✅ Database đã được khởi tạo thành công!");
                    return true;
                }
                else
                {
                    Console.WriteLine("✅ Database đã tồn tại, bỏ qua khởi tạo.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi khởi tạo database: {ex.Message}");
                throw;
            }
        }

        private Task<bool> CheckDatabaseExistsAsync()
        {
            try
            {
                // For SQLite, check if the database file exists
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                var dbPath = builder.DataSource;
                return Task.FromResult(File.Exists(dbPath));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private async Task RunInitialSetupAsync()
        {
            // Đọc file SQL
            var sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "InitialSetup.sql");

            if (!File.Exists(sqlFilePath))
            {
                // If no setup file, just create empty database
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                return;
            }

            var sqlScript = await File.ReadAllTextAsync(sqlFilePath);

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Tách script thành các statements riêng lẻ
                var statements = SplitSqlStatements(sqlScript);

                foreach (var statement in statements)
                {
                    if (string.IsNullOrWhiteSpace(statement))
                        continue;

                    try
                    {
                        using (var command = new SqliteCommand(statement, connection))
                        {
                            command.CommandTimeout = 300; // 5 phút timeout
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng tiếp tục
                        if (!statement.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"⚠️ Warning executing statement: {ex.Message}");
                        }
                    }
                }
            }
        }

        private string[] SplitSqlStatements(string sqlScript)
        {
            // Tách script theo dấu chấm phẩy, nhưng bỏ qua comment
            var lines = sqlScript.Split('\n');
            var statements = new System.Collections.Generic.List<string>();
            var currentStatement = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Bỏ qua comment
                if (trimmedLine.StartsWith("--") || trimmedLine.StartsWith("#"))
                    continue;

                currentStatement.AppendLine(line);

                // Nếu line kết thúc bằng ; thì đó là end of statement
                if (trimmedLine.EndsWith(";"))
                {
                    statements.Add(currentStatement.ToString());
                    currentStatement.Clear();
                }
            }

            // Thêm statement cuối cùng nếu có
            if (currentStatement.Length > 0)
            {
                statements.Add(currentStatement.ToString());
            }

            return statements.ToArray();
        }
    }
}
