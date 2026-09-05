namespace HandEyeVisualGuide.Services;

/// <summary>
/// PLC模拟器：模拟工业PLC的寄存器模型与握手逻辑
/// 接口设计与真实Modbus TCP一致，后续可直接替换为真实通信
/// 
/// 寄存器映射：
///   线圈(Coil) - 布尔量信号：
///     0: 触发请求_TRIG   (PLC→视觉，工件到位请求检测)
///     1: 抓取完成_DONE    (PLC→视觉，机械手抓取完成)
///     2: 检测完成_READY   (视觉→PLC，视觉检测完成)
///     3: 检测结果_OKNG    (视觉→PLC，OK=true / NG=false)
///     4: 系统报警_ALARM   (视觉→PLC，异常报警)
///   保持寄存器(HoldingRegister) - 浮点数据：
///     0: 目标X坐标_TARGET_X  (mm)
///     1: 目标Y坐标_TARGET_Y  (mm)
///     2: 目标角度_TARGET_A   (度)
///     3: 当前X坐标_CURRENT_X (mm，机械手当前位置)
///     4: 当前Y坐标_CURRENT_Y (mm)
///     5: 当前角度_CURRENT_A  (度)
/// </summary>
public class PlcSimulator
{
    #region 寄存器定义
    /// <summary>线圈寄存器数量</summary>
    public const int CoilCount = 16;

    /// <summary>保持寄存器数量</summary>
    public const int RegisterCount = 32;

    // 线圈地址常量
    public const int COIL_TRIG = 0;      // 触发请求
    public const int COIL_DONE = 1;      // 抓取完成
    public const int COIL_READY = 2;     // 检测完成
    public const int COIL_OKNG = 3;      // 检测结果
    public const int COIL_ALARM = 4;     // 系统报警

    // 保持寄存器地址常量
    public const int REG_TARGET_X = 0;    // 目标X
    public const int REG_TARGET_Y = 1;    // 目标Y
    public const int REG_TARGET_A = 2;    // 目标角度
    public const int REG_CURRENT_X = 3;   // 当前X
    public const int REG_CURRENT_Y = 4;   // 当前Y
    public const int REG_CURRENT_A = 5;   // 当前角度
    #endregion

    #region 字段与属性
    private readonly bool[] _coils = new bool[CoilCount];
    private readonly float[] _registers = new float[RegisterCount];

    /// <summary>PLC是否已启动</summary>
    public bool IsRunning { get; private set; } = false;

    /// <summary>PLC名称</summary>
    public string Name { get; set; } = "PLC-Simulator";
    #endregion

    #region 事件
    /// <summary>线圈状态变化事件</summary>
    public event EventHandler<(int address, bool value)>? CoilChanged;

    /// <summary>寄存器值变化事件</summary>
    public event EventHandler<(int address, float value)>? RegisterChanged;
    #endregion

