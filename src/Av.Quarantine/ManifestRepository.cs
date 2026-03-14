using Microsoft.Data.Sqlite;

namespace Av.Quarantine;

internal sealed class ManifestRepository
{
    private readonly string _connectionString;

    public ManifestRepository(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? ".");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        Initialize();
    }

    private void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS quarantine_manifest (
                id TEXT PRIMARY KEY,
                original_path TEXT NOT NULL,
                vault_path TEXT NOT NULL,
                original_size INTEGER NOT NULL,
                original_created_utc TEXT NOT NULL,
                original_modified_utc TEXT NOT NULL,
                quarantined_at_utc TEXT NOT NULL,
                detection_name TEXT NOT NULL,
                original_sha256 TEXT NOT NULL,
                encrypted_sha256 TEXT NOT NULL,
                encryption_iv_base64 TEXT NOT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                restored_at_utc TEXT NULL,
                deleted_at_utc TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_quarantine_manifest_quarantined
            ON quarantine_manifest(quarantined_at_utc);
        """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Upsert(QuarantineRecord record)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO quarantine_manifest (
                id, original_path, vault_path, original_size, original_created_utc,
                original_modified_utc, quarantined_at_utc, detection_name, original_sha256,
                encrypted_sha256, encryption_iv_base64, is_deleted, restored_at_utc, deleted_at_utc)
            VALUES (
                $id, $original_path, $vault_path, $original_size, $original_created_utc,
                $original_modified_utc, $quarantined_at_utc, $detection_name, $original_sha256,
                $encrypted_sha256, $encryption_iv_base64, $is_deleted, $restored_at_utc, $deleted_at_utc)
            ON CONFLICT(id) DO UPDATE SET
                original_path = excluded.original_path,
                vault_path = excluded.vault_path,
                original_size = excluded.original_size,
                original_created_utc = excluded.original_created_utc,
                original_modified_utc = excluded.original_modified_utc,
                quarantined_at_utc = excluded.quarantined_at_utc,
                detection_name = excluded.detection_name,
                original_sha256 = excluded.original_sha256,
                encrypted_sha256 = excluded.encrypted_sha256,
                encryption_iv_base64 = excluded.encryption_iv_base64,
                is_deleted = excluded.is_deleted,
                restored_at_utc = excluded.restored_at_utc,
                deleted_at_utc = excluded.deleted_at_utc;
        """;

        command.Parameters.AddWithValue("$id", record.Id);
        command.Parameters.AddWithValue("$original_path", record.OriginalPath);
        command.Parameters.AddWithValue("$vault_path", record.VaultPath);
        command.Parameters.AddWithValue("$original_size", record.OriginalSize);
        command.Parameters.AddWithValue("$original_created_utc", record.OriginalCreatedUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$original_modified_utc", record.OriginalModifiedUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$quarantined_at_utc", record.QuarantinedAtUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$detection_name", record.DetectionName);
        command.Parameters.AddWithValue("$original_sha256", record.OriginalSha256);
        command.Parameters.AddWithValue("$encrypted_sha256", record.EncryptedSha256);
        command.Parameters.AddWithValue("$encryption_iv_base64", record.EncryptionIvBase64);
        command.Parameters.AddWithValue("$is_deleted", record.IsDeleted ? 1 : 0);
        command.Parameters.AddWithValue("$restored_at_utc", record.RestoredAtUtc?.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$deleted_at_utc", record.DeletedAtUtc?.UtcDateTime.ToString("O"));
        command.ExecuteNonQuery();
    }

    public QuarantineRecord? Get(string id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM quarantine_manifest WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public IReadOnlyList<QuarantineRecord> GetExpired(DateTimeOffset thresholdUtc)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM quarantine_manifest
            WHERE is_deleted = 0
              AND quarantined_at_utc <= $thresholdUtc;
        """;
        command.Parameters.AddWithValue("$thresholdUtc", thresholdUtc.UtcDateTime.ToString("O"));

        using var reader = command.ExecuteReader();
        var results = new List<QuarantineRecord>();
        while (reader.Read())
        {
            results.Add(Read(reader));
        }

        return results;
    }

    private static QuarantineRecord Read(SqliteDataReader reader)
    {
        return new QuarantineRecord(
            Id: reader.GetString(reader.GetOrdinal("id")),
            OriginalPath: reader.GetString(reader.GetOrdinal("original_path")),
            VaultPath: reader.GetString(reader.GetOrdinal("vault_path")),
            OriginalSize: reader.GetInt64(reader.GetOrdinal("original_size")),
            OriginalCreatedUtc: DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("original_created_utc"))),
            OriginalModifiedUtc: DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("original_modified_utc"))),
            QuarantinedAtUtc: DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("quarantined_at_utc"))),
            DetectionName: reader.GetString(reader.GetOrdinal("detection_name")),
            OriginalSha256: reader.GetString(reader.GetOrdinal("original_sha256")),
            EncryptedSha256: reader.GetString(reader.GetOrdinal("encrypted_sha256")),
            EncryptionIvBase64: reader.GetString(reader.GetOrdinal("encryption_iv_base64")),
            IsDeleted: reader.GetInt32(reader.GetOrdinal("is_deleted")) == 1,
            RestoredAtUtc: reader.IsDBNull(reader.GetOrdinal("restored_at_utc"))
                ? null
                : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("restored_at_utc"))),
            DeletedAtUtc: reader.IsDBNull(reader.GetOrdinal("deleted_at_utc"))
                ? null
                : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("deleted_at_utc"))));
    }
}
