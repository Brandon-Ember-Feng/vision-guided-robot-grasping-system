using OpenCvSharp;
using HandEyeVisualGuide.Models;
using System.Diagnostics;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace HandEyeVisualGuide.Services;

/// <summary>
/// 视觉定位检测器：实现两套定位算法并对比精度
/// 算法1：Blob阈值分割 + 连通域 + 图像矩（基础版）
/// 算法2：Canny边缘 + 轮廓 + 最小外接矩形（进阶版，类似工业形状匹配）
/// </summary>
public class VisionDetector
{
    #region 算法参数
    /// <summary>二值化阈值（0~255），工件深色背景浅色，低于此值为工件</summary>
    public int BinaryThreshold { get; set; } = 120;

    /// <summary>形态学核大小（去噪）</summary>
    public int MorphKernelSize { get; set; } = 3;

    /// <summary>Canny边缘检测低阈值</summary>
    public int CannyLowThreshold { get; set; } = 50;

    /// <summary>Canny边缘检测高阈值</summary>
    public int CannyHighThreshold { get; set; } = 150;

    /// <summary>最小工件面积（像素²），过滤噪点</summary>
    public double MinArea { get; set; } = 500;
    #endregion

    #region 算法1：Blob分析 + 图像矩
    /// <summary>
    /// 算法1：Blob阈值分割 + 连通域 + 图像矩计算中心和角度
    /// 原理：
    ///   1. 阈值分割提取工件区域
    ///   2. 形态学开运算去除小噪点
    ///   3. 连通域分析提取每个工件
    ///   4. 图像矩计算中心坐标：cx = m10/m00, cy = m01/m00
    ///   5. 二阶中心矩计算主轴方向：theta = 0.5*atan2(2*mu11, mu20-mu02)
    /// </summary>
    public List<DetectionResult> DetectByBlob(Mat grayImage)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<DetectionResult>();

        // 1. 阈值分割：工件深色(60)，背景浅色(180)，用THRESH_BINARY_INV提取深色工件
        using Mat binary = new();
        Cv2.Threshold(grayImage, binary, BinaryThreshold, 255, ThresholdTypes.BinaryInv);

