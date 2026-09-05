using OpenCvSharp;
using HandEyeVisualGuide.Models;

namespace HandEyeVisualGuide.Services;

/// <summary>
/// 模拟工业相机：生成带随机工件的模拟图像
/// 接口设计与真实相机SDK一致，后续换真实相机只需替换本类
/// </summary>
public class CameraSimulator : IDisposable
{
    #region 相机参数
    /// <summary>图像宽度（像素）</summary>
    public int ImageWidth { get; private set; } = 1280;

    /// <summary>图像高度（像素）</summary>
    public int ImageHeight { get; private set; } = 960;

    /// <summary>像素当量（mm/pixel），即1像素对应多少毫米</summary>
    public double PixelSize { get; private set; } = 0.05;

    /// <summary>视野宽度（mm）</summary>
    public double FovWidth => ImageWidth * PixelSize;

    /// <summary>视野高度（mm）</summary>
    public double FovHeight => ImageHeight * PixelSize;

    /// <summary>相机是否已打开</summary>
    public bool IsOpened { get; private set; } = false;

    /// <summary>曝光时间（模拟，ms）</summary>
    public int ExposureTime { get; set; } = 20;

    /// <summary>增益（模拟，0-100）</summary>
    public int Gain { get; set; } = 10;
    #endregion

    #region 私有字段
    private readonly Random _random = new();
    private Mat? _background;
    private bool _disposed = false;
    #endregion

    #region 相机控制接口（与真实SDK对齐）
    /// <summary>
    /// 打开相机，初始化参数
    /// </summary>
    public void OpenCamera()
    {
        if (IsOpened) return;

        // 生成标准背景图（模拟工业工作台，带轻微纹理）
        _background = new Mat(ImageHeight, ImageWidth, MatType.CV_8UC1, new Scalar(180));
        // 添加背景纹理（细微噪声模拟工作台表面）
        Cv2.Randu(_background, 170, 195);

        IsOpened = true;
    }

    /// <summary>
    /// 采集一帧图像（模拟软触发）
    /// </summary>
    /// <returns>灰度图像Mat</returns>
    public Mat GrabImage()
    {
        if (!IsOpened || _background == null)
            throw new InvalidOperationException("相机未打开，请先调用OpenCamera()");

        // 复制背景
        Mat frame = _background.Clone();

        // 生成1-3个随机工件并绘制
        int workpieceCount = _random.Next(1, 4);
        for (int i = 0; i < workpieceCount; i++)
        {
            Workpiece wp = GenerateRandomWorkpiece();
            DrawWorkpiece(frame, wp);
        }

        // 模拟工业现场：添加噪声 + 亮度波动
        AddNoise(frame);
        SimulateIllumination(frame);

        return frame;
    }

    /// <summary>
    /// 采集一帧图像，并返回图像中所有工件的真值（用于算法验证）
    /// </summary>
    public (Mat image, List<Workpiece> groundTruth) GrabImageWithTruth()
    {
        if (!IsOpened || _background == null)
            throw new InvalidOperationException("相机未打开，请先调用OpenCamera()");

        Mat frame = _background.Clone();
        List<Workpiece> truth = new();

        int workpieceCount = _random.Next(1, 4);
        for (int i = 0; i < workpieceCount; i++)
        {
            Workpiece wp = GenerateRandomWorkpiece();
            DrawWorkpiece(frame, wp);
            truth.Add(wp);
        }

        AddNoise(frame);
        SimulateIllumination(frame);

        return (frame, truth);
    }

    /// <summary>
    /// 关闭相机
    /// </summary>
    public void CloseCamera()
    {
        _background?.Dispose();
        _background = null;
        IsOpened = false;
    }
    #endregion

    #region 坐标系转换
    /// <summary>
    /// 世界坐标(mm) → 像素坐标(pixel)
    /// 世界坐标系原点在视野中心，X向右，Y向下
    /// </summary>
    public OpenCvSharp.Point WorldToPixel(double worldX, double worldY)
    {
        int px = (int)((worldX + FovWidth / 2.0) / PixelSize);
        int py = (int)((worldY + FovHeight / 2.0) / PixelSize);
        return new OpenCvSharp.Point(px, py);
    }

    /// <summary>
    /// 像素坐标(pixel) → 世界坐标(mm)
    /// </summary>
    public Point2d PixelToWorld(int pixelX, int pixelY)
    {
        double wx = pixelX * PixelSize - FovWidth / 2.0;
        double wy = pixelY * PixelSize - FovHeight / 2.0;
        return new Point2d(wx, wy);
    }
    #endregion

    #region 内部方法
    /// <summary>
    /// 在视野范围内生成一个随机工件
    /// </summary>
    private Workpiece GenerateRandomWorkpiece()
    {
        // 工件尺寸：30~60mm
        double width = 30 + _random.NextDouble() * 30;
        double height = 20 + _random.NextDouble() * 25;

        // 位置：视野范围内，留边距避免超出
        double margin = Math.Max(width, height) / 2 + 5;
        double x = (_random.NextDouble() - 0.5) * (FovWidth - 2 * margin);
        double y = (_random.NextDouble() - 0.5) * (FovHeight - 2 * margin);

        // 角度：-180~180度
        double angle = (_random.NextDouble() - 0.5) * 360;

        return new Workpiece
        {
            CenterX = x,
            CenterY = y,
            Angle = angle,
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// 在图像上绘制工件（模拟金属件：深色主体 + 亮边）
    /// </summary>
    private void DrawWorkpiece(Mat frame, Workpiece wp)
    {
        Point2d[] corners = wp.GetCornerPoints();
        OpenCvSharp.Point[] pixelCorners = corners.Select(p => WorldToPixel(p.X, p.Y)).ToArray();

        // 填充工件主体（深灰色金属）
        Cv2.FillConvexPoly(frame, pixelCorners, new Scalar(60 + _random.Next(-10, 10)));

        // 绘制边缘（亮边模拟金属反光）
        Cv2.Polylines(frame, new[] { pixelCorners }, true, new Scalar(220), 2);

        // 中心标记点（模拟工件定位孔）
        OpenCvSharp.Point center = WorldToPixel(wp.CenterX, wp.CenterY);
        Cv2.Circle(frame, center, 4, new Scalar(240), -1);
    }

    /// <summary>
    /// 添加高斯噪声（模拟相机传感器噪声）
    /// </summary>
    private void AddNoise(Mat frame)
    {
        Mat noise = new Mat(frame.Size(), MatType.CV_8UC1);
        Cv2.Randu(noise, 0, 255);
        // 轻微噪声叠加
        Cv2.AddWeighted(frame, 0.95, noise, 0.05, 0, frame);
        noise.Dispose();
    }

    /// <summary>
    /// 模拟光照波动（全局亮度变化）
    /// </summary>
    private void SimulateIllumination(Mat frame)
    {
        // 全局亮度波动 ±15，模拟工业现场光源不稳定
        int brightnessShift = _random.Next(-15, 16);
        if (brightnessShift != 0)
        {
            frame += new Scalar(brightnessShift);
        }

        // 对比度轻微波动
        double contrast = 0.95 + _random.NextDouble() * 0.1; // 0.95~1.05
        if (Math.Abs(contrast - 1.0) > 0.001)
        {
            Cv2.ConvertScaleAbs(frame, frame, contrast, 0);
        }
    }
    #endregion

    #region Dispose
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            CloseCamera();
        }
        _disposed = true;
    }

    ~CameraSimulator()
    {
        Dispose(false);
    }
    #endregion
}
