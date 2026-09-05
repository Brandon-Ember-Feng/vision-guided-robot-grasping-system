using MathNet.Numerics.LinearAlgebra;
using HandEyeVisualGuide.Models;

namespace HandEyeVisualGuide.Services;

/// <summary>
/// 手眼标定器：九点标定法，建立像素坐标系→机械手基坐标系的仿射变换
/// 仿射变换公式：
///   RobotX = a * PixelX + b * PixelY + c
///   RobotY = d * PixelX + e * PixelY + f
/// 通过9组对应点，用最小二乘法超定求解6个参数
/// </summary>
public class HandEyeCalibrator
{
    #region 字段与属性
    private readonly List<CalibrationPoint> _points = new();

    /// <summary>已添加的标定点</summary>
    public IReadOnlyList<CalibrationPoint> Points => _points.AsReadOnly();

    /// <summary>标定点数量</summary>
    public int PointCount => _points.Count;

    /// <summary>是否已完成标定</summary>
    public bool IsCalibrated { get; private set; } = false;

    /// <summary>标定结果</summary>
    public CalibrationResult? Result { get; private set; }
    #endregion

    #region 标定点管理
    /// <summary>
    /// 添加一个标定点
    /// </summary>
    public void AddPoint(double pixelX, double pixelY, double robotX, double robotY)
    {
        _points.Add(new CalibrationPoint(_points.Count + 1, pixelX, pixelY, robotX, robotY));
        IsCalibrated = false; // 点变化后需要重新标定
    }

    /// <summary>
    /// 清除所有标定点
    /// </summary>
    public void ClearPoints()
    {
        _points.Clear();
        IsCalibrated = false;
        Result = null;
    }
    #endregion

    #region 核心标定算法
    /// <summary>
    /// 执行九点标定（最小二乘法求解仿射变换参数）
    /// 至少需要3个点，推荐9个点
    /// </summary>
    public CalibrationResult Calibrate()
    {
        Result = new CalibrationResult { PointCount = _points.Count };

        if (_points.Count < 3)
        {
            Result.Success = false;
            Result.Message = $"标定点不足，至少需要3个点，当前只有{_points.Count}个";
            return Result;
        }

        try
        {
            int n = _points.Count;

            // 构建A矩阵（n行3列）：每行 [pixelX, pixelY, 1]
            double[,] matrixA = new double[n, 3];
            double[] arrayBx = new double[n]; // 机械X坐标
            double[] arrayBy = new double[n]; // 机械Y坐标

            for (int i = 0; i < n; i++)
            {
                matrixA[i, 0] = _points[i].PixelX;
                matrixA[i, 1] = _points[i].PixelY;
                matrixA[i, 2] = 1.0;
                arrayBx[i] = _points[i].RobotX;
                arrayBy[i] = _points[i].RobotY;
            }

            var A = Matrix<double>.Build.DenseOfArray(matrixA);
            var bX = Vector<double>.Build.DenseOfArray(arrayBx);
            var bY = Vector<double>.Build.DenseOfArray(arrayBy);

            // 最小二乘求解：A * p = b（超定方程组，QR分解求解）
            // 解X方向：p = [a, b, c]
            var pX = A.Solve(bX);
            // 解Y方向：p = [d, e, f]
            var pY = A.Solve(bY);

            Result.A = pX[0];
            Result.B = pX[1];
            Result.C = pX[2];
            Result.D = pY[0];
            Result.E = pY[1];
            Result.F = pY[2];

            // 计算标定误差（回代验证）
            CalculateErrors();

            Result.Success = true;
            Result.Message = "标定成功";
            IsCalibrated = true;
        }
        catch (Exception ex)
        {
            Result.Success = false;
            Result.Message = $"标定计算异常: {ex.Message}";
            IsCalibrated = false;
        }

        return Result;
    }

