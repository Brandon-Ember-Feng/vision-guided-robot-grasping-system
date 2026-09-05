namespace HandEyeVisualGuide.Models;

/// <summary>
/// 标定结果：仿射变换参数 + 误差统计
/// </summary>
public class CalibrationResult
{
    /// <summary>是否标定成功</summary>
    public bool Success { get; set; }

    /// <summary>仿射变换6参数：X = a*x + b*y + c</summary>
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }

    /// <summary>仿射变换6参数：Y = d*x + e*y + f</summary>
    public double D { get; set; }
    public double E { get; set; }
    public double F { get; set; }

    /// <summary>标定点数量</summary>
    public int PointCount { get; set; }

    /// <summary>平均误差（mm）</summary>
    public double MeanError { get; set; }

    /// <summary>最大误差（mm）</summary>
    public double MaxError { get; set; }

    /// <summary>最小误差（mm）</summary>
    public double MinError { get; set; }

    /// <summary>误差信息（失败时的原因）</summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// 获取3x3仿射变换矩阵
    /// </summary>
    public double[,] GetMatrix()
    {
        return new double[,]
        {
            { A, B, C },
            { D, E, F },
            { 0, 0, 1 }
        };
    }

    public override string ToString()
    {
        if (!Success) return $"标定失败: {Message}";
        return $"标定成功 | 点数:{PointCount} | 平均误差:{MeanError:F4}mm | 最大误差:{MaxError:F4}mm";
    }
}
