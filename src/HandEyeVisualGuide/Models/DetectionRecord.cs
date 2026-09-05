namespace HandEyeVisualGuide.Models;

/// <summary>
/// 检测记录：每次视觉检测+抓取的完整数据，存入数据库
/// </summary>
public class DetectionRecord
{
    /// <summary>记录ID（自增主键）</summary>
    public long Id { get; set; }

    /// <summary>检测时间</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>工件类型/名称</summary>
    public string WorkpieceType { get; set; } = "矩形金属件";

    /// <summary>批次号</summary>
    public string BatchNo { get; set; } = "";

    /// <summary>检测算法</summary>
    public string Method { get; set; } = "";

    // ===== 视觉检测结果（像素坐标）=====
    /// <summary>检测中心X（像素）</summary>
    public double PixelX { get; set; }
    /// <summary>检测中心Y（像素）</summary>
    public double PixelY { get; set; }
    /// <summary>检测角度（度）</summary>
    public double Angle { get; set; }
    /// <summary>检测置信度（0~1）</summary>
    public double Confidence { get; set; }

    // ===== 坐标转换结果（机械坐标）=====
    /// <summary>目标机械X（mm）</summary>
    public double RobotX { get; set; }
    /// <summary>目标机械Y（mm）</summary>
    public double RobotY { get; set; }

    // ===== 精度评估（与真值对比）=====
    /// <summary>位置误差（mm）</summary>
    public double PositionError { get; set; }
    /// <summary>角度误差（度）</summary>
    public double AngleError { get; set; }

    // ===== 结果与耗时 =====
    /// <summary>检测结果（true=OK, false=NG）</summary>
    public bool ResultOK { get; set; }
    /// <summary>检测耗时（ms）</summary>
    public double ElapsedMs { get; set; }
    /// <summary>是否成功抓取</summary>
    public bool Grabbed { get; set; }

    /// <summary>备注</summary>
    public string Remark { get; set; } = "";

    /// <summary>
    /// 格式化显示
    /// </summary>
    public override string ToString()
    {
        return $"#{Id} {Timestamp:yyyy-MM-dd HH:mm:ss} " +
               $"({RobotX:F2},{RobotY:F2}) {Angle:F1}° " +
               $"{(ResultOK ? "OK" : "NG")} 误差:{PositionError:F4}mm";
    }
}