    /// <summary>
    /// 计算标定误差（将每个点的像素坐标代入标定公式，与真实机械坐标比较）
    /// 注意：直接使用Result参数计算，不调用PixelToRobot，避免IsCalibrated状态依赖
    /// </summary>
    private void CalculateErrors()
    {
        if (Result == null) return;

        double sumError = 0;
        double maxError = 0;
        double minError = double.MaxValue;

        foreach (var pt in _points)
        {
            // 直接用仿射参数计算，不经过PixelToRobot（此时IsCalibrated可能还没设）
            double calcX = Result.A * pt.PixelX + Result.B * pt.PixelY + Result.C;
            double calcY = Result.D * pt.PixelX + Result.E * pt.PixelY + Result.F;
            double error = Math.Sqrt(
                Math.Pow(calcX - pt.RobotX, 2) +
                Math.Pow(calcY - pt.RobotY, 2)
            );

            sumError += error;
            if (error > maxError) maxError = error;
            if (error < minError) minError = error;
        }

        Result.MeanError = sumError / _points.Count;
        Result.MaxError = maxError;
        Result.MinError = minError;
    }
    #endregion

    #region 坐标转换
    /// <summary>
    /// 像素坐标 → 机械坐标（必须先标定成功）
    /// </summary>
    public (double robotX, double robotY) PixelToRobot(double pixelX, double pixelY)
    {
        if (!IsCalibrated || Result == null)
            throw new InvalidOperationException("尚未完成标定，请先调用Calibrate()");

        double robotX = Result.A * pixelX + Result.B * pixelY + Result.C;
        double robotY = Result.D * pixelX + Result.E * pixelY + Result.F;
        return (robotX, robotY);
    }

    /// <summary>
    /// 机械坐标 → 像素坐标（逆变换，用于验证）
    /// </summary>
    public (double pixelX, double pixelY) RobotToPixel(double robotX, double robotY)
    {
        if (!IsCalibrated || Result == null)
            throw new InvalidOperationException("尚未完成标定，请先调用Calibrate()");

        // 仿射逆变换：解线性方程组
        // a*px + b*py = robotX - c
        // d*px + e*py = robotY - f
        double det = Result.A * Result.E - Result.B * Result.D;
        if (Math.Abs(det) < 1e-10)
            throw new InvalidOperationException("标定矩阵奇异，无法求逆");

        double rhsX = robotX - Result.C;
        double rhsY = robotY - Result.F;

        double pixelX = (Result.E * rhsX - Result.B * rhsY) / det;
        double pixelY = (-Result.D * rhsX + Result.A * rhsY) / det;

        return (pixelX, pixelY);
    }
    #endregion

    #region 仿真辅助：生成标准九点
    /// <summary>
    /// 生成标准的3×3九点标定数据（仿真用，模拟真实标定时机械手走到9个位置）
    /// </summary>
    /// <param name="camera">相机模拟器，用于将机械坐标转为像素坐标</param>
    /// <param name="rangeX">X方向标定范围（mm，默认±25）</param>
    /// <param name="rangeY">Y方向标定范围（mm，默认±18）</param>
    public void GenerateStandard9Points(CameraSimulator camera, double rangeX = 25, double rangeY = 18)
    {
        ClearPoints();

        // 3×3均匀分布的9个机械坐标点
        double[] xs = { -rangeX, 0, rangeX };
        double[] ys = { -rangeY, 0, rangeY };

        foreach (var ry in ys)
        {
            foreach (var rx in xs)
            {
                // 机械坐标 → 像素坐标（模拟相机看到标定针的位置）
                var pixel = camera.WorldToPixel(rx, ry);
                // 添加微小噪声（模拟真实标定的像素提取误差，±0.5像素）
                double noiseX = (new Random().NextDouble() - 0.5) * 1.0;
                double noiseY = (new Random().NextDouble() - 0.5) * 1.0;
                AddPoint(pixel.X + noiseX, pixel.Y + noiseY, rx, ry);
            }
        }
    }
    #endregion
}
