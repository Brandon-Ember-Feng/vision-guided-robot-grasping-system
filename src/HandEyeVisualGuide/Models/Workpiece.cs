using OpenCvSharp;

namespace HandEyeVisualGuide.Models;

/// <summary>
/// 工件数据模型：描述一个待抓取工件的位姿与尺寸
/// </summary>
public class Workpiece
{
    /// <summary>工件中心X坐标（机械手基坐标系，单位mm）</summary>
    public double CenterX { get; set; }

    /// <summary>工件中心Y坐标（机械手基坐标系，单位mm）</summary>
    public double CenterY { get; set; }

    /// <summary>工件旋转角度（度，逆时针为正）</summary>
    public double Angle { get; set; }

    /// <summary>工件宽度（mm，X方向）</summary>
    public double Width { get; set; }

    /// <summary>工件高度（mm，Y方向）</summary>
    public double Height { get; set; }

    /// <summary>工件类型名称</summary>
    public string Type { get; set; } = "矩形金属件";

    /// <summary>
    /// 获取工件四个角点在世界坐标系下的坐标（旋转后）
    /// </summary>
    public Point2d[] GetCornerPoints()
    {
        double rad = Angle * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double hw = Width / 2.0;
        double hh = Height / 2.0;

        // 四个角点（相对于中心，未旋转）
        Point2d[] locals =
        {
            new(-hw, -hh),  // 左上
            new( hw, -hh),  // 右上
            new( hw,  hh),  // 右下
            new(-hw,  hh)   // 左下
        };

        // 旋转 + 平移到世界坐标
        Point2d[] result = new Point2d[4];
        for (int i = 0; i < 4; i++)
        {
            result[i] = new Point2d(
                CenterX + locals[i].X * cos - locals[i].Y * sin,
                CenterY + locals[i].X * sin + locals[i].Y * cos
            );
        }
        return result;
    }
}