        // 2. 形态学开运算：先腐蚀后膨胀，去除小噪点
        using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect,
            new Size(MorphKernelSize, MorphKernelSize));
        using Mat cleaned = new();
        Cv2.MorphologyEx(binary, cleaned, MorphTypes.Open, kernel);

        // 3. 连通域分析
        Cv2.FindContours(cleaned, out Point[][] contours, out HierarchyIndex[] hierarchy,
            RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        int idx = 0;
        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area < MinArea) continue; // 过滤噪点

            // 4. 图像矩计算中心坐标
            Moments mu = Cv2.Moments(contour);
            double cx = mu.M10 / mu.M00;
            double cy = mu.M01 / mu.M00;

            // 5. 二阶中心矩计算主轴方向（角度）
            // theta = 0.5 * atan2(2*mu11, mu20 - mu02)
            double angleRad = 0.5 * Math.Atan2(2 * mu.Mu11, mu.Mu20 - mu.Mu02);
            double angleDeg = angleRad * 180.0 / Math.PI;

            // 计算工件等效尺寸
            double width = 2 * Math.Sqrt(mu.Mu20 / mu.M00);
            double height = 2 * Math.Sqrt(mu.Mu02 / mu.M00);

            results.Add(new DetectionResult
            {
                Index = ++idx,
                CenterX = cx,
                CenterY = cy,
                Angle = angleDeg,
                Confidence = 1.0, // Blob算法无置信度概念，设为1
                Method = "Blob+图像矩",
                Width = width,
                Height = height,
                Area = mu.M00, // 零阶矩=面积
                ElapsedMs = 0
            });
        }

        sw.Stop();
        foreach (var r in results) r.ElapsedMs = sw.Elapsed.TotalMilliseconds / Math.Max(results.Count, 1);

        // 按面积降序排列，确保最大的工件（目标）排在第一位
        return results.OrderByDescending(r => r.Area).ToList();
    }
    #endregion

    #region 算法2：Canny边缘 + 轮廓 + 最小外接矩形
    /// <summary>
    /// 算法2：Canny边缘检测 + 轮廓提取 + 最小外接矩形
    /// 原理（类似Halcon形状匹配的简化版）：
    ///   1. 高斯滤波去噪
    ///   2. Canny边缘检测提取工件边缘
    ///   3. 查找轮廓并筛选
    ///   4. 对每个轮廓计算最小外接旋转矩形（MinAreaRect）
    ///   5. 从RotatedRect直接获取中心、角度、尺寸
    /// 优势：对光照变化更鲁棒，亚像素级精度，支持任意角度
    /// </summary>
    public List<DetectionResult> DetectByEdge(Mat grayImage)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<DetectionResult>();

        // 1. 高斯滤波去噪（保护边缘）
        using Mat blurred = new();
        Cv2.GaussianBlur(grayImage, blurred, new Size(5, 5), 1.5);

        // 2. Canny边缘检测
        using Mat edges = new();
        Cv2.Canny(blurred, edges, CannyLowThreshold, CannyHighThreshold);

        // 3. 形态学闭运算：连接断裂的边缘
        using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        using Mat closed = new();
        Cv2.MorphologyEx(edges, closed, MorphTypes.Close, kernel);

        // 4. 查找轮廓
        Cv2.FindContours(closed, out Point[][] contours, out HierarchyIndex[] hierarchy,
            RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        int idx = 0;
        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area < MinArea) continue;

            // 5. 最小外接旋转矩形（核心！直接返回中心、尺寸、角度）
            RotatedRect rect = Cv2.MinAreaRect(contour);

            // OpenCV的角度范围是(-90, 0]，需要转换为(-180, 180]
            // 并且区分长边和短边对应的角度
            double angle = rect.Angle;
            if (rect.Size.Width < rect.Size.Height)
            {
                angle += 90;
            }
            // 归一化到(-180, 180]
            while (angle > 180) angle -= 360;
            while (angle <= -180) angle += 360;

            // 置信度：轮廓面积 / 最小外接矩形面积，越接近1说明形状越规则
            double rectArea = rect.Size.Width * rect.Size.Height;
            double confidence = rectArea > 0 ? Math.Min(area / rectArea, 1.0) : 0;

            results.Add(new DetectionResult
            {
                Index = ++idx,
                CenterX = rect.Center.X,
                CenterY = rect.Center.Y,
                Angle = angle,
                Confidence = confidence,
                Method = "边缘+最小外接矩形",
                Width = rect.Size.Width,
                Height = rect.Size.Height,
                Area = area, // 轮廓面积
                ElapsedMs = 0
            });
        }

        sw.Stop();
        foreach (var r in results) r.ElapsedMs = sw.Elapsed.TotalMilliseconds / Math.Max(results.Count, 1);

        // 按面积降序排列，确保最大的工件（目标）排在第一位
        return results.OrderByDescending(r => r.Area).ToList();
    }
    #endregion

    #region 可视化：在图像上绘制检测结果
    /// <summary>
    /// 在图像上绘制检测结果（中心十字、角度、外框）
    /// </summary>
    public Mat DrawResults(Mat colorImage, List<DetectionResult> results, Scalar color)
    {
        Mat output = colorImage.Clone();

        foreach (var r in results)
        {
            Point center = new Point((int)r.CenterX, (int)r.CenterY);

            // 画中心十字
            Cv2.Line(output, center - new Point(15, 0), center + new Point(15, 0), color, 2);
            Cv2.Line(output, center - new Point(0, 15), center + new Point(0, 15), color, 2);

            // 画旋转矩形框
            double rad = r.Angle * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double hw = r.Width / 2;
            double hh = r.Height / 2;

            Point2d[] localCorners =
            {
                new(-hw, -hh), new(hw, -hh), new(hw, hh), new(-hw, hh)
            };
            Point[] corners = localCorners.Select(p =>
                new Point(
                    (int)(r.CenterX + p.X * cos - p.Y * sin),
                    (int)(r.CenterY + p.X * sin + p.Y * cos)
                )).ToArray();
            Cv2.Polylines(output, new[] { corners }, true, color, 2);

            // 标注角度和置信度
            string label = $"{r.Method}: {r.Angle:F1}° ({r.Confidence:F2})";
            Cv2.PutText(output, label, center + new Point(20, -15),
                HersheyFonts.HersheySimplex, 0.5, color, 1);
        }

        return output;
    }
    #endregion
}
