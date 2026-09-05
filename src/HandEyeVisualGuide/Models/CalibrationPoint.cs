namespace HandEyeVisualGuide.Models;

/// <summary>
/// 标定点：一组像素坐标与对应机械坐标的配对
/// </summary>
public class CalibrationPoint
{
    /// <summary>点序号</summary>
    public int Index { get; set; }

    /// <summary>像素坐标X</summary>
    public double PixelX { get; set; }

    /// <summary>像素坐标Y</summary>
    public double PixelY { get; set; }

    /// <summary>机械坐标X（mm）</summary>
    public double RobotX { get; set; }

    /// <summary>机械坐标Y（mm）</summary>
    public double RobotY { get; set; }

    public CalibrationPoint() { }

    public CalibrationPoint(int index, double px, double py, double rx, double ry)
    {
        Index = index;
        PixelX = px;
        PixelY = py;
        RobotX = rx;
        RobotY = ry;
    }
}
