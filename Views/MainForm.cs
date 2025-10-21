using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

public partial class MainForm : Form
{
    private ModbusClient _modbusClient;
    private Thread _pollingThread;
    private bool _pollingActive;

    // 通用功能按钮
    private Button btnLocalRemote;
    private Button btnStart;
    private Button btnStop;
    private Button btnSelfTest;
    private Button btnReset;

    // 工艺设定控件
    private Button btnProcessConfirm;
    private Button btnProcessRead;
    private Button btnProcessWrite;
    private TextBox txtRunProcessID;
    private TextBox txtEditID;
    private TextBox txtDIWSpeed;
    private TextBox txtDIWTime;
    private TextBox txtN2Speed;
    private TextBox txtN2Time;

    // 信号显示
    private Panel signalProcessRunning;
    private Panel signalProcessComplete;
    private Panel signalWaferDetection;
    private Panel signalStandbyComplete;
    private Panel signalAlarm;

    // 连接设置
    private TextBox txtIPAddress;
    private TextBox txtPort;
    private Button btnConnect;
    private Label lblConnectionStatus;

    public MainForm()
    {
        InitializeComponent();
        InitializeUI();
    }

    private void InitializeUI()
    {
        this.Text = "MODBUS TCP/IP 控制面板";
        this.Size = new Size(800, 600);

        // 连接设置区域
        var groupConnection = new GroupBox
        {
            Text = "连接设置",
            Location = new Point(20, 20),
            Size = new Size(740, 80)
        };
        this.Controls.Add(groupConnection);

        txtIPAddress = new TextBox
        {
            Text = "127.0.0.1",
            Location = new Point(20, 25),
            Size = new Size(150, 20)
        };
        groupConnection.Controls.Add(txtIPAddress);

        txtPort = new TextBox
        {
            Text = "502",
            Location = new Point(180, 25),
            Size = new Size(80, 20)
        };
        groupConnection.Controls.Add(txtPort);

        btnConnect = new Button
        {
            Text = "连接",
            Location = new Point(280, 25),
            Size = new Size(80, 25)
        };
        btnConnect.Click += BtnConnect_Click;
        groupConnection.Controls.Add(btnConnect);

        lblConnectionStatus = new Label
        {
            Text = "未连接",
            Location = new Point(380, 28),
            Size = new Size(200, 20),
            ForeColor = Color.Red
        };
        groupConnection.Controls.Add(lblConnectionStatus);

        // 通用功能区域
        var groupGeneral = new GroupBox
        {
            Text = "通用功能",
            Location = new Point(20, 120),
            Size = new Size(740, 80)
        };
        this.Controls.Add(groupGeneral);

        btnLocalRemote = CreateToggleButton("本地/远程", 20, 25, groupGeneral);
        btnStart = CreateToggleButton("启动", 120, 25, groupGeneral);
        btnStop = CreateToggleButton("停止", 220, 25, groupGeneral);
        btnSelfTest = CreateToggleButton("自检", 320, 25, groupGeneral);
        btnReset = CreateToggleButton("复位", 420, 25, groupGeneral);

        // 工艺设定区域
        var groupProcess = new GroupBox
        {
            Text = "工艺设定",
            Location = new Point(20, 220),
            Size = new Size(740, 180)
        };
        this.Controls.Add(groupProcess);

        // 工艺设定按钮
        btnProcessConfirm = CreateToggleButton("工艺确认", 20, 25, groupProcess);
        btnProcessRead = CreateToggleButton("工艺读取", 120, 25, groupProcess);
        btnProcessWrite = CreateToggleButton("工艺写入", 220, 25, groupProcess);

        // 工艺设定输入框
        AddLabelAndTextBox("运行工艺ID:", 20, 60, groupProcess, out txtRunProcessID);
        AddLabelAndTextBox("编辑ID:", 20, 90, groupProcess, out txtEditID);
        AddLabelAndTextBox("DIW转速:", 250, 60, groupProcess, out txtDIWSpeed);
        AddLabelAndTextBox("DIW时间:", 250, 90, groupProcess, out txtDIWTime);
        AddLabelAndTextBox("N2转速:", 480, 60, groupProcess, out txtN2Speed);
        AddLabelAndTextBox("N2时间:", 480, 90, groupProcess, out txtN2Time);

        // 信号显示区域
        var groupSignals = new GroupBox
        {
            Text = "信号显示",
            Location = new Point(20, 420),
            Size = new Size(740, 100)
        };
        this.Controls.Add(groupSignals);

        // 信号指示灯
        AddSignalIndicator("工艺运行中", 20, 25, groupSignals, out signalProcessRunning);
        AddSignalIndicator("工艺完成", 120, 25, groupSignals, out signalProcessComplete);
        AddSignalIndicator("晶圆检测", 220, 25, groupSignals, out signalWaferDetection);
        AddSignalIndicator("待机完成", 320, 25, groupSignals, out signalStandbyComplete);
        AddSignalIndicator("报警信号", 420, 25, groupSignals, out signalAlarm);
    }

