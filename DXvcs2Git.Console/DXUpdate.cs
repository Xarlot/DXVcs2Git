using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVcs2Git.Console {
    public class DXUpdateDB : IDisposable {
        readonly SqlConnection connection;

        public DXUpdateDB(string connectionString) {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }

        public bool CommitExists(string hash) {
            using (var command = new SqlCommand($"select count(*) from Commits where Hash = @hash", connection)) {
                command.Parameters.AddWithValue("@hash", hash);
                using (var reader = command.ExecuteReader()) {
                    reader.Read();
                    return reader.GetInt32(0) != 0;
                }
            }
        }

        public void AddCommit(string hash) {
            using (var command = new SqlCommand($"if not exists (select * from Commits where Hash = @hash) insert into Commits values (@hash)", connection)) {
                command.Parameters.AddWithValue("@hash", hash);
                command.ExecuteNonQuery();
            }
        }

        public void Dispose() {
            connection.Dispose();
        }
    }
}
