namespace HandEyeVisualGuide.Services;

/// <summary>
/// 日志服务：分级日志记录，支持文件持久化和内存查询
/// 日志级别：Info(普通信息) > Warn(警告) > Error(错误) > Fatal(致命)
/// </summary>
public class LogService
{
    #region 单例
    private static readonly Lazy<LogService> _instance = new(() => new LogService());
    public static LogService Instance => _instance.Value;
    #endregion

    #region 字段与属性
    private readonly List<LogEntry> _logs = new();
    private readonly object _lock = new();
    private string _logDirectory = "";
    private string _currentLogFile = "";

    /// <summary>日志文件目录</summary>
    public string LogDirectory => _logDirectory;

    /// <summary>当前日志文件路径</summary>
    public string CurrentLogFile => _currentLogFile;

    /// <summary>内存中保留的最大日志条数（超过后清理旧的）</summary>
    public int MaxMemoryLogs { get; set; } = 10000;
    #endregion

    #region 初始化
    private LogService()
    {
        // 默认日志目录：程序运行目录下的Logs文件夹
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
        _currentLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
    }

    /// <summary>
    /// 设置日志目录
    /// </summary>
    public void SetLogDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        _logDirectory = directory;
        _currentLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
    }
    #endregion

    #region 日志写入
    /// <summary>
    /// 写Info级别日志
    /// </summary>
    public void Info(string module, string message)
    {
        WriteLog(LogLevel.Info, module, message);
    }

    /// <summary>
    /// 写Warn级别日志
    /// </summary>
    public void Warn(string module, string message)
    {
        WriteLog(LogLevel.Warn, module, message);
    }

    /// <summary>
    /// 写Error级别日志
    /// </summary>
    public void Error(string module, string message, Exception? ex = null)
    {
        string fullMsg = ex != null ? $"{message} | 异常: {ex.Message}" : message;
        WriteLog(LogLevel.Error, module, fullMsg);
    }

    /// <summary>
    /// 写Fatal级别日志
    /// </summary>
    public void Fatal(string module, string message, Exception? ex = null)
    {
        string fullMsg = ex != null ? $"{message} | 异常: {ex.Message}" : message;
        WriteLog(LogLevel.Fatal, module, fullMsg);
    }

    /// <summary>
    /// 核心写入方法
    /// </summary>
    private void WriteLog(LogLevel level, string module, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Module = module,
            Message = message
        };

        lock (_lock)
        {
            _logs.Add(entry);

            // 内存日志超过上限时，清理最旧的一半
            if (_logs.Count > MaxMemoryLogs)
            {
                _logs.RemoveRange(0, _logs.Count / 2);
            }
        }

        // 写入文件
        try
        {
            // 跨天则新建日志文件
            string todayFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
            if (todayFile != _currentLogFile)
            {
                _currentLogFile = todayFile;
            }

            string line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] [{module}] {message}";
            File.AppendAllText(_currentLogFile, line + Environment.NewLine);
        }
        catch
        {
            // 日志写入失败不影响主流程
        }
    }
    #endregion

    #region 日志查询
    /// <summary>
    /// 获取所有内存日志
    /// </summary>
    public List<LogEntry> GetAllLogs()
    {
        lock (_lock)
        {
            return new List<LogEntry>(_logs);
        }
    }

    /// <summary>
    /// 按级别筛选日志
    /// </summary>
    public List<LogEntry> GetLogsByLevel(LogLevel level)
    {
        lock (_lock)
        {
            return _logs.Where(l => l.Level == level).ToList();
        }
    }

    /// <summary>
    /// 获取最近N条日志
    /// </summary>
    public List<LogEntry> GetRecentLogs(int count)
    {
        lock (_lock)
        {
            return _logs.Skip(Math.Max(0, _logs.Count - count)).ToList();
        }
    }

    /// <summary>
    /// 清空内存日志（不删除文件）
    /// </summary>
    public void ClearMemoryLogs()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }
    #endregion
}

/// <summary>
/// 日志级别枚举
/// </summary>
public enum LogLevel
{
    Info,
    Warn,
    Error,
    Fatal
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Module { get; set; } = "";
    public string Message { get; set; } = "";

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss.fff}] [{Level,-5}] [{Module}] {Message}";
    }
}