    private Button CreateToggleButton(string text, int x, int y, Control parent)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(80, 25),
            Tag = false
        };

        btn.MouseDown += (sender, e) => 
        {
            btn.Tag = true;
            SendCoilCommand(btn);
        };

        btn.MouseUp += (sender, e) => 
        {
            btn.Tag = false;
            SendCoilCommand(btn);
        };

        parent.Controls.Add(btn);
        return btn;
    }

    private void AddLabelAndTextBox(string labelText, int x, int y, Control parent, out TextBox textBox)
    {
        var label = new Label
        {
            Text = labelText,
            Location = new Point(x, y),
            Size = new Size(80, 20)
        };
        parent.Controls.Add(label);

        textBox = new TextBox
        {
            Location = new Point(x + 85, y - 3),
            Size = new Size(100, 20)
        };
        parent.Controls.Add(textBox);
    }

    private void AddSignalIndicator(string labelText, int x, int y, Control parent, out Panel signal)
    {
        var label = new Label
        {
            Text = labelText,
            Location = new Point(x, y + 30),
            Size = new Size(80, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };
        parent.Controls.Add(label);

        signal = new Panel
        {
            Location = new Point(x + 30, y),
            Size = new Size(20, 20),
            BackColor = parent.BackColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        parent.Controls.Add(signal);
    }

    private void BtnConnect_Click(object sender, EventArgs e)
    {
        if (_modbusClient != null && _modbusClient.IsConnected)
        {
            _pollingActive = false;
            if (_pollingThread != null && _pollingThread.IsAlive)
            {
                _pollingThread.Join(500);
            }

            _modbusClient.Disconnect();
            btnConnect.Text = "连接";
            lblConnectionStatus.Text = "未连接";
            lblConnectionStatus.ForeColor = Color.Red;
        }
        else
        {
            if (int.TryParse(txtPort.Text, out int port))
            {
                _modbusClient = new ModbusClient(txtIPAddress.Text, port);
                if (_modbusClient.Connect())
                {
                    btnConnect.Text = "断开";
                    lblConnectionStatus.Text = "已连接";
                    lblConnectionStatus.ForeColor = Color.Green;

                    // 启动轮询线程
                    _pollingActive = true;
                    _pollingThread = new Thread(PollSignals);
                    _pollingThread.IsBackground = true;
                    _pollingThread.Start();
                }
                else
                {
                    MessageBox.Show("连接失败，请检查IP地址和端口", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("端口号无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void SendCoilCommand(Button btn)
    {
        if (_modbusClient == null || !_modbusClient.IsConnected) return;

        ushort address = 0;
        if (btn == btnLocalRemote) address = 0;
        else if (btn == btnStart) address = 1;
        else if (btn == btnStop) address = 2;
        else if (btn == btnSelfTest) address = 3;
        else if (btn == btnReset) address = 4;
        else if (btn == btnProcessConfirm) address = 5;
        else if (btn == btnProcessRead) address = 6;
        else if (btn == btnProcessWrite) address = 7;

        bool value = (bool)btn.Tag;
        _modbusClient.WriteSingleCoil(address, value);
    }

    private void PollSignals()
    {
        while (_pollingActive)
        {
            if (_modbusClient != null && _modbusClient.IsConnected)
            {
                try
                {
                    // 读取信号状态 (地址33-37)
                    var signals = _modbusClient.ReadCoils(33, 5);

                    if (signals != null && signals.Length >= 5)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            signalProcessRunning.BackColor = signals[0] ? Color.Green : this.BackColor;
                            signalProcessComplete.BackColor = signals[1] ? Color.Green : this.BackColor;
                            signalWaferDetection.BackColor = signals[2] ? Color.Green : this.BackColor;
                            signalStandbyComplete.BackColor = signals[3] ? Color.Green : this.BackColor;
                            signalAlarm.BackColor = signals[4] ? Color.Green : this.BackColor;
                        });
                    }

                    // 读取工艺参数
                    var processParams = _modbusClient.ReadHoldingRegisters(0, 6);
                    if (processParams != null && processParams.Length >= 6)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            txtRunProcessID.Text = processParams[0].ToString();
                            txtEditID.Text = processParams[1].ToString();
                            txtDIWSpeed.Text = processParams[2].ToString();
                            txtDIWTime.Text = processParams[3].ToString();
                            txtN2Speed.Text = processParams[4].ToString();
                            txtN2Time.Text = processParams[5].ToString();
                        });
                    }
                }
                catch
                {
                    // 忽略通信错误
                }
            }

            Thread.Sleep(500);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _pollingActive = false;
        if (_pollingThread != null && _pollingThread.IsAlive)
        {
            _pollingThread.Join(500);
        }

        _modbusClient?.Dispose();
        base.OnFormClosing(e);
    }
}
