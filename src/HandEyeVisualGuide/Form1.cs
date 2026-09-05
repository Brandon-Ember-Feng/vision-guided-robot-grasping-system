using OpenCvSharp;
using HandEyeVisualGuide.Services;
using HandEyeVisualGuide.Models;
using Point = OpenCvSharp.Point;

namespace HandEyeVisualGuide;

public partial class Form1 : Form
{
    private readonly CameraSimulator _camera = new();
    private readonly HandEyeCalibrator _calibrator = new();
    private readonly VisionDetector _detector = new();
    private readonly PlcSimulator _plc = new();
    private readonly RobotSimulator _robot = new();
    private Mat? _currentFrame; // 当前帧灰度图缓存，用于检测
    private List<Workpiece> _currentTruth = new(); // 当前帧真值
    private bool _autoGrabRunning = false; // 自动抓取是否正在运行
    private int _frameCount = 0;
    private DateTime _lastFpsTime = DateTime.Now;
    private double _fps = 0;

    public Form1()
    {
        InitializeComponent();
        UpdateCameraInfo();
        _plc.Start(); // 启动PLC模拟器
    }

    #region 按钮事件
    /// <summary>
    /// 打开/关闭相机
    /// </summary>
    private void btnOpenCamera_Click(object? sender, EventArgs e)
    {
        if (!_camera.IsOpened)
        {
            _camera.OpenCamera();
            btnOpenCamera.Text = "关闭相机";
            btnGrab.Enabled = true;
            btnContinuous.Enabled = true;
            lblStatus.Text = "相机已打开";
            UpdateCameraInfo();
        }
        else
        {
            timer1.Stop();
            btnContinuous.Text = "连续采集";
            _camera.CloseCamera();
            btnOpenCamera.Text = "打开相机";
            btnGrab.Enabled = false;
            btnContinuous.Enabled = false;
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;
            lblStatus.Text = "相机已关闭";
            UpdateCameraInfo();
        }
    }

    /// <summary>
    /// 单帧采集
    /// </summary>
    private void btnGrab_Click(object? sender, EventArgs e)
    {
        if (!_camera.IsOpened) return;
        GrabAndDisplay();
    }

    /// <summary>
    /// 连续采集开关
    /// </summary>
    private void btnContinuous_Click(object? sender, EventArgs e)
    {
        if (!_camera.IsOpened) return;

        if (timer1.Enabled)
        {
            timer1.Stop();
            btnContinuous.Text = "连续采集";
            lblStatus.Text = "连续采集已停止";
        }
        else
        {
            timer1.Start();
            btnContinuous.Text = "停止采集";
            lblStatus.Text = "连续采集中...";
        }
    }

    /// <summary>
    /// 定时器：连续采集
    /// </summary>
    private void timer1_Tick(object? sender, EventArgs e)
    {
        GrabAndDisplay();
        UpdateFps();
    }

