using HandEyeVisualGuide.Services;

namespace HandEyeVisualGuide.Models;

/// <summary>
/// 视觉检测结果：单个工件的定位信息
/// </summary>
public class DetectionResult
{
    /// <summary>工件序号</summary>
    public int Index { get; set; }

    /// <summary>检测到的中心X坐标（像素）</summary>
    public double CenterX { get; set; }

    /// <summary>检测到的中心Y坐标（像素）</summary>
    public double CenterY { get; set; }

    /// <summary>检测到的旋转角度（度）</summary>
    public double Angle { get; set; }

    /// <summary>检测置信度（0~1）</summary>
    public double Confidence { get; set; }

    /// <summary>轮廓面积（像素²，用于排序筛选）</summary>
    public double Area { get; set; }

    /// <summary>算法类型</summary>
    public string Method { get; set; } = "";

    /// <summary>检测耗时（毫秒）</summary>
    public double ElapsedMs { get; set; }

    /// <summary>工件宽度（像素，Blob算法可测）</summary>
    public double Width { get; set; }

    /// <summary>工件高度（像素，Blob算法可测）</summary>
    public double Height { get; set; }

    /// <summary>
    /// 与真值对比，计算定位误差
    /// </summary>
    public (double positionError, double angleError) CompareWithTruth(Workpiece truth, CameraSimulator camera)
    {
        var truthPixel = camera.WorldToPixel(truth.CenterX, truth.CenterY);
        double posErr = Math.Sqrt(
            Math.Pow(CenterX - truthPixel.X, 2) +
            Math.Pow(CenterY - truthPixel.Y, 2)
        ) * camera.PixelSize; // 转换为mm

        // 角度误差（处理180度对称性，矩形工件180度看起来一样）
        double angleErr = Math.Abs(Angle - truth.Angle) % 180;
        if (angleErr > 90) angleErr = 180 - angleErr;

        return (posErr, angleErr);
    }
}
