namespace HandEyeVisualGuide.Services;

/// <summary>
/// 机械手模拟器：模拟四轴SCARA机械手的运动控制与抓取动作
/// 包含：当前位姿、移动到目标位姿、抓取/释放、运动状态
/// </summary>
public class RobotSimulator
{
    #region 字段与属性
    /// <summary>当前X坐标（mm，机械手基坐标系）</summary>
    public double CurrentX { get; private set; } = 0;

    /// <summary>当前Y坐标（mm）</summary>
    public double CurrentY { get; private set; } = 0;

    /// <summary>当前旋转角度（度）</summary>
    public double CurrentAngle { get; private set; } = 0;

    /// <summary>是否已抓取工件</summary>
    public bool IsGrabbed { get; private set; } = false;

    /// <summary>运动状态</summary>
    public RobotState State { get; private set; } = RobotState.Idle;

    /// <summary>最大运动速度（mm/s）</summary>
    public double MaxSpeed { get; set; } = 500;

    /// <summary>最大角速度（度/s）</summary>
    public double MaxAngularSpeed { get; set; } = 360;

    /// <summary>抓取动作耗时（ms）</summary>
    public int GrabTimeMs { get; set; } = 200;
    #endregion

    #region 事件
    /// <summary>位置更新事件（运动过程中持续触发）</summary>
    public event EventHandler<(double x, double y, double angle)>? PositionUpdated;

    /// <summary>运动完成事件</summary>
    public event EventHandler? MoveCompleted;

    /// <summary>抓取完成事件</summary>
    public event EventHandler? GrabCompleted;
    #endregion

    #region 运动控制
    /// <summary>
    /// 移动到目标位姿（模拟匀速运动，带位置更新回调）
    /// </summary>
    /// <param name="targetX">目标X（mm）</param>
    /// <param name="targetY">目标Y（mm）</param>
    /// <param name="targetAngle">目标角度（度）</param>
    /// <param name="speedRatio">速度比例（0~1，默认1.0）</param>
    public async Task MoveToAsync(double targetX, double targetY, double targetAngle, double speedRatio = 1.0)
    {
        if (State == RobotState.Moving)
            throw new InvalidOperationException("机械手正在运动中，无法执行新的移动指令");

        State = RobotState.Moving;

        double startX = CurrentX, startY = CurrentY, startAngle = CurrentAngle;
        double distance = Math.Sqrt(Math.Pow(targetX - startX, 2) + Math.Pow(targetY - startY, 2));
        double angleDiff = NormalizeAngle(targetAngle - startAngle);

        // 计算运动时间（取直线运动和旋转运动的较长者）
        double moveTime = distance / (MaxSpeed * speedRatio);
        double rotateTime = Math.Abs(angleDiff) / (MaxAngularSpeed * speedRatio);
        double totalTime = Math.Max(moveTime, rotateTime);
        totalTime = Math.Max(totalTime, 0.1); // 最少0.1秒

        // 模拟运动：按时间插值更新位置
        int steps = (int)(totalTime * 50); // 50Hz更新频率
        steps = Math.Max(steps, 5);
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            // 加减速曲线（S曲线，平滑启停）
            double smooth = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;

            CurrentX = startX + (targetX - startX) * smooth;
            CurrentY = startY + (targetY - startY) * smooth;
            CurrentAngle = NormalizeAngle(startAngle + angleDiff * smooth);

            PositionUpdated?.Invoke(this, (CurrentX, CurrentY, CurrentAngle));
            await Task.Delay((int)(totalTime * 1000 / steps));
        }

        // 确保最终位置精确
        CurrentX = targetX;
        CurrentY = targetY;
        CurrentAngle = NormalizeAngle(targetAngle);
        PositionUpdated?.Invoke(this, (CurrentX, CurrentY, CurrentAngle));

        State = RobotState.Idle;
        MoveCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 移动到安全位置（原点上方，等待位）
    /// </summary>
    public async Task MoveToHomeAsync()
    {
        await MoveToAsync(0, 0, 0);
    }
    #endregion

    #region 抓取控制
    /// <summary>
    /// 执行抓取动作（下降→吸合→上升）
    /// </summary>
    public async Task GrabAsync()
    {
        if (IsGrabbed) return;
        State = RobotState.Grabbing;

        // 模拟抓取耗时
        await Task.Delay(GrabTimeMs);
        IsGrabbed = true;

        State = RobotState.Idle;
        GrabCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 执行释放动作
    /// </summary>
    public async Task ReleaseAsync()
    {
        if (!IsGrabbed) return;
        State = RobotState.Grabbing;

        await Task.Delay(GrabTimeMs / 2);
        IsGrabbed = false;

        State = RobotState.Idle;
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 角度归一化到(-180, 180]
    /// </summary>
    private static double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle <= -180) angle += 360;
        return angle;
    }

    /// <summary>
    /// 计算到目标位置的距离
    /// </summary>
    public double DistanceTo(double targetX, double targetY)
    {
        return Math.Sqrt(Math.Pow(targetX - CurrentX, 2) + Math.Pow(targetY - CurrentY, 2));
    }

    /// <summary>
    /// 复位机械手到原点
    /// </summary>
    public void Reset()
    {
        CurrentX = 0;
        CurrentY = 0;
        CurrentAngle = 0;
        IsGrabbed = false;
        State = RobotState.Idle;
        PositionUpdated?.Invoke(this, (0, 0, 0));
    }
    #endregion
}

/// <summary>
/// 机械手状态枚举
/// </summary>
public enum RobotState
{
    /// <summary>空闲</summary>
    Idle,
    /// <summary>运动中</summary>
    Moving,
    /// <summary>抓取/释放中</summary>
    Grabbing,
    /// <summary>报警</summary>
    Alarm
}