    /// <summary>
    /// 生成仿真九点标定数据
    /// </summary>
    private void btnGen9Points_Click(object? sender, EventArgs e)
    {
        if (!_camera.IsOpened)
        {
            MessageBox.Show("请先打开相机", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 生成标准3×3九点（模拟真实标定时机械手走到9个位置，相机拍到标定针）
        _calibrator.GenerateStandard9Points(_camera, rangeX: 25, rangeY: 18);

        UpdateCalibrationPointsDisplay();
        lblCalibResult.Text = $"已生成 {_calibrator.PointCount} 个标定点，点击\"执行标定\"";
        lblStatus.Text = $"已生成 {_calibrator.PointCount} 个仿真标定点";
    }

    /// <summary>
    /// 执行九点标定
    /// </summary>
    private void btnCalibrate_Click(object? sender, EventArgs e)
    {
        if (_calibrator.PointCount < 3)
        {
            MessageBox.Show("标定点不足，请先生成九点数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = _calibrator.Calibrate();

        if (result.Success)
        {
            lblCalibResult.Text =
                $"标定成功！平均误差:{result.MeanError:F4}mm 最大:{result.MaxError:F4}mm\r\n" +
                $"X = {result.A:F6}*x + {result.B:F6}*y + {result.C:F3}\r\n" +
                $"Y = {result.D:F6}*x + {result.E:F6}*y + {result.F:F3}";
            lblStatus.Text = $"手眼标定完成 | 平均误差 {result.MeanError:F4}mm | 最大误差 {result.MaxError:F4}mm";
            UpdateCalibrationPointsDisplay();
        }
        else
        {
            lblCalibResult.Text = $"标定失败: {result.Message}";
            lblStatus.Text = "标定失败";
        }
    }

    /// <summary>
    /// Blob检测按钮
    /// </summary>
    private void btnDetectBlob_Click(object? sender, EventArgs e)
    {
        if (!EnsureFrame()) return;
        var results = _detector.DetectByBlob(_currentFrame!);
        DisplayDetectionResults(results, Scalar.Blue, "Blob+图像矩");
    }

    /// <summary>
    /// 边缘检测按钮
    /// </summary>
    private void btnDetectEdge_Click(object? sender, EventArgs e)
    {
        if (!EnsureFrame()) return;
        var results = _detector.DetectByEdge(_currentFrame!);
        DisplayDetectionResults(results, Scalar.Cyan, "边缘+最小外接矩形");
    }

    /// <summary>
    /// 两种算法对比检测
    /// </summary>
    private void btnDetectCompare_Click(object? sender, EventArgs e)
    {
        if (!EnsureFrame()) return;

        var blobResults = _detector.DetectByBlob(_currentFrame!);
        var edgeResults = _detector.DetectByEdge(_currentFrame!);

        // 绘制：Blob蓝色，边缘青色，真值绿色
        using Mat colorImage = new();
        Cv2.CvtColor(_currentFrame!, colorImage, ColorConversionCodes.GRAY2BGR);

        // 先画真值（绿色）
        foreach (var wp in _currentTruth)
        {
            Point2d[] corners = wp.GetCornerPoints();
            Point[] pixelCorners = corners.Select(p => _camera.WorldToPixel(p.X, p.Y)).ToArray();
            Cv2.Polylines(colorImage, new[] { pixelCorners }, true, Scalar.Green, 2);
            Point center = _camera.WorldToPixel(wp.CenterX, wp.CenterY);
            Cv2.PutText(colorImage, "真值", center + new Point(-30, -25),
                HersheyFonts.HersheySimplex, 0.5, Scalar.Green, 1);
        }

        // Blob结果（蓝色）
        using Mat blobDrawn = _detector.DrawResults(colorImage, blobResults, Scalar.Blue);
        // 边缘结果（青色，线更粗）
        using Mat finalImage = _detector.DrawResults(blobDrawn, edgeResults, Scalar.Magenta);

        var oldImage = pictureBox1.Image;
        pictureBox1.Image = MatToBitmap(finalImage);
        oldImage?.Dispose();

        // 右侧显示对比结果
        DisplayCompareResults(blobResults, edgeResults);
    }

    /// <summary>
    /// 确保当前帧已采集
    /// </summary>
    private bool EnsureFrame()
    {
        if (_currentFrame == null || _currentFrame.Empty())
        {
            MessageBox.Show("请先点击\"单帧采集\"获取一帧图像", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 显示单种算法的检测结果
    /// </summary>
    private void DisplayDetectionResults(List<DetectionResult> results, Scalar color, string methodName)
    {
        // 绘制检测结果
        using Mat colorImage = new();
        Cv2.CvtColor(_currentFrame!, colorImage, ColorConversionCodes.GRAY2BGR);
        using Mat drawn = _detector.DrawResults(colorImage, results, color);

        var oldImage = pictureBox1.Image;
        pictureBox1.Image = MatToBitmap(drawn);
        oldImage?.Dispose();

        // 右侧显示检测数据和误差
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"【{methodName}】检测到 {results.Count} 个工件");
        sb.AppendLine("================================");

        double totalPosErr = 0, totalAngleErr = 0;
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.AppendLine($"工件{r.Index}:");
            sb.AppendLine($"  像素中心: ({r.CenterX,6:F1}, {r.CenterY,6:F1})");
            sb.AppendLine($"  检测角度: {r.Angle,7:F2}°  置信度: {r.Confidence:F2}");
            sb.AppendLine($"  耗时: {r.ElapsedMs:F2}ms");

            // 与真值对比（按序号匹配）
            if (i < _currentTruth.Count)
            {
                var (posErr, angleErr) = r.CompareWithTruth(_currentTruth[i], _camera);
                totalPosErr += posErr;
                totalAngleErr += angleErr;
                sb.AppendLine($"  位置误差: {posErr:F4}mm  角度误差: {angleErr:F2}°");
            }
            sb.AppendLine();
        }

        if (results.Count > 0 && _currentTruth.Count > 0)
        {
            int n = Math.Min(results.Count, _currentTruth.Count);
            sb.AppendLine($"--------------------------------");
            sb.AppendLine($"平均位置误差: {totalPosErr / n:F4}mm");
            sb.AppendLine($"平均角度误差: {totalAngleErr / n:F2}°");
        }

        txtResult.Text = sb.ToString();
        lblStatus.Text = $"{methodName}完成 | 检测到{results.Count}个工件";
    }

    /// <summary>
    /// 显示两种算法对比结果
    /// </summary>
    private void DisplayCompareResults(List<DetectionResult> blob, List<DetectionResult> edge)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【算法对比】蓝=Blob  品红=边缘  绿=真值");
        sb.AppendLine("================================");

        int n = Math.Min(Math.Min(blob.Count, edge.Count), _currentTruth.Count);

        double blobPosSum = 0, blobAngleSum = 0;
        double edgePosSum = 0, edgeAngleSum = 0;

        for (int i = 0; i < n; i++)
        {
            var (bPos, bAng) = blob[i].CompareWithTruth(_currentTruth[i], _camera);
            var (ePos, eAng) = edge[i].CompareWithTruth(_currentTruth[i], _camera);
            blobPosSum += bPos; blobAngleSum += bAng;
            edgePosSum += ePos; edgeAngleSum += eAng;

            sb.AppendLine($"工件{i + 1}:");
            sb.AppendLine($"  Blob : 位置{bPos:F4}mm 角度{bAng:F2}° 耗时{blob[i].ElapsedMs:F1}ms");
            sb.AppendLine($"  边缘 : 位置{ePos:F4}mm 角度{eAng:F2}° 耗时{edge[i].ElapsedMs:F1}ms");
            sb.AppendLine();
        }

        if (n > 0)
        {
            sb.AppendLine("========================================");
            sb.AppendLine($"         位置误差    角度误差    耗时");
            sb.AppendLine($"Blob   : {blobPosSum / n,7:F4}mm  {blobAngleSum / n,6:F2}°  {blob[0].ElapsedMs,5:F1}ms");
            sb.AppendLine($"边缘   : {edgePosSum / n,7:F4}mm  {edgeAngleSum / n,6:F2}°  {edge[0].ElapsedMs,5:F1}ms");
            sb.AppendLine($"----------------------------------------");
            string posWinner = blobPosSum < edgePosSum ? "Blob" : "边缘";
            string angleWinner = blobAngleSum < edgeAngleSum ? "Blob" : "边缘";
            sb.AppendLine($"位置精度优: {posWinner}  角度精度优: {angleWinner}");
        }

        txtResult.Text = sb.ToString();
        lblStatus.Text = $"算法对比完成 | Blob:{blob.Count}个 边缘:{edge.Count}个";
    }

    /// <summary>
    /// 视觉引导抓取按钮：完整闭环流程
    /// </summary>
    private async void btnAutoGrab_Click(object? sender, EventArgs e)
    {
        if (_autoGrabRunning) return;

        // 前置检查
        if (!_calibrator.IsCalibrated)
        {
            MessageBox.Show("请先完成手眼标定（生成九点→执行标定）", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!EnsureFrame()) return;

        _autoGrabRunning = true;
        btnAutoGrab.Enabled = false;

        try
        {
            await RunAutoGrabCycle();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"抓取流程异常: {ex.Message}";
            _plc.Alarm = true;
        }
        finally
        {
            _autoGrabRunning = false;
            btnAutoGrab.Enabled = true;
        }
    }

    /// <summary>
    /// 查看历史检测记录
    /// </summary>
    private void btnViewRecords_Click(object? sender, EventArgs e)
    {
        var records = DatabaseService.Instance.GetAllRecords(50); // 最近50条

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"【历史检测记录】共 {DatabaseService.Instance.TotalCount} 条，显示最近 {records.Count} 条");
        sb.AppendLine("================================================================");
        sb.AppendLine($"{"ID",4} | {"时间",19} | {"机械X",7} {"机械Y",7} | {"角度",6} | {"结果",4} | {"位置误差",8} | {"抓取",4}");
        sb.AppendLine("-----+---------------------+---------------+--------+------+----------+------");

        foreach (var r in records)
        {
            sb.AppendLine($"{r.Id,4} | {r.Timestamp:yyyy-MM-dd HH:mm:ss} | {r.RobotX,7:F2} {r.RobotY,7:F2} | {r.Angle,6:F1} | {(r.ResultOK ? "OK" : "NG"),4} | {r.PositionError,8:F4} | {(r.Grabbed ? "✓" : "✗"),4}");
        }

        if (records.Count == 0)
        {
            sb.AppendLine("（暂无记录，请先执行视觉引导抓取）");
        }

        txtResult.Text = sb.ToString();
        lblStatus.Text = $"已加载 {records.Count} 条历史记录（数据库共 {DatabaseService.Instance.TotalCount} 条）";
    }

    /// <summary>
    /// 生成生产统计报表
    /// </summary>
    private void btnReport_Click(object? sender, EventArgs e)
    {
        var report = DatabaseService.Instance.GetProductionReport();
        var todayRecords = DatabaseService.Instance.GetTodayRecords();
        var todayReport = DatabaseService.Instance.GetProductionReport(DateTime.Today, DateTime.Today.AddDays(1));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════╗");
        sb.AppendLine("║           视 觉 检 测 生 产 报 表            ║");
        sb.AppendLine("╚══════════════════════════════════════════════╝");
        sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("【累计统计】");
        sb.AppendLine($"  检测总数:     {report.TotalCount,6} 件");
        sb.AppendLine($"  合格数:       {report.OKCount,6} 件");
        sb.AppendLine($"  不合格数:     {report.NGCount,6} 件");
        sb.AppendLine($"  合格率:       {report.PassRate,6:F1} %");
        sb.AppendLine($"  抓取成功数:   {report.GrabbedCount,6} 件");
        sb.AppendLine($"  抓取成功率:   {report.GrabSuccessRate,6:F1} %");
        sb.AppendLine();
        sb.AppendLine("【精度统计】");
        sb.AppendLine($"  平均位置误差: {report.AvgPositionError,8:F4} mm");
        sb.AppendLine($"  最大位置误差: {report.MaxPositionError,8:F4} mm");
        sb.AppendLine($"  平均角度误差: {report.AvgAngleError,8:F2} °");
        sb.AppendLine($"  平均检测耗时: {report.AvgElapsedMs,8:F2} ms");
        sb.AppendLine($"  平均置信度:   {report.AvgConfidence,8:F3}");
        sb.AppendLine();
        sb.AppendLine("【今日统计】");
        sb.AppendLine($"  今日检测数:   {todayReport.TotalCount,6} 件");
        sb.AppendLine($"  今日合格率:   {todayReport.PassRate,6:F1} %");
        sb.AppendLine($"  今日抓取率:   {todayReport.GrabSuccessRate,6:F1} %");
        sb.AppendLine();
        sb.AppendLine("==================================================");
        sb.AppendLine("数据库文件: " + DatabaseService.Instance.DbPath);
        sb.AppendLine("日志目录:   " + LogService.Instance.LogDirectory);

        txtResult.Text = sb.ToString();
        lblStatus.Text = $"生产报表已生成 | 累计 {report.TotalCount} 件 合格率 {report.PassRate:F1}%";

        // 同时记录日志
        LogService.Instance.Info("Report", $"生成生产报表: 累计{report.TotalCount}件 合格率{report.PassRate:F1}%");
    }

    /// <summary>
    /// 执行一次完整的视觉引导抓取循环
    /// </summary>
    private async Task RunAutoGrabCycle()
    {
        var log = new System.Text.StringBuilder();
        void AddLog(string msg)
        {
            log.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
            txtResult.Text = log.ToString();
            lblStatus.Text = msg;
            Application.DoEvents();
        }

        // ===== 第1步：系统初始化 =====
        AddLog("【1/7】系统初始化：启动PLC，机械手回原点...");
        _plc.Start();
        _plc.ResetSignals();
        _robot.Reset();
        UpdateRobotDisplay();
        await Task.Delay(300);

        // ===== 第2步：工件到位，PLC发触发信号 =====
        AddLog("【2/7】工件到位，PLC置位触发信号(TRIG=ON)...");
        _plc.TriggerRequest = true;
        await Task.Delay(100);

        // ===== 第3步：视觉采集与检测 =====
        AddLog("【3/7】视觉系统检测到触发，采集图像并执行Blob检测...");
        var results = _detector.DetectByBlob(_currentFrame!);
        if (results.Count == 0)
        {
            AddLog("检测失败：未找到工件！");
            _plc.Alarm = true;
            return;
        }
        var target = results[0];
        AddLog($"  检测到工件: 像素({target.CenterX:F1},{target.CenterY:F1}) 角度{target.Angle:F1}° 置信度{target.Confidence:F2}");

        // ===== 第4步：坐标转换（像素→机械） =====
        AddLog("【4/7】通过标定矩阵进行坐标转换（像素→机械）...");
        var (robotX, robotY) = _calibrator.PixelToRobot(target.CenterX, target.CenterY);
        AddLog($"  转换结果: 机械坐标({robotX:F2},{robotY:F2})mm 角度{target.Angle:F1}°");

        // 与真值对比（找到距离最近的真值，避免多工件时匹配错误）
        if (_currentTruth.Count > 0)
        {
            var (posErr, angErr, matchedIdx) = CompareWithNearestTruth(target);
            AddLog($"  定位误差: 位置{posErr:F4}mm 角度{angErr:F2}° (匹配真值#{matchedIdx + 1})");
        }

        // ===== 第5步：视觉写数据到PLC，置位检测完成 =====
        AddLog("【5/7】视觉系统写目标坐标到PLC寄存器，置位检测完成(READY=ON)...");
        _plc.TargetX = (float)robotX;
        _plc.TargetY = (float)robotY;
        _plc.TargetAngle = (float)target.Angle;
        _plc.DetectResultOK = true;
        _plc.DetectReady = true;
        await Task.Delay(100);

        // ===== 第6步：PLC控制机械手移动到目标位置并抓取 =====
        AddLog("【6/7】PLC读取目标坐标，控制机械手移动到抓取位...");
        _plc.TriggerRequest = false; // 复位触发信号

        // 订阅机械手位置更新，实时绘制
        EventHandler<(double x, double y, double angle)> posHandler = (s, pos) =>
        {
            _plc.CurrentX = (float)pos.x;
            _plc.CurrentY = (float)pos.y;
            _plc.CurrentAngle = (float)pos.angle;
            UpdateRobotDisplay(target);
        };
        _robot.PositionUpdated += posHandler;

        try
        {
            await _robot.MoveToAsync(robotX, robotY, target.Angle, speedRatio: 0.6);
        }
        finally
        {
            _robot.PositionUpdated -= posHandler;
        }

        AddLog($"  机械手到达目标位: ({_robot.CurrentX:F2},{_robot.CurrentY:F2}) {_robot.CurrentAngle:F1}°");
        AddLog("  执行抓取动作...");
        await _robot.GrabAsync();
        AddLog($"  抓取完成! IsGrabbed={_robot.IsGrabbed}");

        // ===== 第7步：抓取完成，PLC发完成信号，流程结束 =====
        AddLog("【7/7】抓取完成，PLC置位完成信号(DONE=ON)，视觉系统复位...");
        _plc.GrabDone = true;
        _plc.CurrentX = (float)_robot.CurrentX;
        _plc.CurrentY = (float)_robot.CurrentY;
        _plc.CurrentAngle = (float)_robot.CurrentAngle;

        UpdateRobotDisplay(target);
        await Task.Delay(200);

        // 复位信号（模拟下一个循环准备）
        _plc.DetectReady = false;
        _plc.GrabDone = false;

        // ===== 写入数据库（使用最近真值匹配计算误差）=====
        double recordPosErr = 0, recordAngErr = 0;
        if (_currentTruth.Count > 0)
        {
            (recordPosErr, recordAngErr, _) = CompareWithNearestTruth(target);
        }

        var record = new DetectionRecord
        {
            Timestamp = DateTime.Now,
            WorkpieceType = "矩形金属件",
            Method = target.Method,
            PixelX = target.CenterX,
            PixelY = target.CenterY,
            Angle = target.Angle,
            Confidence = target.Confidence,
            RobotX = robotX,
            RobotY = robotY,
            PositionError = recordPosErr,
            AngleError = recordAngErr,
            ResultOK = recordPosErr < 0.5, // 位置误差<0.5mm判为OK
            ElapsedMs = target.ElapsedMs,
            Grabbed = _robot.IsGrabbed,
            Remark = "自动抓取流程"
        };
        long recordId = DatabaseService.Instance.InsertRecord(record);
        LogService.Instance.Info("Vision", $"检测记录已入库 ID={recordId} 位置误差={recordPosErr:F4}mm");

        AddLog("========================================");
        AddLog("✅ 视觉引导抓取流程全部完成！");
        AddLog($"   记录ID: {recordId}  已存入数据库");
        AddLog($"   目标: ({robotX:F2},{robotY:F2})mm  {target.Angle:F1}°");
        AddLog($"   实际: ({_robot.CurrentX:F2},{_robot.CurrentY:F2})mm  {_robot.CurrentAngle:F1}°");
        AddLog($"   位置误差: {recordPosErr:F4}mm  角度误差: {recordAngErr:F2}°");
        AddLog($"   抓取状态: {(_robot.IsGrabbed ? "已抓取" : "未抓取")}");
        AddLog($"   数据库总记录数: {DatabaseService.Instance.TotalCount}");
    }

    /// <summary>
    /// 找到与检测结果距离最近的真值并计算误差（解决多工件时匹配错误的问题）
    /// </summary>
    /// <returns>(位置误差mm, 角度误差°, 匹配的真值索引)</returns>
    private (double posErr, double angleErr, int matchedIndex) CompareWithNearestTruth(DetectionResult target)
    {
        double bestPosErr = double.MaxValue;
        double bestAngleErr = 0;
        int bestIndex = 0;

        for (int i = 0; i < _currentTruth.Count; i++)
        {
            var (posErr, angleErr) = target.CompareWithTruth(_currentTruth[i], _camera);
            if (posErr < bestPosErr)
            {
                bestPosErr = posErr;
                bestAngleErr = angleErr;
                bestIndex = i;
            }
        }

        return (bestPosErr, bestAngleErr, bestIndex);
    }

    /// <summary>
    /// 更新机械手在图像上的显示（绘制机械手当前位置和目标位置）
    /// </summary>
    private void UpdateRobotDisplay(DetectionResult? target = null)
    {
        if (_currentFrame == null) return;

        using Mat colorImage = new();
        Cv2.CvtColor(_currentFrame, colorImage, ColorConversionCodes.GRAY2BGR);

        // 绘制目标位置（黄色虚线框）
        if (target != null)
        {
            Point targetCenter = new((int)target.CenterX, (int)target.CenterY);
            Cv2.Circle(colorImage, targetCenter, 20, Scalar.Yellow, 2);
            Cv2.PutText(colorImage, "TARGET", targetCenter + new Point(25, -5),
                HersheyFonts.HersheySimplex, 0.5, Scalar.Yellow, 1);
        }

        // 绘制机械手当前位置（红色箭头）
        var robotPixel = _camera.WorldToPixel(_robot.CurrentX, _robot.CurrentY);
        Point robotCenter = new(robotPixel.X, robotPixel.Y);

        // 机械手主体（圆圈）
        Cv2.Circle(colorImage, robotCenter, 15, Scalar.Red, 2);
        Cv2.Circle(colorImage, robotCenter, 5, Scalar.Red, -1);

        // 方向箭头
        double rad = _robot.CurrentAngle * Math.PI / 180.0;
        Point arrowEnd = new(
            (int)(robotCenter.X + 30 * Math.Cos(rad)),
            (int)(robotCenter.Y + 30 * Math.Sin(rad))
        );
        Cv2.ArrowedLine(colorImage, robotCenter, arrowEnd, Scalar.Red, 2);

        // 标注
        string status = _robot.State == RobotState.Moving ? "MOVING" :
                        _robot.State == RobotState.Grabbing ? "GRABBING" :
                        _robot.IsGrabbed ? "GRABBED" : "IDLE";
        Cv2.PutText(colorImage, $"ROBOT:{status}", robotCenter + new Point(20, 20),
            HersheyFonts.HersheySimplex, 0.5, Scalar.Red, 1);

        // 绘制从机械手到目标的连线（运动中）
        if (target != null && _robot.State == RobotState.Moving)
        {
            Point targetCenter = new((int)target.CenterX, (int)target.CenterY);
            Cv2.Line(colorImage, robotCenter, targetCenter, Scalar.LightBlue, 1, LineTypes.AntiAlias);
        }

        var oldImage = pictureBox1.Image;
        pictureBox1.Image = MatToBitmap(colorImage);
        oldImage?.Dispose();
    }
    #endregion

    #region 核心方法
    /// <summary>
    /// Mat转Bitmap（通过PNG编码流，安全可靠）
    /// </summary>
    private static Bitmap MatToBitmap(Mat mat)
    {
        Cv2.ImEncode(".png", mat, out byte[] buffer);
        using var ms = new MemoryStream(buffer);
        return new Bitmap(ms);
    }

    /// <summary>
    /// 采集一帧并显示
    /// </summary>
    private void GrabAndDisplay()
    {
        try
        {
            var (image, truth) = _camera.GrabImageWithTruth();

            // 缓存当前帧（用于后续检测）
            _currentFrame?.Dispose();
            _currentFrame = image.Clone();
            _currentTruth = truth;

            // 转换为Bitmap显示（灰度图转彩色以便标注）
            using Mat colorImage = new();
            Cv2.CvtColor(image, colorImage, ColorConversionCodes.GRAY2BGR);

            // 在图上标注真值（绿色框 + 中心十字）
            foreach (var wp in truth)
            {
                Point2d[] corners = wp.GetCornerPoints();
                Point[] pixelCorners = corners
                    .Select(p => _camera.WorldToPixel(p.X, p.Y))
                    .ToArray();

                // 画工件轮廓（绿色）
                Cv2.Polylines(colorImage, new[] { pixelCorners }, true, Scalar.Green, 2);

                // 画中心十字
                Point center = _camera.WorldToPixel(wp.CenterX, wp.CenterY);
                Cv2.Line(colorImage, center - new Point(10, 0), center + new Point(10, 0), Scalar.Red, 2);
                Cv2.Line(colorImage, center - new Point(0, 10), center + new Point(0, 10), Scalar.Red, 2);

                // 标注角度
                Cv2.PutText(colorImage, $"{wp.Angle:F1}°",
                    center + new Point(15, -10),
                    HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 2);
            }

            // 显示图像
            var oldImage = pictureBox1.Image;
            pictureBox1.Image = MatToBitmap(colorImage);
            oldImage?.Dispose();

            // 显示真值信息
            UpdateTruthInfo(truth);

            image.Dispose();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"采集错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 更新相机参数显示
    /// </summary>
    private void UpdateCameraInfo()
    {
        lblCameraInfo.Text =
            $"分辨率: {_camera.ImageWidth}x{_camera.ImageHeight}\r\n" +
            $"像素当量: {_camera.PixelSize:F3} mm/pix\r\n" +
            $"视野: {_camera.FovWidth:F1}x{_camera.FovHeight:F1} mm\r\n" +
            $"曝光: {_camera.ExposureTime}ms  增益: {_camera.Gain}\r\n" +
            $"状态: {(_camera.IsOpened ? "已打开" : "未打开")}";
    }

    /// <summary>
    /// 更新工件真值显示
    /// </summary>
    private void UpdateTruthInfo(List<Workpiece> truth)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"工件数量: {truth.Count}");
        sb.AppendLine("------------------------");
        for (int i = 0; i < truth.Count; i++)
        {
            var wp = truth[i];
            sb.AppendLine($"工件{i + 1}:");
            sb.AppendLine($"  中心: ({wp.CenterX,7:F2}, {wp.CenterY,7:F2}) mm");
            sb.AppendLine($"  角度: {wp.Angle,7:F2}°");
            sb.AppendLine($"  尺寸: {wp.Width:F1} x {wp.Height:F1} mm");
        }
        txtResult.Text = sb.ToString();
    }

    /// <summary>
    /// FPS计算
    /// </summary>
    private void UpdateFps()
    {
        _frameCount++;
        if ((DateTime.Now - _lastFpsTime).TotalSeconds >= 1.0)
        {
            _fps = _frameCount / (DateTime.Now - _lastFpsTime).TotalSeconds;
            _frameCount = 0;
            _lastFpsTime = DateTime.Now;
            lblFps.Text = $"FPS: {_fps:F1}";
        }
    }

    /// <summary>
    /// 更新标定点列表显示
    /// </summary>
    private void UpdateCalibrationPointsDisplay()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("No.| 像素X  像素Y | 机械X  机械Y | 误差mm");
        sb.AppendLine("---+--------------+--------------+-------");

        foreach (var pt in _calibrator.Points)
        {
            string errorStr = "  -  ";
            if (_calibrator.IsCalibrated)
            {
                var (calcX, calcY) = _calibrator.PixelToRobot(pt.PixelX, pt.PixelY);
                double err = Math.Sqrt(Math.Pow(calcX - pt.RobotX, 2) + Math.Pow(calcY - pt.RobotY, 2));
                errorStr = $"{err:F4}";
            }

            sb.AppendLine($"{pt.Index,2} | {pt.PixelX,6:F1} {pt.PixelY,6:F1} | {pt.RobotX,6:F2} {pt.RobotY,6:F2} | {errorStr}");
        }

        txtCalibPoints.Text = sb.ToString();
    }
    #endregion

    #region 窗体事件
    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        timer1.Stop();
        _currentFrame?.Dispose();
        _plc.Stop();
        _camera.Dispose();
    }
    #endregion
}