    #region 控制
    /// <summary>
    /// 启动PLC
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;
        Array.Clear(_coils, 0, _coils.Length);
        Array.Clear(_registers, 0, _registers.Length);
        IsRunning = true;
    }

    /// <summary>
    /// 停止PLC
    /// </summary>
    public void Stop()
    {
        IsRunning = false;
        Array.Clear(_coils, 0, _coils.Length);
        Array.Clear(_registers, 0, _registers.Length);
    }

    /// <summary>
    /// 复位所有信号（保持当前位置数据）
    /// </summary>
    public void ResetSignals()
    {
        _coils[COIL_TRIG] = false;
        _coils[COIL_DONE] = false;
        _coils[COIL_READY] = false;
        _coils[COIL_OKNG] = false;
        _coils[COIL_ALARM] = false;
    }
    #endregion

    #region 线圈操作（Coil - 布尔量）
    /// <summary>
    /// 读线圈
    /// </summary>
    public bool ReadCoil(int address)
    {
        if (address < 0 || address >= CoilCount)
            throw new ArgumentOutOfRangeException(nameof(address), $"线圈地址超出范围(0~{CoilCount - 1})");
        return _coils[address];
    }

    /// <summary>
    /// 写线圈
    /// </summary>
    public void WriteCoil(int address, bool value)
    {
        if (address < 0 || address >= CoilCount)
            throw new ArgumentOutOfRangeException(nameof(address), $"线圈地址超出范围(0~{CoilCount - 1})");
        if (_coils[address] != value)
        {
            _coils[address] = value;
            CoilChanged?.Invoke(this, (address, value));
        }
    }

    /// <summary>
    /// 脉冲触发：置位后自动复位（模拟PLC上升沿触发）
    /// </summary>
    public void PulseCoil(int address, int durationMs = 50)
    {
        WriteCoil(address, true);
        Task.Delay(durationMs).ContinueWith(_ => WriteCoil(address, false));
    }
    #endregion

    #region 保持寄存器操作（HoldingRegister - 浮点量）
    /// <summary>
    /// 读保持寄存器
    /// </summary>
    public float ReadRegister(int address)
    {
        if (address < 0 || address >= RegisterCount)
            throw new ArgumentOutOfRangeException(nameof(address), $"寄存器地址超出范围(0~{RegisterCount - 1})");
        return _registers[address];
    }

    /// <summary>
    /// 写保持寄存器
    /// </summary>
    public void WriteRegister(int address, float value)
    {
        if (address < 0 || address >= RegisterCount)
            throw new ArgumentOutOfRangeException(nameof(address), $"寄存器地址超出范围(0~{RegisterCount - 1})");
        if (Math.Abs(_registers[address] - value) > 0.0001)
        {
            _registers[address] = value;
            RegisterChanged?.Invoke(this, (address, value));
        }
    }

    /// <summary>
    /// 批量写保持寄存器
    /// </summary>
    public void WriteRegisters(int startAddress, float[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            WriteRegister(startAddress + i, values[i]);
        }
    }
    #endregion

    #region 便捷属性（语义化访问）
    public bool TriggerRequest { get => ReadCoil(COIL_TRIG); set => WriteCoil(COIL_TRIG, value); }
    public bool GrabDone { get => ReadCoil(COIL_DONE); set => WriteCoil(COIL_DONE, value); }
    public bool DetectReady { get => ReadCoil(COIL_READY); set => WriteCoil(COIL_READY, value); }
    public bool DetectResultOK { get => ReadCoil(COIL_OKNG); set => WriteCoil(COIL_OKNG, value); }
    public bool Alarm { get => ReadCoil(COIL_ALARM); set => WriteCoil(COIL_ALARM, value); }

    public float TargetX { get => ReadRegister(REG_TARGET_X); set => WriteRegister(REG_TARGET_X, value); }
    public float TargetY { get => ReadRegister(REG_TARGET_Y); set => WriteRegister(REG_TARGET_Y, value); }
    public float TargetAngle { get => ReadRegister(REG_TARGET_A); set => WriteRegister(REG_TARGET_A, value); }
    public float CurrentX { get => ReadRegister(REG_CURRENT_X); set => WriteRegister(REG_CURRENT_X, value); }
    public float CurrentY { get => ReadRegister(REG_CURRENT_Y); set => WriteRegister(REG_CURRENT_Y, value); }
    public float CurrentAngle { get => ReadRegister(REG_CURRENT_A); set => WriteRegister(REG_CURRENT_A, value); }
    #endregion

    #region 状态快照
    /// <summary>
    /// 获取PLC状态快照（用于界面显示）
    /// </summary>
    public string GetStatusSnapshot()
    {
        return $"触发:{(TriggerRequest ? "ON" : "OFF")}  " +
               $"完成:{(GrabDone ? "ON" : "OFF")}  " +
               $"就绪:{(DetectReady ? "ON" : "OFF")}  " +
               $"结果:{(DetectResultOK ? "OK" : "NG")}  " +
               $"报警:{(Alarm ? "ON" : "OFF")}\r\n" +
               $"目标: ({TargetX,7:F2}, {TargetY,7:F2}, {TargetAngle,6:F1}°)\r\n" +
               $"当前: ({CurrentX,7:F2}, {CurrentY,7:F2}, {CurrentAngle,6:F1}°)";
    }
    #endregion
}
