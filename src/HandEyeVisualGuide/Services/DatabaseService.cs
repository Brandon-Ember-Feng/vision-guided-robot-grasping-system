using Microsoft.Data.Sqlite;
using HandEyeVisualGuide.Models;

namespace HandEyeVisualGuide.Services;

/// <summary>
/// 数据库服务：SQLite存储检测记录，支持增删改查和统计报表
/// 数据库文件：程序运行目录下的 vision_system.db
/// </summary>
public class DatabaseService
{
    #region 单例
    private static readonly Lazy<DatabaseService> _instance = new(() => new DatabaseService());
    public static DatabaseService Instance => _instance.Value;
    #endregion

    #region 字段与属性
    private readonly string _dbPath;
    private readonly string _connectionString;

    /// <summary>数据库文件路径</summary>
    public string DbPath => _dbPath;

    /// <summary>总记录数</summary>
    public long TotalCount => GetRecordCount();
    #endregion

    #region 初始化
    private DatabaseService()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vision_system.db");
        _connectionString = $"Data Source={_dbPath}";
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库，创建表
    /// </summary>
    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // 检测记录表
        string createTableSql = @"
            CREATE TABLE IF NOT EXISTS DetectionRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                WorkpieceType TEXT DEFAULT '',
                BatchNo TEXT DEFAULT '',
                Method TEXT DEFAULT '',
                PixelX REAL DEFAULT 0,
                PixelY REAL DEFAULT 0,
                Angle REAL DEFAULT 0,
                Confidence REAL DEFAULT 0,
                RobotX REAL DEFAULT 0,
                RobotY REAL DEFAULT 0,
                PositionError REAL DEFAULT 0,
                AngleError REAL DEFAULT 0,
                ResultOK INTEGER DEFAULT 1,
                ElapsedMs REAL DEFAULT 0,
                Grabbed INTEGER DEFAULT 0,
                Remark TEXT DEFAULT ''
            );";

        using var cmd = new SqliteCommand(createTableSql, conn);
        cmd.ExecuteNonQuery();

        // 创建索引（按时间查询优化）
        string createIndexSql = "CREATE INDEX IF NOT EXISTS idx_timestamp ON DetectionRecords(Timestamp);";
        using var cmd2 = new SqliteCommand(createIndexSql, conn);
        cmd2.ExecuteNonQuery();

        LogService.Instance.Info("Database", "数据库初始化完成");
    }
    #endregion

    #region 增删改查
    /// <summary>
    /// 插入一条检测记录
    /// </summary>
    public long InsertRecord(DetectionRecord record)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        string sql = @"
            INSERT INTO DetectionRecords
            (Timestamp, WorkpieceType, BatchNo, Method, PixelX, PixelY, Angle, Confidence,
             RobotX, RobotY, PositionError, AngleError, ResultOK, ElapsedMs, Grabbed, Remark)
            VALUES
            (@Timestamp, @WorkpieceType, @BatchNo, @Method, @PixelX, @PixelY, @Angle, @Confidence,
             @RobotX, @RobotY, @PositionError, @AngleError, @ResultOK, @ElapsedMs, @Grabbed, @Remark);
            SELECT last_insert_rowid();";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Timestamp", record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        cmd.Parameters.AddWithValue("@WorkpieceType", record.WorkpieceType);
        cmd.Parameters.AddWithValue("@BatchNo", record.BatchNo);
        cmd.Parameters.AddWithValue("@Method", record.Method);
        cmd.Parameters.AddWithValue("@PixelX", record.PixelX);
        cmd.Parameters.AddWithValue("@PixelY", record.PixelY);
        cmd.Parameters.AddWithValue("@Angle", record.Angle);
        cmd.Parameters.AddWithValue("@Confidence", record.Confidence);
        cmd.Parameters.AddWithValue("@RobotX", record.RobotX);
        cmd.Parameters.AddWithValue("@RobotY", record.RobotY);
        cmd.Parameters.AddWithValue("@PositionError", record.PositionError);
        cmd.Parameters.AddWithValue("@AngleError", record.AngleError);
        cmd.Parameters.AddWithValue("@ResultOK", record.ResultOK ? 1 : 0);
        cmd.Parameters.AddWithValue("@ElapsedMs", record.ElapsedMs);
        cmd.Parameters.AddWithValue("@Grabbed", record.Grabbed ? 1 : 0);
        cmd.Parameters.AddWithValue("@Remark", record.Remark);

        long id = (long)cmd.ExecuteScalar();
        record.Id = id;
        return id;
    }

    /// <summary>
    /// 查询所有记录（按时间倒序）
    /// </summary>
    public List<DetectionRecord> GetAllRecords(int limit = 1000)
    {
        var records = new List<DetectionRecord>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        string sql = $"SELECT * FROM DetectionRecords ORDER BY Timestamp DESC LIMIT {limit}";
        using var cmd = new SqliteCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            records.Add(ReadRecord(reader));
        }
        return records;
    }

    /// <summary>
    /// 按日期范围查询记录
    /// </summary>
    public List<DetectionRecord> GetRecordsByDate(DateTime startDate, DateTime endDate)
    {
        var records = new List<DetectionRecord>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        string sql = "SELECT * FROM DetectionRecords WHERE Timestamp >= @Start AND Timestamp < @End ORDER BY Timestamp DESC";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Start", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@End", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            records.Add(ReadRecord(reader));
        }
        return records;
    }

    /// <summary>
    /// 获取今日记录
    /// </summary>
    public List<DetectionRecord> GetTodayRecords()
    {
        DateTime today = DateTime.Today;
        return GetRecordsByDate(today, today.AddDays(1));
    }

    /// <summary>
    /// 删除指定ID的记录
    /// </summary>
    public bool DeleteRecord(long id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string sql = "DELETE FROM DetectionRecords WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// 清空所有记录
    /// </summary>
    public void ClearAllRecords()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string sql = "DELETE FROM DetectionRecords; DELETE FROM sqlite_sequence WHERE name='DetectionRecords';";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.ExecuteNonQuery();
        LogService.Instance.Warn("Database", "已清空所有检测记录");
    }
    #endregion

    #region 统计报表
    /// <summary>
    /// 生产统计报表
    /// </summary>
    public ProductionReport GetProductionReport(DateTime? startDate = null, DateTime? endDate = null)
    {
        var records = startDate.HasValue && endDate.HasValue
            ? GetRecordsByDate(startDate.Value, endDate.Value)
            : GetAllRecords();

        var report = new ProductionReport
        {
            StartDate = startDate ?? DateTime.MinValue,
            EndDate = endDate ?? DateTime.MaxValue,
            TotalCount = records.Count,
            OKCount = records.Count(r => r.ResultOK),
            NGCount = records.Count(r => !r.ResultOK),
            GrabbedCount = records.Count(r => r.Grabbed),
            AvgPositionError = records.Count > 0 ? records.Average(r => r.PositionError) : 0,
            MaxPositionError = records.Count > 0 ? records.Max(r => r.PositionError) : 0,
            AvgAngleError = records.Count > 0 ? records.Average(r => r.AngleError) : 0,
            AvgElapsedMs = records.Count > 0 ? records.Average(r => r.ElapsedMs) : 0,
            AvgConfidence = records.Count > 0 ? records.Average(r => r.Confidence) : 0
        };
        report.PassRate = report.TotalCount > 0 ? (double)report.OKCount / report.TotalCount * 100 : 0;
        report.GrabSuccessRate = report.TotalCount > 0 ? (double)report.GrabbedCount / report.TotalCount * 100 : 0;

        return report;
    }

    /// <summary>
    /// 获取总记录数
    /// </summary>
    private long GetRecordCount()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        string sql = "SELECT COUNT(*) FROM DetectionRecords";
        using var cmd = new SqliteCommand(sql, conn);
        return (long)cmd.ExecuteScalar();
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 从DataReader读取一条记录
    /// </summary>
    private static DetectionRecord ReadRecord(SqliteDataReader reader)
    {
        return new DetectionRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp"))),
            WorkpieceType = reader.GetString(reader.GetOrdinal("WorkpieceType")),
            BatchNo = reader.GetString(reader.GetOrdinal("BatchNo")),
            Method = reader.GetString(reader.GetOrdinal("Method")),
            PixelX = reader.GetDouble(reader.GetOrdinal("PixelX")),
            PixelY = reader.GetDouble(reader.GetOrdinal("PixelY")),
            Angle = reader.GetDouble(reader.GetOrdinal("Angle")),
            Confidence = reader.GetDouble(reader.GetOrdinal("Confidence")),
            RobotX = reader.GetDouble(reader.GetOrdinal("RobotX")),
            RobotY = reader.GetDouble(reader.GetOrdinal("RobotY")),
            PositionError = reader.GetDouble(reader.GetOrdinal("PositionError")),
            AngleError = reader.GetDouble(reader.GetOrdinal("AngleError")),
            ResultOK = reader.GetInt32(reader.GetOrdinal("ResultOK")) == 1,
            ElapsedMs = reader.GetDouble(reader.GetOrdinal("ElapsedMs")),
            Grabbed = reader.GetInt32(reader.GetOrdinal("Grabbed")) == 1,
            Remark = reader.GetString(reader.GetOrdinal("Remark"))
        };
    }
    #endregion
}

/// <summary>
/// 生产统计报表
/// </summary>
public class ProductionReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalCount { get; set; }
    public int OKCount { get; set; }
    public int NGCount { get; set; }
    public int GrabbedCount { get; set; }
    public double PassRate { get; set; }        // 合格率 %
    public double GrabSuccessRate { get; set; } // 抓取成功率 %
    public double AvgPositionError { get; set; }
    public double MaxPositionError { get; set; }
    public double AvgAngleError { get; set; }
    public double AvgElapsedMs { get; set; }
    public double AvgConfidence { get; set; }

    public override string ToString()
    {
        return $"检测总数:{TotalCount} 合格:{OKCount} 合格率:{PassRate:F1}% " +
               $"抓取成功:{GrabbedCount} 抓取率:{GrabSuccessRate:F1}% " +
               $"平均位置误差:{AvgPositionError:F4}mm";
    }
}
