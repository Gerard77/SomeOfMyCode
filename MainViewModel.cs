using DmitryBrant.ImageFormats;
using PageableDataGrid.Library;
using RemoteDriving.Client.BLL;
using RemoteDriving.ClientProxy_gRPC;
using RemoteDriving.Model;
using RemoteDriving.Model.Wheel;
using RemoteDriving.Tools;
using RemoteDriving.UI.Annotations;
using RemoteDriving.UI.View;
using RemoteDriving.UI.Viewmodel.Controller;
using RemoteDriving.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static RemoteDriving.Model.Enums;
using Application = System.Windows.Application;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Capgemini.Spain.RemoteDriving.Tools;
using Image = System.Drawing.Image;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Microsoft.Maps.MapControl.WPF;
using Capgemini.Spain.RemoteDriving.Model;
using System.Linq;
using RemoteDriving.Model.ModelRos;
using Capgemini.Spain.RemoteDriving.Ros2CSharp;
using Capgemini.Spain.RemoteDriving.UI.Tools;

namespace RemoteDriving.UI.Viewmodel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region variables privadas
        ClientProxy client;
        public BackgroundWorker backgroundWorker1;
        private bool WheelEnable = false;
        private static readonly Random _random = new Random();
        private int backgroundWorkerDelay = Parameters.Instance.GetInt("backgroundWorkerDelay");
        private int releaseServerVMDelay = Parameters.Instance.GetInt("releaseServerVMDelay");
        private string controlTripleScreen = Parameters.Instance.GetString("controlTripleScreen");
        private bool getLogsFromServerBool = Parameters.Instance.GetBool("getLogsFromServer");
        private bool startCamsAuto = Parameters.Instance.GetBool("startCamsAuto");
        private bool _isDebugMode = Parameters.Instance.GetBool("isDebugMode");
        private bool autoStartRemoteDriving = Parameters.Instance.GetBool("autoStartRemoteDriving");
        private bool autoSwitchToTripleScreenAtStart = Parameters.Instance.GetBool("autoSwitchToTripleScreenAtStart");
        private bool codeBehindCallsClientProxy = Parameters.Instance.GetBool("codeBehindCallsClientProxy");
        private bool ros2csInClient = Parameters.Instance.GetBool("ros2csInClient");

        private ROS2Listener ros2listener;

        private SharpDXController objWheelDX;
        private CancellationTokenSource backgroundToken;
        private bool flagWheel = false;
        private bool flagCamera = false;
        private bool flagCamera2 = false;
        private bool flagCamera3 = false;
        private bool flagCamera4 = false;
        private bool flagCamera5 = false;
        private bool flagLatency = false;
        private bool flagDisplayLogs = false;
        private bool flagLegendButtons = false;
        private bool flagFirstControlPanel = true;
        private bool flagGPS = false;
        private bool flagVH = false;
        private bool flagBateriesAreOK = true;

        private int viewState;
        private enum viewStates
        {
            compactLayout = 0,
            tripleLayout = 1
        }

        //--TODO-- switch frontend to rear visibility
        private bool flagRearCamState = false;

        private WheelInfo info = null;

        //LOGS:
        private int _logId;
        private SortablePageableCollection<LogInfo> _dataLog;
        public SortablePageableCollection<LogInfo> DataLog
        {
            get => _dataLog;
        }
        private MainLayoutManager _manager;
        private System.Threading.Timer printDataLogTimer;
        private System.Threading.Timer getLogsFromServerTimer;
        private System.Threading.Timer borderReverseVisibilityTimer;
        private System.Threading.Timer Check_ICU_Timer;
        private bool flagVisibilityBorderReverse = true;

        private System.Threading.Timer saveFramesTimer;
        private int id_frame1 = 0;
        private int id_frame2 = 0;
        private int id_frame3 = 0;
        private int id_frame4 = 0;
        private int id_frame5 = 0;
        private static bool saveFramesReceived = Parameters.Instance.GetBool("saveFramesReceived");

        //the log list by default is playing:
        private bool _LogsStartStopLogs = true;

        //Triple screen (mainscreen)
        private bool _threeScreensConnected;
        private MainScreen _MainScreen;
        private RightScreen _RightScreen;
        private LeftScreen _LeftScreen;
        private UserInterface.Window _WindowStartUI;

        private int _IntValueBattery1;
        private int _IntValueBattery2;
        private int _IntValueBattery3;

        private bool onPanelClick;
        private bool canChange;

        //Variables to count the real value of fps from cameras printed to the frontend
        private int FPS1_Real_int = 0;
        private int FPS2_Real_int = 0;
        private int FPS3_Real_int = 0;
        private int FPS4_Real_int = 0;
        private int FPS5_Real_int = 0;
        private System.Threading.Timer FPS1Timer;
        private System.Threading.Timer FPS2Timer;
        private System.Threading.Timer FPS3Timer;
        private System.Threading.Timer FPS4Timer;
        private System.Threading.Timer FPS5Timer;

        private Capgemini.Spain.RemoteDriving.Model.ModelRos.Remotis.IcU auxICU = new Capgemini.Spain.RemoteDriving.Model.ModelRos.Remotis.IcU(0,0,0,0,0,0,0,0,0);
        private Capgemini.Spain.RemoteDriving.Model.ModelRos.Remotis.IcU newICUData = new Capgemini.Spain.RemoteDriving.Model.ModelRos.Remotis.IcU(0, 0, 0, 0, 0, 0, 0, 0, 0);
        #endregion

        #region propiedades de pantalla

        private Visibility _logVisibility;
        public Visibility LogVisibility
        {
            get
            {
                return _logVisibility;
            }
        }
        private Visibility _onBoardCamVisibility;
        public Visibility OnBoardCamVisibility
        {
            get
            {
                return _onBoardCamVisibility;
            }
        }
        private Visibility _rearCamVisibility;
        public Visibility RearCamVisibility
        {
            get
            {
                return _rearCamVisibility;
            }
        }

        private Visibility _rearIconVisibility;
        public Visibility RearIconVisibility
        {
            get
            {
                return _rearIconVisibility;
            }
        }

        private Visibility _boardIconVisibility;
        public Visibility BoardIconVisibility
        {
            get
            {
                return _boardIconVisibility;
            }
        }

        private Visibility _logsIconVisibility;
        public Visibility LogsIconVisibility
        {
            get
            {
                return _logsIconVisibility;
            }
        }

        private Visibility _mapIconVisibility;
        public Visibility MapIconVisibility
        {
            get
            {
                return _mapIconVisibility;
            }
        }

        private Visibility _mapViewVisibility;
        public Visibility MapViewVisibility
        {
            get
            {
                return _mapViewVisibility;
            }
        }

        private Visibility _carViewVisibility;
        public Visibility CarViewVisibility
        {
            get
            {
                return _carViewVisibility;
            }
        }

        private Visibility _carIconVisibility;
        public Visibility CarIconVisibility
        {
            get
            {
                return _carIconVisibility;
            }
        }

        private Visibility _panelViewVisibility;
        public Visibility PanelViewVisibility
        {
            get
            {
                return _panelViewVisibility;
            }
        }
        private Visibility _legendButtonVisibility;
        public Visibility LegendButtonsVisibility
        {
            get
            {
                return _legendButtonVisibility;
            }
        }
        private Visibility _wheelNotConnectedVisibility;
        public Visibility WheelNotConnectedVisibility
        {
            get
            {
                return _wheelNotConnectedVisibility;
            }
        }

        private Visibility _ReverseBorderVisibility;
        public Visibility ReverseBorderVisibility
        {
            get
            {
                return _ReverseBorderVisibility;
            }
        }

        private String _panelMargin;
        public String PanelMargin
        {
            get
            {
                return _panelMargin;
            }
        }

        private String _velocityMargin;
        public String VelocityMargin
        {
            get
            {
                return _velocityMargin;
            }
        }

        private String _kmMargin;
        public String KMMargin
        {
            get
            {
                return _kmMargin;
            }
        }

        private BitmapImage _Foto;
        public ImageSource Foto
        {
            get
            {
                return _Foto;
            }
        }
        private BitmapImage _Foto2;
        public ImageSource Foto2
        {
            get
            {
                return _Foto2;
            }
        }
        private BitmapImage _Foto3;
        public ImageSource Foto3
        {
            get
            {
                return _Foto3;
            }
        }
        private BitmapImage _Foto4;
        public ImageSource Foto4
        {
            get
            {
                return _Foto4;
            }
        }
        private BitmapImage _Foto5;
        public ImageSource Foto5
        {
            get
            {
                return _Foto5;
            }
        }
        private BitmapImage _Foto_legend;
        public ImageSource Foto_legend
        {
            get
            {
                return _Foto_legend;
            }
        }
        private string _FPS;
        public string FPS
        {
            get
            {
                return _FPS;
            }
        }
        private string _FPS2;
        public string FPS2
        {
            get
            {
                return _FPS2;
            }
        }
        private string _FPS3;
        public string FPS3
        {
            get
            {
                return _FPS3;
            }
        }
        private string _FPS4;
        public string FPS4
        {
            get
            {
                return _FPS4;
            }
        }
        private string _FPS5;
        public string FPS5
        {
            get
            {
                return _FPS5;
            }
        }
        private string _LatencyCam1;
        public string LatencyCam1
        {
            get
            {
                return _LatencyCam1;
            }
        }
        private string _LatencyCam2;
        public string LatencyCam2
        {
            get
            {
                return _LatencyCam2;
            }
        }
        private string _LatencyCam3;
        public string LatencyCam3
        {
            get
            {
                return _LatencyCam3;
            }
        }
        private string _LatencyCam4;
        public string LatencyCam4
        {
            get
            {
                return _LatencyCam4;
            }
        }
        private string _LatencyCam5;
        public string LatencyCam5
        {
            get
            {
                return _LatencyCam5;
            }
        }
        private string _LatencyGPS;
        public string LatencyGPS
        {
            get
            {
                return _LatencyGPS;
            }
        }
        private string _FPS_Real;
        public string FPS_Real
        {
            get
            {
                return _FPS_Real;
            }
        }
        private string _FPS2_Real;
        public string FPS2_Real
        {
            get
            {
                return _FPS2_Real;
            }
        }
        private string _FPS3_Real;
        public string FPS3_Real
        {
            get
            {
                return _FPS3_Real;
            }
        }
        private string _FPS4_Real;
        public string FPS4_Real
        {
            get
            {
                return _FPS4_Real;
            }
        }
        private string _FPS5_Real;
        public string FPS5_Real
        {
            get
            {
                return _FPS5_Real;
            }
        }
        private string _ObstaclesMessagesCounter;
        public string ObstaclesMessagesCounter
        {
            get
            {
                return _ObstaclesMessagesCounter;
            }
        }
        private string _StatusMessagesCounter;
        public string StatusMessagesCounter
        {
            get
            {
                return _StatusMessagesCounter;
            }
        }
        private string _TrafficMessagesCounter;
        public string TrafficMessagesCounter
        {
            get
            {
                return _TrafficMessagesCounter;
            }
        }
        private string _WorldModelMessagesCounter;
        public string WorldModelMessagesCounter
        {
            get
            {
                return _WorldModelMessagesCounter;
            }
        }
        private string _CommandMessagesCounter;
        public string CommandMessagesCounter
        {
            get
            {
                return _CommandMessagesCounter;
            }
        }
        private string _LanesMessagesCounter;
        public string LanesMessagesCounter
        {
            get
            {
                return _LanesMessagesCounter;
            }
        }
        private string _txtButtonWheelPressed;
        public string txtButtonWheelPressed
        {
            get
            {
                return _txtButtonWheelPressed;
            }
        }
        private string _reverseDrivingState;
        public string reverseDrivingState
        {
            get
            {
                return _reverseDrivingState;
            }
        }

        //Background coloring for Driving State and Stop Logs
        private string _reverseDrivingStateBckg;
        public string reverseDrivingStateBckg
        {
            get
            {
                return _reverseDrivingStateBckg;
            }
        }
        private string _stopLogsStateBckg;
        public string stopLogsStateBckg
        {
            get
            {
                return _stopLogsStateBckg;
            }
        }
        private string _ButtonStartStopLogsText;
        public string ButtonStartStopLogsText
        {
            get
            {
                return _ButtonStartStopLogsText;
            }
        }
        private bool _LogsShowDebugEnabled = true;
        public bool LogsShowDebugEnabled
        {
            get
            {
                return _LogsShowDebugEnabled;
            }
        }
        private bool _LogsShowInfoEnabled = true;
        public bool LogsShowInfoEnabled
        {
            get
            {
                return _LogsShowInfoEnabled;
            }
        }
        private bool _LogsShowWarningsEnabled = true;
        public bool LogsShowWarningsEnabled
        {
            get
            {
                return _LogsShowWarningsEnabled;
            }
        }
        private bool _LogsShowErrorsEnabled = true;
        public bool LogsShowErrorsEnabled
        {
            get
            {
                return _LogsShowErrorsEnabled;
            }
        }
        private bool _LogsShowDebugChecked = true;
        public bool LogsShowDebugChecked
        {
            get
            {
                return _LogsShowDebugChecked;
            }
        }
        private bool _LogsShowInfoChecked = true;
        public bool LogsShowInfoChecked
        {
            get
            {
                return _LogsShowInfoChecked;
            }
        }
        private bool _LogsShowWarningsChecked = true;
        public bool LogsShowWarningsChecked
        {
            get
            {
                return _LogsShowWarningsChecked;
            }
        }
        private bool _LogsShowErrorsChecked = true;
        public bool LogsShowErrorsChecked
        {
            get
            {
                return _LogsShowErrorsChecked;
            }
        }

        private string _LogErrorDescriptor;
        public string LogErrorDescriptor
        {
            get
            {
                return _LogErrorDescriptor;
            }
        }
        public string _LogsTextSearch;
        public string LogsTextSearch
        {
            set
            {
                _LogsTextSearch = value;
            }
        }
        private bool _ChangeViewIsEnabled = true;
        public bool ChangeViewIsEnabled
        {
            get
            {
                return _ChangeViewIsEnabled;
            }
        }

        private String _IconError;

        public string IconError
        {
            get
            {
                return _IconError;
            }
        }

        private String _IconHandBrake;
        public string IconHandBrake
        {
            get
            {
                return _IconHandBrake;
            }
        }

        private String _IconReverse;
        public string IconReverse
        {
            get
            {
                return _IconReverse;
            }
        }

        private String _IconWifi;
        public string IconWifi
        {
            get
            {
                return _IconWifi;
            }
        }

        private String _IconHighVoltage;
        public string IconHighVoltage
        {
            get
            {
                return _IconHighVoltage;
            }
        }

        private String _IconLowVoltage;
        public string IconLowVoltage
        {
            get
            {
                return _IconLowVoltage;
            }
        }

        private String _IconBattery1;
        public string IconBattery1
        {
            get
            {
                return _IconBattery1;
            }
        }

        private String _IconBattery2;
        public string IconBattery2
        {
            get
            {
                return _IconBattery2;
            }
        }

        private String _IconBattery3;
        public string IconBattery3
        {
            get
            {
                return _IconBattery3;
            }
        }

        private String _ValueBattery1;
        public String ValueBattery1
        {
            get
            {
                return _ValueBattery1;
            }
        }
       
        private String _ColorBat1;
        public String ColorBat1
        {
            get
            {
                return _ColorBat1;
            }
        }
        private String _ColorBat2;
        public String ColorBat2
        {
            get
            {
                return _ColorBat2;
            }
        }

        private String _ColorBat3;
        public String ColorBat3
        {
            get
            {
                return _ColorBat3;
            }
        }
        private double _oldAngleLeftWheel = 0;
        private double _oldAngleRightWheel = 0;
        private double _AngleLeftWheel;
        public double AngleLeftWheel
        {
            get
            {
                return _AngleLeftWheel;
            }
        }

        private double _AngleRightWheel;
        public double AngleRightWheel
        {
            get
            {
                return _AngleRightWheel;
            }
        }

        private String _Velocity;
        public String Velocity
        {
            get
            {
                return _Velocity;
            }
        }

        private String _XTransformPanel;
        public String XTransformPanel
        {
            get
            {
                return _XTransformPanel;
            }
        }
        private String _YTransformPanel;
        public String YTransformPanel
        {
            get
            {
                return _YTransformPanel;
            }
        }

        private String _ToTransform2;
        public String ToTransform2
        {
            get
            {
                return _ToTransform2;
            }
        }
        private String _FromTransform2;
        public String FromTransform2
        {
            get
            {
                return _FromTransform2;
            }
        }

        private String _PanelTransform;
        public String PanelTransform
        {
            get
            {
                return _PanelTransform;
            }
        }
        #endregion

        #region propiedades de pantalla botones
        private AsyncCommand _ButtonCaptureWheel;

        private AsyncCommand _ButtonCaptureRandom;

        private AsyncCommand _ButtonStopCapture;

        private AsyncCommand _ButtonCaptureCamera;

        private AsyncCommand _ButtonStopCamera;

        private AsyncCommand _ButtonCaptureCam2;

        private AsyncCommand _ButtonCaptureCam3;

        private AsyncCommand _ButtonCaptureCam4;

        private AsyncCommand _ButtonCaptureCam5;

        private AsyncCommand _ButtonStopCam2;

        private AsyncCommand _ButtonStopCam3;

        private AsyncCommand _ButtonStopCam4;

        private AsyncCommand _ButtonStopCam5;

        private AsyncCommand _ButtonLogListShow;

        private AsyncCommand _ButtonLogListHide;

        private AsyncCommand _ChangeView;

        private AsyncCommand _ButtonShowErrors;

        private AsyncCommand _ButtonShowWarnings;

        private AsyncCommand _ButtonShowDebug;

        private AsyncCommand _ButtonShowInfo;

        private AsyncCommand _ButtonShowAll;

        private AsyncCommand _GoToPreviousLogPageCommand;

        private AsyncCommand _GoToNextLogPageCommand;

        private AsyncCommand _ButtonStopLogs;

        private AsyncCommand _ButtonSearchLogs;

        private AsyncCommand _ButtonMapVis;

        private AsyncCommand _ButtonCarVis;

        private AsyncCommand _ButtonPanelVis;

        private AsyncCommand _ButtonCaptureLatency;

        private AsyncCommand _ButtonStopLatency;

        private AsyncCommand _ButtonSaveFrames;

        public AsyncCommand ButtonCaptureWheel
        {
            get
            {
                return _ButtonCaptureWheel ??= new AsyncCommand(async () => { await Button_Click("1"); });
            }
        }
        public AsyncCommand ButtonCaptureRandom
        {
            get
            {
                return _ButtonCaptureRandom ??= new AsyncCommand(async () => { await Button_Click("2"); });
            }
        }
        public AsyncCommand ButtonStopCapture
        {
            get
            {
                return _ButtonStopCapture ??= new AsyncCommand(async () => { await Button_Click("3"); });
            }
        }
        public AsyncCommand ButtonCaptureCamera
        {
            get
            {
                return _ButtonCaptureCamera ??= new AsyncCommand(async () => { await Button_Click("4"); });
            }
        }
        public AsyncCommand ButtonStopCamera
        {
            get
            {
                return _ButtonStopCamera ??= new AsyncCommand(async () => { await Button_Click("5"); });
            }
        }
        public AsyncCommand ButtonCaptureCam2
        {
            get
            {
                return _ButtonCaptureCam2 ??= new AsyncCommand(async () => { await Button_Click("6"); });
            }
        }
        public AsyncCommand ButtonCaptureCam3
        {
            get
            {
                return _ButtonCaptureCam3 ??= new AsyncCommand(async () => { await Button_Click("7"); });
            }
        }
        public AsyncCommand ButtonStopCam2
        {
            get
            {
                return _ButtonStopCam2 ??= new AsyncCommand(async () => { await Button_Click("8"); });
            }
        }
        public AsyncCommand ButtonStopCam3
        {
            get
            {
                return _ButtonStopCam3 ??= new AsyncCommand(async () => { await Button_Click("9"); });
            }
        }
        public AsyncCommand ChangeView
        {
            get
            {
                return _ChangeView ??= new AsyncCommand(async () => { await Button_Click("10"); });
            }
        }
        public AsyncCommand ButtonCaptureCam4
        {
            get
            {
                return _ButtonCaptureCam4 ??= new AsyncCommand(async () => { await Button_Click("11"); });
            }
        }
        public AsyncCommand ButtonStopCam4
        {
            get
            {
                return _ButtonStopCam4 ??= new AsyncCommand(async () => { await Button_Click("12"); });
            }
        }
        public AsyncCommand ButtonCaptureCam5
        {
            get
            {
                return _ButtonCaptureCam5 ??= new AsyncCommand(async () => { await Button_Click("13"); });
            }
        }
        public AsyncCommand ButtonStopCam5
        {
            get
            {
                return _ButtonStopCam5 ??= new AsyncCommand(async () => { await Button_Click("14"); });
            }
        }
        public AsyncCommand ButtonLogListShow
        {
            get
            {
                return _ButtonLogListShow ??= new AsyncCommand(async () => { await Button_Click("15"); });
            }
        }
        public AsyncCommand ButtonLogListHide
        {
            get
            {
                return _ButtonLogListHide ??= new AsyncCommand(async () => { await Button_Click("16"); });
            }
        }
        public AsyncCommand ButtonShowErrors
        {
            get
            {
                return _ButtonShowErrors ??= new AsyncCommand(async () => { await Button_Click("17"); });
            }
        }
        public AsyncCommand ButtonShowWarnings
        {
            get
            {
                return _ButtonShowWarnings ??= new AsyncCommand(async () => { await Button_Click("18"); });
            }
        }
        public AsyncCommand ButtonShowDebug
        {
            get
            {
                return _ButtonShowDebug ??= new AsyncCommand(async () => { await Button_Click("19"); });
            }
        }
        public AsyncCommand ButtonShowInfo
        {
            get
            {
                return _ButtonShowInfo ??= new AsyncCommand(async () => { await Button_Click("20"); });
            }
        }
        public AsyncCommand GoToPreviousLogPageCommand
        {
            get
            {
                return _GoToPreviousLogPageCommand ??= new AsyncCommand(async () => { await Button_Click("21"); });
            }
        }
        public AsyncCommand GoToNextLogPageCommand
        {
            get
            {
                return _GoToNextLogPageCommand ??= new AsyncCommand(async () => { await Button_Click("22"); });
            }
        }
        public AsyncCommand ButtonSearchLogs
        {
            get
            {
                return _ButtonSearchLogs ??= new AsyncCommand(async () => { await Button_Click("23"); });
            }
        }
        public AsyncCommand ButtonStopLogs
        {
            get
            {
                return _ButtonStopLogs ??= new AsyncCommand(async () => { await Button_Click("24"); });
            }
        }
        public AsyncCommand ButtonMapVis
        {
            get
            {
                return _ButtonMapVis ??= new AsyncCommand(async () => { await Button_Click("25"); });
            }
        }
        public AsyncCommand ButtonCarVis
        {
            get
            {
                return _ButtonCarVis ??= new AsyncCommand(async () => { await Button_Click("26"); });
            }
        }
        public AsyncCommand ButtonPanelVis
        {
            get
            {
                return _ButtonPanelVis ??= new AsyncCommand(async () => { await Button_Click("27"); });
            }
        }
        public AsyncCommand ButtonCaptureLatency
        {
            get
            {
                return _ButtonCaptureLatency ??= new AsyncCommand(async () => { await Button_Click("28"); });
            }
        }
        public AsyncCommand ButtonStopLatency
        {
            get
            {
                return _ButtonStopLatency ??= new AsyncCommand(async () => { await Button_Click("29"); });
            }
        }
        public AsyncCommand ButtonSaveFrames
        {
            get
            {
                return _ButtonSaveFrames ??= new AsyncCommand(async () => { await Button_Click("30"); });
            }
        }

        
        #endregion

        #region propiedades de botones del volante

        private readonly string PRESSED_0 = "0 pressed";
        private readonly string UNPRESSED_0 = "0 unpressed";
        private readonly string PRESSED_1 = "1 pressed";
        private readonly string UNPRESSED_1 = "1 unpressed";
        private readonly string PRESSED_2 = "2 pressed";
        private readonly string UNPRESSED_2 = "2 unpressed";
        private readonly string PRESSED_3 = "3 pressed";
        private readonly string UNPRESSED_3 = "3 unpressed";
        private readonly string PRESSED_4 = "4 pressed";
        private readonly string UNPRESSED_4 = "4 unpressed";
        private readonly string PRESSED_5 = "5 pressed";
        private readonly string UNPRESSED_5 = "5 unpressed";
        private readonly string PRESSED_7 = "7 pressed";
        private readonly string UNPRESSED_7 = "7 unpressed";
        private readonly string PRESSED_8 = "8 pressed";
        private readonly string UNPRESSED_8 = "8 unpressed";
        private readonly string PRESSED_9 = "9 pressed";
        private readonly string UNPRESSED_9 = "9 unpressed";
        private readonly string PRESSED_10 = "10 pressed";
        private readonly string UNPRESSED_10 = "10 unpressed";
        private readonly string PRESSED_11 = "11 pressed";
        private readonly string UNPRESSED_11 = "11 unpressed";
        private readonly string PRESSED_12 = "12 pressed";
        private readonly string UNPRESSED_12 = "12 unpressed";

        private string last_0_button;
        private string current_0_button = String.Empty;
        private string last_1_button;
        private string current_1_button = String.Empty;
        private string last_2_button;
        private string current_2_button = String.Empty;
        private string last_3_button;
        private string current_3_button = String.Empty;
        private string last_4_button;
        private string current_4_button = String.Empty;
        private string last_5_button;
        private string current_5_button = String.Empty;
        private string last_7_button;
        private string current_7_button = String.Empty;
        private string last_8_button;
        private string current_8_button = String.Empty;
        private string last_9_button;
        private string current_9_button = String.Empty;
        private string last_10_button;
        private string current_10_button = String.Empty;
        private string last_11_button;
        private string current_11_button = String.Empty;
        private string last_12_button;
        private string current_12_button = String.Empty;

        private AsyncCommand _Pruebas;
        public AsyncCommand Pruebas
        {
            get
            {
                return _Pruebas ??= new AsyncCommand(() => { SwitchWheelButtonPressed("8 unpressed"); });
            }
        }

        #endregion
        public MainViewModel()
        {
            HelperLog.Instance.LogLevel = Parameters.Instance.GetInt("logLevel");
            HelperLog.Instance.WriteDebug(MethodInfo.GetCurrentMethod(), "----ViewModel Init Start----");

            _manager = new MainLayoutManager();
            bool wheelNotConnected = CheckWheelConnected();
            physicalScreensCheck();
            InitCompactLayout();
            InitContent();

            Application.Current.Dispatcher.ShutdownStarted += releaseServerViewModel;
            _dataLog = new SortablePageableCollection<LogInfo>((IEnumerable<LogInfo>)new List<LogInfo>());

            //Execute PrintDataLog once to initialize variables
            PrintDataLog(0);
            printDataLogTimer = new System.Threading.Timer(_ => PrintDataLog(1), null, 0, 2000);

            client = new ClientProxy();
            if (!codeBehindCallsClientProxy)
            {
                ClientEventHandlerControllers();
            }

            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);

            if (ros2csInClient) //Case in which we using ros2cs directly in client for cams display
            {
                ros2listener = ROS2Listener.Instance;
            }
            if (!wheelNotConnected && autoStartRemoteDriving)
            {
                Button_Click("1");
            }
            if (getLogsFromServerBool)
            {
                getLogsFromServerTimer = new System.Threading.Timer(_ => getLogsFromServer(), null, 0, 2000);
            }
            if (startCamsAuto) //Only for cams coming from server, incompatible with codeBehindCallsClientProxy = true
            {
                Button_Click("4");
                Button_Click("6");
                Button_Click("7");
            }
            if (_isDebugMode == false)
            {
                if (autoSwitchToTripleScreenAtStart)
                {
                    SwitchViewLayout();
                }
                ActivateGpsService();
                ActivateVehicleHardware();
            }
            HelperLog.Instance.WriteDebug(MethodInfo.GetCurrentMethod(), "----ViewModel Init End----");
        }
        private void InitCompactLayout()
        {
            _reverseDrivingState = "reverse driving disabled";
            viewState = (int)viewStates.compactLayout;

            _WindowStartUI = new UserInterface.Window();
            if (_threeScreensConnected) _WindowStartUI.Left = 2080;
            else _WindowStartUI.Left = 0;
            _WindowStartUI.Top = 0;
            _WindowStartUI.Width = 1600;
            _WindowStartUI.Height = 840;
			
            _WindowStartUI.NavigationService.Navigate(new CompactLayout { DataContext = this });
            _WindowStartUI.Show();
            _onBoardCamVisibility = Visibility.Hidden;
            _rearCamVisibility = Visibility.Hidden;
            _logVisibility = Visibility.Hidden;
            _mapViewVisibility = Visibility.Visible;
            _mapIconVisibility = Visibility.Hidden;
            _carViewVisibility = Visibility.Visible;
            _carIconVisibility = Visibility.Hidden;
            _legendButtonVisibility = Visibility.Hidden;
            _ReverseBorderVisibility = Visibility.Hidden;

            _panelMargin = "0,235,0,-235";
            _velocityMargin = "0,283,0,0";
            _kmMargin = "0,381,0,0";

            if (controlTripleScreen == "true" && _threeScreensConnected == false) { _ChangeViewIsEnabled = false; OnPropertyChanged("ChangeViewIsEnabled"); }
            if (_threeScreensConnected) { _WindowStartUI.Left = 2080; }

            OnPropertyChanged("ChangeViewIsEnabled");

            ButtonStopCapture.SetCanExecute(false);
            ButtonStopCamera.SetCanExecute(false);
            ButtonStopCam2.SetCanExecute(false);
            ButtonStopCam3.SetCanExecute(false);
            ButtonStopCam4.SetCanExecute(false);
            ButtonStopCam5.SetCanExecute(false);
            ButtonLogListHide.SetCanExecute(false);
            ButtonStopLatency.SetCanExecute(false);
            _stopLogsStateBckg = "#FFD3D3D3";
            OnPropertyChanged("stopLogsStateBckg");
            _ButtonStartStopLogsText = "Stop Logs";
            OnPropertyChanged("ButtonStartStopLogsText");
            _reverseDrivingStateBckg = Colors.Firebrick.ToString();
            OnPropertyChanged("reverseDrivingStateBckg");
        }
        private BitmapImage imgConversionPpm(byte[] imagebytes)
        {
            if (imagebytes != null)
            {
                using (var ms = new System.IO.MemoryStream(imagebytes))
                {
                    Bitmap bmp = PnmReader.Load(ms);
                    using (var memory = new System.IO.MemoryStream())
                    {
                        bmp.Save(memory, ImageFormat.Png);
                        memory.Position = 0;
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();

                        return bitmapImage;
                    }
                }
            }
            else
            {
                return null;
            }
        }
        private BitmapImage imgConversionBitmap(byte[] imagebytes)
        {
            if (imagebytes != null)
            {
                var memory = new MemoryStream(imagebytes);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
            else
            {
                return null;
            }
        }
        private BitmapImage imgConversionImageSharp(byte[] imagebytes)
        {
            if (imagebytes != null)
            {
                using (var ms = new System.IO.MemoryStream(imagebytes))
                {
                    SixLabors.ImageSharp.Image<Rgb24> img = SixLabors.ImageSharp.Image.Load<Rgb24>(imagebytes);
                    Bitmap bmp = ImageSharpExtensions.ToBitmap<Rgb24>(img);
                    using (var memory = new System.IO.MemoryStream())
                    {
                        bmp.Save(memory, ImageFormat.Png);
                        memory.Position = 0;
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();

                        return bitmapImage;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        private void InitContent()
        {
            _PanelTransform = "150";
            _YTransformPanel = "150";
            _panelViewVisibility = Visibility.Visible;
            onPanelClick = true;
			
            _IconError = "../Images/ERROR.png";
            _IconHandBrake = "../Images/HANDBRAKE.png";
            _IconReverse = "../Images/GRAY_REVERSE.png";
            _IconWifi = "../Images/WIFI.png";
            _IconHighVoltage = "../Images/HIGHVOLTAGE.png";
            _IconLowVoltage = "../Images/LOWVOLTAGE.png";

            BitmapImage bmImg = new BitmapImage(new Uri("pack://application:,,,/Images/CentralView.png"));
            _Foto = bmImg;

            BitmapImage bmImg2 = new BitmapImage(new Uri("pack://application:,,,/Images/LeftView.png"));
            _Foto2 = bmImg2;

            BitmapImage bmImg3 = new BitmapImage(new Uri("pack://application:,,,/Images/RightView.png"));
            _Foto3 = bmImg3;

            BitmapImage bmLegend = new BitmapImage(new Uri("pack://application:,,,/Images/botones_volante.png"));

            _Foto_legend = bmLegend;

            //Battery values
            _IntValueBattery1 = 100;
            _IntValueBattery2 = 100;
            _IntValueBattery3 = 100;

            if(_IntValueBattery1 < 100)
            {
                _ValueBattery1 = _IntValueBattery1 + "%";
            }
            else
            {
                _ValueBattery1 = _IntValueBattery1 + "%";
            }
               
            //under 50% 
            if(_IntValueBattery1 < 25)
            {
                _ColorBat1 = "#FF5372";
                _IconBattery1 = "../Images/Red_Battery1.png";
            }
            else
            {
                _ColorBat1 = "#70D174";
                _IconBattery1 = "../Images/Green_Battery1.png";
            }
            if (_IntValueBattery2 < 25)
            {
                _ColorBat2 = "#FF5372";
                _IconBattery2 = "../Images/Red_Battery2.png";
            }
            else
            {
                _ColorBat2 = "#70D174";
                _IconBattery2 = "../Images/Green_Battery2.png";
            }

            if (_IntValueBattery3 < 25)
            {
                _ColorBat3 = "#FF5372";
                _IconBattery3 = "../Images/Red_Battery3.png";
            }
            else
            {
                _ColorBat3 = "#70D174";
                _IconBattery3 = "../Images/Green_Battery3.png";
            }

            //Speed indicator
            _Velocity = "0";

            //Angle Wheels
            _AngleLeftWheel = 0.0;
            _AngleRightWheel = 0;

            Check_ICU_Timer = new System.Threading.Timer(_ => Check_ICU_Timer_Action(), null, 0, 5000);

            OnPanelVisible();
        }
        private void Check_ICU_Timer_Action()
        {
            if (newICUData.imu_accel_x == auxICU.imu_accel_x && newICUData.imu_accel_y == auxICU.imu_accel_y &&
                newICUData.imu_accel_z == auxICU.imu_accel_z && newICUData.imu_gyro_x == auxICU.imu_gyro_x &&
                newICUData.imu_gyro_y == auxICU.imu_gyro_y && newICUData.imu_gyro_z == auxICU.imu_gyro_z &&
                newICUData.imu_mag_x == auxICU.imu_mag_x && newICUData.imu_mag_y == auxICU.imu_mag_y &&
                newICUData.imu_mag_z == auxICU.imu_mag_z)
            {
                flagBateriesAreOK = true;
            }
            else
            {
                flagBateriesAreOK = false;
            }
        }

        public void OnPanelVisible()
        {
			//Checking of params in control panel, if any is zero or red must be shown
            if(_Velocity == "0" || _IntValueBattery1 < 25 || _IntValueBattery2 < 25 || _IntValueBattery3 < 25)
            {
                canChange = false;
                onPanelClick = false;
                
                _panelMargin = "0,87,0,-84";
                _velocityMargin = "0,173,0,0";
                _kmMargin = "0,273,0,0";

                _PanelTransform = "0";
                _XTransformPanel = "0";
                _YTransformPanel = "0"; 
                OnPropertyChanged("XTransformPanel");
                OnPropertyChanged("YTransformPanel");
                _ToTransform2 = "0";
                OnPropertyChanged("ToTransform2");
                OnPropertyChanged("PanelTransform");

                OnPropertyChanged("PanelViewVisibility");
                OnPropertyChanged("PanelMargin");
                OnPropertyChanged("KMMargin");
                OnPropertyChanged("VelocityMargin");
            }
            else
            {
                canChange = true;
            }
        }

        public void PrintDataLog(int init)
        {
            List<string> logTypesChecked = new List<string>();
            if (init == 0)
            {
                _logId = 0;
                _dataLog = new SortablePageableCollection<LogInfo>((IEnumerable<LogInfo>)new List<LogInfo>());
            }
            if (LogsShowDebugChecked)
            {
                logTypesChecked.Add(HelperLogType.debug.ToString());
            }
            if (LogsShowInfoChecked)
            {
                logTypesChecked.Add(HelperLogType.info.ToString());
            }
            if (LogsShowWarningsChecked)
            {
                logTypesChecked.Add(HelperLogType.warning.ToString());
            }
            if (LogsShowErrorsChecked)
            {
                logTypesChecked.Add(HelperLogType.error.ToString());
            }
            for(var i=0;i<HelperLog.Instance.LogData.Count;i++)
            {
                if (HelperLog.Instance.LogData[i].Id < HelperLog.Instance.LogId && 
                    HelperLog.Instance.LogData[i].Id >= _logId && 
                    logTypesChecked.Contains(HelperLog.Instance.LogData[i].Type.ToString()))
                {
                    _logId++;
                    _dataLog.Add(HelperLog.Instance.LogData[i]);
                }
            }
            _logId = HelperLog.Instance.LogId;
            OnPropertyChanged("DataLog");
        }

        public void visibilityBorderReverse()
        {
            if (flagVisibilityBorderReverse)
            {
                _ReverseBorderVisibility = Visibility.Visible;
            }
            else
            {
                _ReverseBorderVisibility = Visibility.Hidden;
            }
            flagVisibilityBorderReverse = !flagVisibilityBorderReverse;
            OnPropertyChanged("ReverseBorderVisibility");
        }
        public void getLogsFromServer()
        {
            ClientProxy.connectLogsService();
        }

        public void PrintDataLogSearch(string text)
        {
            _logId = 0;
            _dataLog = new SortablePageableCollection<LogInfo>((IEnumerable<LogInfo>)new List<LogInfo>());
            foreach (var logInfo in HelperLog.Instance.LogData)
            {
                if (logInfo.Id < HelperLog.Instance.LogId && logInfo.Id >= _logId && (logInfo.Date.ToString().Contains(text)
                    || logInfo.Description.Contains(text) || logInfo.FromClass.Contains(text) || logInfo.FromMethod.Contains(text)
                    || logInfo.Type.ToString().Contains(text) || logInfo.Trace.Contains(text) || logInfo.Id.ToString().Contains(text)))
                {
                    _logId++;
                    _dataLog.Add(logInfo);
                }
            }
            _logId = HelperLog.Instance.LogId;
            OnPropertyChanged("DataLog");
        }
        public void OnLogDataRowDoubleClick(object selectedRow)
        {
            LogInfo selectedLog = (LogInfo)selectedRow;
            _LogErrorDescriptor = selectedLog.Trace;
            OnPropertyChanged("LogErrorDescriptor");
        }
        private void physicalScreensCheck()
        {
            Screen[] scr = Screen.AllScreens;
            if (scr.Length < 3)
            {
                HelperLog.Instance.WriteWarning(MethodBase.GetCurrentMethod(), "No 3 screens detected, button Change View disabled");
                _threeScreensConnected = false;

            }
            else
            {
                HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "3 screens detected, button Change View enabled");
                _threeScreensConnected = true;
            }
        }
        private bool CheckWheelConnected()
        {
            var auxWheelDX = new SharpDXController();
            bool wheelNotConnected = auxWheelDX.checkWheelConnected();
            if (!wheelNotConnected)
            {
                _wheelNotConnectedVisibility = Visibility.Hidden;
            }
            else
            {
                _wheelNotConnectedVisibility = Visibility.Visible;
            }
            OnPropertyChanged("WheelNotConnectedVisibility");
            return wheelNotConnected;
        }
        private void ClientEventHandlerControllers()
        {
            ClientProxy.ImageUpdate += async (sender, args) => await ImageUpdateAsync(args.image, args.fps);
            ClientProxy.ImageUpdate2 += async (sender, args) => await ImageUpdateAsync2(args.image, args.fps);
            ClientProxy.ImageUpdate3 += async (sender, args) => await ImageUpdateAsync3(args.image, args.fps);
            ClientProxy.ImageUpdate4 += async (sender, args) => await ImageUpdateAsync4(args.image, args.fps);
            ClientProxy.ImageUpdate5 += async (sender, args) => await ImageUpdateAsync5(args.image, args.fps);

            ClientProxy.LatencyUpdate += async (sender, args) => await LatencyUpdateAsync(args.latency_counter_camera1,
                args.latency_counter_camera2, args.latency_counter_camera3, args.latency_counter_camera4, args.latency_counter_camera5,
                args.latency_gps, args.obstaclesMessageCounter, args.statusMessageCounter, args.trafficMessageCounter, args.worldModelMessageCounter,
                args.commandMessageCounter, args.lanesMessageCounter);
            ClientProxy.VHUpdate += async (sender, args) => await VehicleHardwareUpdateAsync(args.vehicleHardwareInfo);
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Driving Wheel Input Listener Init");
            BackgroundWorker worker = sender as BackgroundWorker;
            objWheelDX = new SharpDXController();

            //If not fake throw this
            backgroundToken = new CancellationTokenSource();
            Task.Run(() => objWheelDX.CaptureJoysticks(backgroundToken.Token, (info)=>
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                }
                else if (WheelEnable == false)
                {
                    info.Brake = _random.Next(100);
                    info.Button = _random.Next(5).ToString();
                    info.Throttle = _random.Next(100);
                    info.WheelTurn = _random.Next(100);
                }
                else
                {
                    info = objWheelDX.WheelData;
                    SwitchWheelButtonPressed(info.Button);
                }
                client.setWheelData(info);
            }));
        }
        private void SwitchWheelButtonPressed(string btnPressed)
        {
            string btnValue = btnPressed == "" ? "" : btnPressed.Substring(0, btnPressed.IndexOf(" "));
            var flag = false;
            switch (btnValue)
            {
                case "0":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_0_button, ref current_0_button, PRESSED_0, UNPRESSED_0);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "gear left pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: gear left");
                        ExecuteCommand("26");
                    }
                    break;
                case "1":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_1_button, ref current_1_button, PRESSED_1, UNPRESSED_1);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "gear right pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: gear right");
                        ExecuteCommand("25");
                    }
                    break;
                case "2":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_2_button, ref current_2_button, PRESSED_2, UNPRESSED_2);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "triangle pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: triangle");
                        //Central
                        if (!flagCamera) { ExecuteCommand("4"); } else { ExecuteCommand("5"); }
                    }
                    break;
                case "3":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_3_button, ref current_3_button, PRESSED_3, UNPRESSED_3);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "square pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: square");
                        //left
                        if (!flagCamera2) { ExecuteCommand("6"); } else { ExecuteCommand("8"); }
                    }
                    break;
                case "4":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_4_button, ref current_4_button, PRESSED_4, UNPRESSED_4);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "O pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: circle");
                        //right
                        if (!flagCamera3) { ExecuteCommand("7"); } else { ExecuteCommand("9"); }
                    }
                    break;
                case "5":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_5_button, ref current_5_button, PRESSED_5, UNPRESSED_5);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "X pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: X");
                        //back
                        if (!flagCamera4) { ExecuteCommand("11"); } else { ExecuteCommand("12"); }
                    }
                    break;
                case "7":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_7_button, ref current_7_button, PRESSED_7, UNPRESSED_7);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "ST pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: ST");
                        if (!flagCamera5) { ExecuteCommand("13"); } else { ExecuteCommand("14"); }
                    }
                    break;
                case "8":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_8_button, ref current_8_button, PRESSED_8, UNPRESSED_8);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "rear pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: R2");
                        ReverseDrivingStateChange();
                    }
                    break;
                case "9":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_9_button, ref current_9_button, PRESSED_9, UNPRESSED_9);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "L2 pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: L2");
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), $"onPanelClick: {onPanelClick}");
                        ExecuteCommand("27");
                        if (flagFirstControlPanel)
                        {
                            flagFirstControlPanel = false;
                            ExecuteCommand("27");
                        }
                    }
                    break;
                case "10":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_10_button, ref current_10_button, PRESSED_10, UNPRESSED_10);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "L3 pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: L3");
                        if (!flagLegendButtons)
                        {
                            _legendButtonVisibility = Visibility.Visible;
                        }
                        else
                        {
                            _legendButtonVisibility = Visibility.Hidden;
                        }
                        flagLegendButtons = !flagLegendButtons;
                        OnPropertyChanged("LegendButtonsVisibility");

                    }
                    break;
                case "11":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_11_button, ref current_11_button, PRESSED_11, UNPRESSED_11);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "R3 pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: R3");
                        if (!flagDisplayLogs) { ExecuteCommand("15"); } else { ExecuteCommand("16"); }
                        
                    }
                    break;
                case "12":
                    flag = CheckWheelButtonPressedUnpressed(btnPressed, ref last_12_button, ref current_12_button, PRESSED_12, UNPRESSED_12);
                    if (flag)
                    {
                        _txtButtonWheelPressed = "PS pressed";
                        HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Steering Wheel button pressed: PS");
                        ExecuteCommand("10");
                    }
                    break;
                default:
                    break;
            }
            if (flag)
            {
                OnPropertyChanged("txtButtonWheelPressed");
            }
        }
        private bool CheckWheelButtonPressedUnpressed(string btnPressed, ref string lastState, ref string currentState, string pressed, string unpressed)
        {
            bool flag = false;
            lastState = currentState;
            currentState = btnPressed;
            if (lastState.Equals(pressed) && currentState.Equals(unpressed))
            {
                flag = true;
            }
            return flag;
        }
        private void ExecuteCommand(string btnValue)
        {
            Application.Current.Dispatcher.Invoke(() => Button_Click(btnValue));
        }
        private void ReverseDrivingStateChange()
        {
            flagRearCamState = !flagRearCamState;
            if (flagRearCamState)
            {
                _reverseDrivingState = "reverse driving enabled";
                _reverseDrivingStateBckg = Colors.DarkGreen.ToString(); //"#2ECC71";
                _IconReverse = "../Images/RED_REVERSE.png";
                flagVisibilityBorderReverse = true;
                borderReverseVisibilityTimer = new System.Threading.Timer(_ => visibilityBorderReverse(), null, 0, 500);
            }
            else
            {
                borderReverseVisibilityTimer.Dispose();
                flagVisibilityBorderReverse = false;
                _reverseDrivingState = "reverse driving disabled";
                _reverseDrivingStateBckg = Colors.Firebrick.ToString(); //"#FFFFFF";
                _IconReverse = "../Images/GRAY_REVERSE.png";
                _ReverseBorderVisibility = Visibility.Hidden;
            }
            OnPropertyChanged("reverseDrivingState");
            OnPropertyChanged("reverseDrivingStateBckg");
            OnPropertyChanged("ReverseBorderVisibility");
            OnPropertyChanged("IconReverse");
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //ResultLabel = $"hola caracola!!!";
        }
        private int auxcounterloop = 0;
        private Queue<CamUpdate> CamUpdateBuffer = new Queue<CamUpdate>();
        private void WriteUpdateBuffer(byte[] img, int fps)
        {
            CamUpdateBuffer.Enqueue(new CamUpdate { image = img, fps = fps});
            _FPS4 = $"fps Proxy: {fps.ToString()}";
            OnPropertyChanged("FPS4");
            _FPS5 = $"CamUpdateBuffer.Count: {CamUpdateBuffer.Count}";
            OnPropertyChanged("FPS5");
        }
        private async Task TestImageUpdateAsyncBuffer()
        {
            while (true)
            {
                if (Application.Current is not null && CamUpdateBuffer.Count > 0)
                {
                    CamUpdate aux = CamUpdateBuffer.Dequeue();
                    if (aux.image != null)
                    {
                        await Task.Delay(5);
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            try
                            {
                                auxcounterloop++;
                                FPS1_Real_int++;
                                _Foto = imgConversionBitmap(aux.image);
                                OnPropertyChanged("Foto");
                                _FPS = auxcounterloop.ToString();
                                OnPropertyChanged("FPS");
                            }
                            catch (Exception ex)
                            {
                                HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                            }
                        }));
                    }
                }
            }
        }
        private Task ImageUpdateAsync(byte[] img, int fps)
        {
            if (Application.Current is not null && img is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        if (img?.Length > 0)
                        {
                            auxcounterloop++;
                            FPS1_Real_int++;
                            _Foto = imgConversionBitmap(img);
                            OnPropertyChanged("Foto");
                            _FPS = auxcounterloop.ToString();
                            OnPropertyChanged("FPS");
                        }
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task ImageUpdateAsync2(byte[] img, int fps)
        {
            if (Application.Current is not null && img is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        if (img?.Length > 0)
                        {
                            FPS2_Real_int++;
                            _Foto2 = imgConversionBitmap(img);
                            OnPropertyChanged("Foto2");
                            _FPS2 = fps.ToString();
                            OnPropertyChanged("FPS2");
                        }
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task ImageUpdateAsync3(byte[] img, int fps)
        {
            if (Application.Current is not null && img is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        FPS3_Real_int++;
                        _Foto3 = imgConversionBitmap(img);
                        OnPropertyChanged("Foto3");
                        _FPS3 = fps.ToString();
                        OnPropertyChanged("FPS3");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task ImageUpdateAsync4(byte[] img, int fps)
        {
            if (Application.Current is not null && img is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        FPS4_Real_int++;
                        _Foto4 = imgConversionBitmap(img);
                        OnPropertyChanged("Foto4");
                        _FPS4 = fps.ToString();
                        OnPropertyChanged("FPS4");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task ImageUpdateAsync5(byte[] img, int fps)
        {
            if (Application.Current is not null && img is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        FPS5_Real_int++;
                        _Foto5 = imgConversionBitmap(img);
                        OnPropertyChanged("Foto5");
                        _FPS5 = fps.ToString();
                        OnPropertyChanged("FPS5");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private void UpdateFPS(int cam)
        {
            switch (cam)
            {
                case 1:
                    _FPS_Real = FPS1_Real_int.ToString();
                    OnPropertyChanged("FPS_Real");
                    FPS1_Real_int = 0;
                    if (saveFramesReceived)
                    {
                        try
                        {
                            // This will get the current WORKING directory (i.e. \bin\Debug)
                            string workingDirectory = Environment.CurrentDirectory;
                            // This will get the current PROJECT directory
                            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.FullName;
                            HelperLog.Instance.WriteWarning(MethodBase.GetCurrentMethod(), $"PATH: {projectDirectory}");
                            // Save the bitmap into a file.
                            if (_Foto is not null)
                            {
                                using (FileStream stream =
                                new FileStream($"{projectDirectory}\\Frames_received\\img_cam1_{id_frame1}.png", FileMode.Create))
                                {
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(_Foto));
                                    encoder.Save(stream);
                                }
                            }
                            id_frame1++;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod()," CAM 1 ",e);
                            saveFramesReceived = false;
                        }
                    }
                    break;
                case 2:
                    _FPS2_Real = FPS2_Real_int.ToString();
                    OnPropertyChanged("FPS2_Real");
                    FPS2_Real_int = 0;
                    if (saveFramesReceived)
                    {
                        try
                        {
                            // This will get the current WORKING directory (i.e. \bin\Debug)
                            string workingDirectory = Environment.CurrentDirectory;
                            // This will get the current PROJECT directory
                            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.FullName;
                            HelperLog.Instance.WriteWarning(MethodBase.GetCurrentMethod(), $"PATH: {projectDirectory}");
                            // Save the bitmap into a file.
                            if (_Foto2 is not null)
                            {
                                using (FileStream stream =
                                new FileStream($"{projectDirectory}\\Frames_received\\img_cam2_{id_frame2}.png", FileMode.Create))
                                {
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(_Foto2));
                                    encoder.Save(stream);
                                }
                            }
                            id_frame2++;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), " CAM 2 ", e);
                            saveFramesReceived = false;
                        }
                    }
                    break;
                case 3:
                    _FPS3_Real = FPS3_Real_int.ToString();
                    OnPropertyChanged("FPS3_Real");
                    FPS3_Real_int = 0;
                    if (saveFramesReceived)
                    {
                        try
                        {
                            // This will get the current WORKING directory (i.e. \bin\Debug)
                            string workingDirectory = Environment.CurrentDirectory;
                            // This will get the current PROJECT directory
                            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.FullName;
                            HelperLog.Instance.WriteWarning(MethodBase.GetCurrentMethod(), $"PATH: {projectDirectory}");
                            // Save the bitmap into a file.
                            if (_Foto3 is not null)
                            {
                                using (FileStream stream =
                                new FileStream($"{projectDirectory}\\Frames_received\\img_cam3_{id_frame3}.png", FileMode.Create))
                                {
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(_Foto3));
                                    encoder.Save(stream);
                                }
                            }
                            id_frame3++;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), " CAM 3 ", e);
                            saveFramesReceived = false;
                        }
                    }
                    break;
                case 4:
                    _FPS4_Real = FPS4_Real_int.ToString();
                    OnPropertyChanged("FPS4_Real");
                    FPS4_Real_int = 0;
                    if (saveFramesReceived)
                    {
                        try
                        {
                            // This will get the current WORKING directory (i.e. \bin\Debug)
                            string workingDirectory = Environment.CurrentDirectory;
                            // This will get the current PROJECT directory
                            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.FullName;
                            HelperLog.Instance.WriteWarning(MethodBase.GetCurrentMethod(), $"PATH: {projectDirectory}");
                            // Save the bitmap into a file.
                            if (_Foto4 is not null)
                            {
                                using (FileStream stream =
                                new FileStream($"{projectDirectory}\\Frames_received\\img_cam4_{id_frame4}.png", FileMode.Create))
                                {
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(_Foto4));
                                    encoder.Save(stream);
                                }
                            }
                            id_frame4++;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), " CAM 4 ", e);
                            saveFramesReceived = false;
                        }
                    }
                    break;
                case 5:
                    _FPS5_Real = FPS5_Real_int.ToString();
                    OnPropertyChanged("FPS5_Real");
                    FPS5_Real_int = 0;
                    if (saveFramesReceived)
                    {
                        try
                        {
                            // This will get the current WORKING directory (i.e. \bin\Debug)
                            string workingDirectory = Environment.CurrentDirectory;
                            // This will get the current PROJECT directory
                            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.Parent.FullName;
                            HelperLog.Instance.WriteWarning(MethodBase.GetCurrentMethod(), $"PATH: {projectDirectory}");
                            // Save the bitmap into a file.
                            if (_Foto5 is not null)
                            {
                                using (FileStream stream =
                                new FileStream($"{projectDirectory}\\Frames_received\\img_cam5_{id_frame5}.png", FileMode.Create))
                                {
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(_Foto5));
                                    encoder.Save(stream);
                                }
                            }
                            id_frame5++;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), " CAM 5 ", e);
                            saveFramesReceived = false;
                        }
                    }
                    break;
            }
        }
        private Task LatencyUpdateAsync(int latency_cam1, int latency_cam2, int latency_cam3, int latency_cam4, int latency_cam5,
            int latency_gps, int obstaclesMessageCounter, int statusMessageCounter, int trafficMessageCounter, int worldModelMessageCounter,
            int commandMessageCounter, int lanesMessageCounter
            )
        {
            if (Application.Current is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        _LatencyCam1 = latency_cam1.ToString();
                        OnPropertyChanged("LatencyCam1");
                        _LatencyCam2 = latency_cam2.ToString();
                        OnPropertyChanged("LatencyCam2");
                        _LatencyCam3 = latency_cam3.ToString();
                        OnPropertyChanged("LatencyCam3");
                        _LatencyCam4 = latency_cam4.ToString();
                        OnPropertyChanged("LatencyCam4");
                        _LatencyCam5 = latency_cam5.ToString();
                        OnPropertyChanged("LatencyCam5");
                        _LatencyGPS = latency_gps.ToString();
                        OnPropertyChanged("LatencyGPS");
                        _CommandMessagesCounter = commandMessageCounter.ToString();
                        OnPropertyChanged("CommandMessagesCounter");
                        _LanesMessagesCounter = lanesMessageCounter.ToString();
                        OnPropertyChanged("LanesMessagesCounter");
                        _ObstaclesMessagesCounter = obstaclesMessageCounter.ToString();
                        OnPropertyChanged("ObstaclesMessagesCounter");
                        _StatusMessagesCounter = statusMessageCounter.ToString();
                        OnPropertyChanged("StatusMessagesCounter");
                        _TrafficMessagesCounter = trafficMessageCounter.ToString();
                        OnPropertyChanged("TrafficMessagesCounter");
                        _WorldModelMessagesCounter = worldModelMessageCounter.ToString();
                        OnPropertyChanged("WorldModelMessagesCounter");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task VehicleHardwareUpdateAsync(VehicleHardwareInfo vhi)
        {
            if (Application.Current is not null && vhi is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        newICUData.imu_accel_x = vhi.IcU.imu_accel_x;
                        newICUData.imu_accel_y = vhi.IcU.imu_accel_y;
                        newICUData.imu_accel_z = vhi.IcU.imu_accel_z;
                        newICUData.imu_gyro_x = vhi.IcU.imu_gyro_x;
                        newICUData.imu_gyro_y = vhi.IcU.imu_gyro_y;
                        newICUData.imu_gyro_z = vhi.IcU.imu_gyro_z;
                        newICUData.imu_mag_x = vhi.IcU.imu_mag_x;
                        newICUData.imu_mag_y = vhi.IcU.imu_mag_y;
                        newICUData.imu_mag_z = vhi.IcU.imu_mag_z;

                        _ValueBattery1 = vhi.BmsData.bms_soc + "%";//pcntg bateria

                        _Velocity = $"{(((vhi.InverterData.speed_rpm * 2 * 3.14) / 60) * 0.332):0.#}"; //Velocity

                        _AngleLeftWheel = -(vhi.StrsensData.steer_sensor_angle-127.5)*180/255 - _oldAngleLeftWheel;  //wheel angle
                        _AngleRightWheel = -(vhi.StrsensData.steer_sensor_angle - 127.5) * 180 / 255 - _oldAngleRightWheel;

                        OnPropertyChanged("ValueBattery1");
                        OnPropertyChanged("Velocity");
                        OnPropertyChanged("AngleLeftWheel");
                        OnPropertyChanged("AngleRightWheel");

                        if (vhi.BmsData.bms_battery_state == 4) //Control battery HV
                        {
                            _ColorBat1 = "#70D174";
                            _IconBattery1 = "../Images/Green_Battery1.png";
                            _IconHighVoltage = "../Images/HIGHVOLTAGE.png";
                        }
                        else
                        {
                            _ColorBat1 = "#FF5372";
                            _IconBattery1 = "../Images/Red_Battery1.png";
                            _IconHighVoltage = "../Images/RED_HIGHVOLTAGE.png";
                        }
                        if (flagBateriesAreOK) //Control LV, Nvidia and connection
                        {
                            _ColorBat2 = "#70D174";
                            _IconBattery2 = "../Images/Green_Battery2.png";
                            _IconLowVoltage = "../Images/LOWVOLTAGE.png";
                            _ColorBat3 = "#70D174";
                            _IconBattery3 = "../Images/Green_Battery3.png";
                            _IconWifi = "../Images/WIFI.png";
                        }
                        else
                        {
                            _ColorBat2 = "#FF5372";
                            _IconBattery2 = "../Images/Red_Battery2.png";
                            _IconLowVoltage = "../Images/RED_LOWVOLTAGE.png";
                            _ColorBat3 = "#FF5372";
                            _IconBattery3 = "../Images/Red_Battery3.png";
                            _IconWifi = "../Images/RED_WIFI.png";
                        }
                        OnPropertyChanged("ColorBat1");
                        OnPropertyChanged("ColorBat2");
                        OnPropertyChanged("ColorBat3");
                        OnPropertyChanged("IconBattery1");
                        OnPropertyChanged("IconBattery2");
                        OnPropertyChanged("IconBattery3");
                        OnPropertyChanged("IconLowVoltage");
                        OnPropertyChanged("IconWifi");
                        if (vhi.SftyData.hand_brake_pulled == false) //handbrake
                        {
                            _IconHandBrake = "../Images/HANDBRAKE.png";
                        }
                        else
                        {
                            _IconHandBrake = "../Images/RED_HANDBRAKE.png";
                        }
                        OnPropertyChanged("IconHandBrake");
                        if (vhi.SftyScstate.shutdown_emgcybuttonc_pushed || vhi.SftyScstate.shutdown_emgcybuttonl_pushed || vhi.SftyScstate.shutdown_emgcybuttonr_pushed) //seta
                        {
                            _IconError = "../Images/RED_ERROR.png";
                        }
                        else
                        {
                            _IconError = "../Images/ERROR.png";
                        }
                        OnPropertyChanged("IconError");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task LatencyUpdateAsyncCam2(int fps, CancellationToken token)
        {
            if (Application.Current is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        _LatencyCam2 = fps.ToString();
                        OnPropertyChanged("LatencyCam2");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task LatencyUpdateAsyncCam3(int fps, CancellationToken token)
        {
            if (Application.Current is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        _LatencyCam3 = fps.ToString();
                        OnPropertyChanged("LatencyCam3");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task LatencyUpdateAsyncCam4(int fps, CancellationToken token)
        {
            if (Application.Current is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        _LatencyCam4 = fps.ToString();
                        OnPropertyChanged("LatencyCam4");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        private Task LatencyUpdateAsyncCam5(int fps, CancellationToken token)
        {
            if (Application.Current is not null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        _LatencyCam5 = fps.ToString();
                        OnPropertyChanged("LatencyCam5");
                    }
                    catch (Exception ex)
                    {
                        HelperLog.Instance.WriteError(MethodBase.GetCurrentMethod(), "", ex);
                    }
                }));
            }
            return Task.CompletedTask;
        }
        public bool checkUserConnected()
        {
            //return client.checkUserConnected();
            return true;
        }
        public void ackUserConnected()
        {
            //client.ackUserConnected();
        }
        private void SwitchViewLayout()
        {
            if (viewState == (int)viewStates.compactLayout)
            {
                HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Opening Main Screen (triple view)");
                _MainScreen = new MainScreen();
                _MainScreen.Left = 1930;
                _MainScreen.Top = 0;
                _MainScreen.Closed += CloseSideScreens;
                _MainScreen.WindowStyle = WindowStyle.None;
                _MainScreen.Show();
                _MainScreen.ResizeMode = System.Windows.ResizeMode.NoResize;
                _MainScreen.DataContext = this;
                _MainScreen.WindowState = WindowState.Maximized;

                OpenSideScreens();
                HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(),$"_wheelNotConnectedVisibility: {_wheelNotConnectedVisibility}");

                foreach (Window item in Application.Current.Windows)
                {
                    if (item.Title == "WindowStartUI") item.Close();
                }

                viewState = (int)viewStates.tripleLayout;
            }
            else if (viewState == (int)viewStates.tripleLayout)
            {
                HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Opening Screen (compact view)");
                _WindowStartUI = new UserInterface.Window();
                if (_threeScreensConnected) _WindowStartUI.Left = 2080;
                else _WindowStartUI.Left = 160;
                _WindowStartUI.Top = 0;
                _WindowStartUI.Width = 1600;
                _WindowStartUI.Height = 840;
                _WindowStartUI.NavigationService.Navigate(new CompactLayout { DataContext = this });
                _WindowStartUI.Show();

                _RightScreen.Close();
                _LeftScreen.Close();

                foreach (System.Windows.Window item in Application.Current.Windows)
                {
                    if (item.Title == "MainScreen") item.Close();
                }
                viewState = (int)viewStates.compactLayout;
            }
        }
        private void OpenSideScreens()
        {
            HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Opening Side Screens (triple view)");
            Screen[] screens = Screen.AllScreens;
            _RightScreen = new RightScreen();
            _RightScreen.Left = 3800;
            _RightScreen.Top = 0;
            _RightScreen.WindowStyle = WindowStyle.None;
            _RightScreen.Show();
            _RightScreen.ResizeMode = System.Windows.ResizeMode.NoResize;
            _RightScreen.WindowState = WindowState.Maximized;
            _RightScreen.DataContext = this;

            _LeftScreen = new LeftScreen();
            _LeftScreen.Left = 0;
            _LeftScreen.Top = 0;
            _LeftScreen.WindowStyle = WindowStyle.None;
            _LeftScreen.Show();
            _LeftScreen.ResizeMode = System.Windows.ResizeMode.NoResize;
            _LeftScreen.WindowState = WindowState.Maximized;
            _LeftScreen.DataContext = this;
        }
        private void CloseSideScreens(object sender, EventArgs e)
        {
            foreach (System.Windows.Window item in Application.Current.Windows)
            {
                if (item.Title == "RightScreen" || item.Title == "LeftScreen") item.Close();
            }
        }
        private void releaseServerViewModel(object sender, EventArgs e)
        {
            HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Shutdown started -- Disposing unreleased services:");
            if (backgroundWorker1.IsBusy == true)
            {
                HelperLog.Instance.WriteInfo(MethodBase.GetCurrentMethod(), "Disposing Steering Wheel listener async task");
                // Stop the asynchronous operation.
                backgroundWorker1.CancelAsync();
                backgroundToken.Cancel();
            }
            if (flagWheel)
            {
                Task t = Task.Run(() => client.DisposeAsyncWheel());
                t.Wait();
                Thread.Sleep(releaseServerVMDelay);
            }
            if (flagCamera)
            {
                ClientProxy.DisposeAsyncCamera();
                FPS1Timer.Dispose();
            }
            if (flagCamera2)
            {
                ClientProxy.DisposeAsyncCamera2();
                FPS2Timer.Dispose();
            }
            if (flagCamera3)
            {
                ClientProxy.DisposeAsyncCamera3();
                FPS3Timer.Dispose();
            }
            if (flagCamera4)
            {
                ClientProxy.DisposeAsyncCamera4();
                FPS4Timer.Dispose();
            }
            if (flagCamera5)
            {
                ClientProxy.DisposeAsyncCamera5();
                FPS5Timer.Dispose();
            }
            if (flagLatency)
            {
                client.DisposeAsyncLatencyCams();
            }
            if (getLogsFromServerBool)
            {
                getLogsFromServerTimer.Dispose();
            }
            if (flagRearCamState)
            {
                borderReverseVisibilityTimer.Dispose();
            }
            if (flagGPS)
            {
                ClientProxy.DisposeAsyncGPS();
            }
            if (flagVH)
            {
                ClientProxy.DisposeAsyncVHStatus();
            }
            if (ros2csInClient)
            {
                ROS2Listener.Instance.KillROS2();
            }
            //Task.Delay(100);
            //Thread.Sleep(100);
        }

        public void SendRoute(List<Location> route)
        {
            List<LocationData> route_modified = route.Select(x => new LocationData { latitude = x.Latitude, longitude = x.Longitude }).ToList();
            client.setRouteInfo(route_modified);
            _ = ClientProxy.connectRouteService();
        }
        public void ActivateGpsService()
        {
            _ = ClientProxy.connectGPSService();
            flagGPS = true;
        }
        public void ActivateVehicleHardware()
        {
            _ = ClientProxy.connectVehicleHardwareService();
            flagVH = true;
        }

        #region button control
        private Task Button_Click(string type)
        {
            switch (type)
            {
                case "1":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Capture Wheel pressed");
                    WheelEnable = true;
                    flagWheel = true;
                    if (backgroundWorker1.IsBusy != true)
                    {
                        // Start the asynchronous operation.
                        backgroundWorker1.RunWorkerAsync();
                        _ = ClientProxy.connectWheelService();
                    }
                    else
                    {
                        objWheelDX.reAcquire = true;
                        info.Brake = 0;
                        info.Throttle = 0;
                        info.WheelTurn = 0;
                        info.Button = "";
                    }
                    ButtonCaptureWheel.SetCanExecute(false);
                    ButtonCaptureRandom.SetCanExecute(true);
                    ButtonStopCapture.SetCanExecute(true);
                    break;
                case "2":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Capture Random pressed");
                    WheelEnable = false;
                    flagWheel = true;
                    if (backgroundWorker1.IsBusy != true)
                    {
                        // Start the asynchronous operation.
                        backgroundWorker1.RunWorkerAsync();
                        _ = ClientProxy.connectWheelService();
                    }

                    ButtonCaptureRandom.SetCanExecute(false);
                    ButtonCaptureWheel.SetCanExecute(true);
                    ButtonStopCapture.SetCanExecute(true);
                    break;
                case "3":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Wheel pressed");
                    if (backgroundWorker1.IsBusy == true)
                    {
                        // Stop the asynchronous operation.
                        backgroundWorker1.CancelAsync();
                        backgroundToken.Cancel();
                    }
                    WheelEnable = false;
                    flagWheel = false;

                    ButtonStopCapture.SetCanExecute(false);
                    ButtonCaptureWheel.SetCanExecute(true);
                    ButtonCaptureRandom.SetCanExecute(true);

                    ValueTask t = client.DisposeAsyncWheel();
                    break;
                case "4":
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Capture Camera pressed");
                        flagCamera = true;
                        ClientProxy.connectCameraService();
                        FPS1Timer = new System.Threading.Timer(_ => UpdateFPS(1), null, 0, 1000);
                        ButtonCaptureCamera.SetCanExecute(false);
                        ButtonStopCamera.SetCanExecute(true);
                    }
                    break;
                case "5":
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Camera pressed");
                        flagCamera = false;
                        ClientProxy.DisposeAsyncCamera();
                        ButtonStopCamera.SetCanExecute(false);
                        ButtonCaptureCamera.SetCanExecute(true);
                        FPS1Timer.Dispose();
                    }
                    break;
                case "6":
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Capture Cam 2 pressed");
                        flagCamera2 = true;
                        ClientProxy.connectCamera2Service();
                        FPS2Timer = new System.Threading.Timer(_ => UpdateFPS(2), null, 0, 1000);
                        ButtonCaptureCam2.SetCanExecute(false);
                        ButtonStopCam2.SetCanExecute(true);
                    }
                    break;
                case "7":
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Capture Cam 3 pressed");
                        flagCamera3 = true;
                        ClientProxy.connectCamera3Service();
                        FPS3Timer = new System.Threading.Timer(_ => UpdateFPS(3), null, 0, 1000);
                        ButtonCaptureCam3.SetCanExecute(false);
                        ButtonStopCam3.SetCanExecute(true);
                    }
                    break;
                case "8":
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Cam 2 pressed");
                        flagCamera2 = false;
                        ClientProxy.DisposeAsyncCamera2();
                        ButtonStopCam2.SetCanExecute(false);
                        ButtonCaptureCam2.SetCanExecute(true);
                        FPS2Timer.Dispose();
                    }
                    break;
                case "9":
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Cam 3 pressed");
                        flagCamera3 = false;
                        ClientProxy.DisposeAsyncCamera3();
                        ButtonStopCam3.SetCanExecute(false);
                        ButtonCaptureCam3.SetCanExecute(true);
                        FPS3Timer.Dispose();
                    }
                    break;
                case "10":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Change View pressed");
                    SwitchViewLayout();
                    break;
                case "11":
                    //REARCAMVISIBILITY TRUE
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Capture Cam 4 pressed");
                        flagCamera4 = true;
                        ClientProxy.connectCamera4Service();
                        FPS4Timer = new System.Threading.Timer(_ => UpdateFPS(4), null, 0, 1000);
                        ButtonCaptureCam4.SetCanExecute(false);
                        ButtonStopCam4.SetCanExecute(true);
                        _rearCamVisibility = Visibility.Visible;
                        _rearIconVisibility = Visibility.Hidden;
                        OnPropertyChanged("RearCamVisibility");
                        OnPropertyChanged("RearIconVisibility");
                    }
                    break;
                case "12":
                    //REARCAMVISIBILITY TRUE
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Cam 4 pressed");
                        flagCamera4 = false;
                        ClientProxy.DisposeAsyncCamera4();
                        ButtonStopCam4.SetCanExecute(false);
                        ButtonCaptureCam4.SetCanExecute(true);
                        _rearCamVisibility = Visibility.Hidden;
                        _rearIconVisibility = Visibility.Visible;
                        OnPropertyChanged("RearCamVisibility");
                        OnPropertyChanged("RearIconVisibility");

                        FPS1_Real_int = 0;
                        FPS4Timer.Dispose();
                    }
                    break;
                case "13":
                    if (!ros2csInClient)
                    {
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Capture Cam 5 pressed");
                        flagCamera5 = true;
                        ClientProxy.connectCamera5Service();
                        FPS5Timer = new System.Threading.Timer(_ => UpdateFPS(5), null, 0, 1000);
                        ButtonCaptureCam5.SetCanExecute(false);
                        ButtonStopCam5.SetCanExecute(true);
                        _onBoardCamVisibility = Visibility.Visible;
                        _boardIconVisibility = Visibility.Hidden;
                        OnPropertyChanged("OnBoardCamVisibility");
                        OnPropertyChanged("BoardIconVisibility");
                    }
                    break;
                case "14":
                    if (!ros2csInClient)
                    {
                        //ON BOARD CAM
                        HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Cam 5 pressed");
                        flagCamera5 = false;
                        ClientProxy.DisposeAsyncCamera5();
                        ButtonStopCam5.SetCanExecute(false);
                        ButtonCaptureCam5.SetCanExecute(true);
                        _onBoardCamVisibility = Visibility.Hidden;
                        _boardIconVisibility = Visibility.Visible;
                        OnPropertyChanged("OnBoardCamVisibility");
                        OnPropertyChanged("BoardIconVisibility");
                        FPS5Timer.Dispose();
                    }
                    break;
                case "15":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Show Log List pressed");
                    flagDisplayLogs = true;
                    _logVisibility = Visibility.Visible;
                    _logsIconVisibility = Visibility.Hidden;
                    ButtonLogListShow.SetCanExecute(false);
                    ButtonLogListHide.SetCanExecute(true);
                    OnPropertyChanged("LogVisibility");
                    OnPropertyChanged("LogsIconVisibility");
                    break;
                case "16":
                    //LOG VISIBILITY
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Hide Log List pressed");
                    flagDisplayLogs = false;
                    _logVisibility = Visibility.Hidden;
                    _logsIconVisibility = Visibility.Visible;
                    ButtonLogListShow.SetCanExecute(true);
                    ButtonLogListHide.SetCanExecute(false);
                    OnPropertyChanged("LogVisibility");
                    OnPropertyChanged("LogsIconVisibility");
                    break;
                case "17":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Show Errors pressed");
                    _LogsShowErrorsChecked = !_LogsShowErrorsChecked;
                    PrintDataLog(0);
                    printDataLogTimer.Dispose();
                    printDataLogTimer = new System.Threading.Timer(_ => PrintDataLog(1), null, 0, 2000);
                    break;
                case "18":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Show Warnings pressed");
                    _LogsShowWarningsChecked = !_LogsShowWarningsChecked;
                    PrintDataLog(0);
                    printDataLogTimer.Dispose();
                    printDataLogTimer = new System.Threading.Timer(_ => PrintDataLog(1), null, 0, 2000);
                    break;
                case "19":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Show Debug pressed");
                    _LogsShowDebugChecked = !_LogsShowDebugChecked;
                    PrintDataLog(0);
                    printDataLogTimer.Dispose();
                    printDataLogTimer = new System.Threading.Timer(_ => PrintDataLog(1), null, 0, 2000);
                    break;
                case "20":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Show Info pressed");
                    _LogsShowInfoChecked = !_LogsShowInfoChecked;
                    PrintDataLog(0);
                    printDataLogTimer.Dispose();
                    printDataLogTimer = new System.Threading.Timer(_ => PrintDataLog(1), null, 0, 2000);
                    break;
                case "21":
                    DataLog.GoToPreviousPage();
                    break;
                case "22":
                    DataLog.GoToNextPage();
                    break;
                case "23":
                    if (_LogsTextSearch != "")
                    {
                        printDataLogTimer.Dispose();
                        _ButtonStartStopLogsText = "Start Logs";
                        _stopLogsStateBckg = "#FF808080";
                        _LogsShowDebugEnabled = false;
                        _LogsShowErrorsEnabled = false;
                        _LogsShowInfoEnabled = false;
                        _LogsShowWarningsEnabled = false;
                        OnPropertyChanged("ButtonStartStopLogsText");
                        OnPropertyChanged("stopLogsStateBckg");
                        OnPropertyChanged("LogsShowDebugEnabled");
                        OnPropertyChanged("LogsShowErrorsEnabled");
                        OnPropertyChanged("LogsShowInfoEnabled");
                        OnPropertyChanged("LogsShowWarningsEnabled");
                        _LogsStartStopLogs = false;
                        PrintDataLogSearch(_LogsTextSearch);
                        _LogsTextSearch = "";
                        OnPropertyChanged("LogsTextSearch");
                    }
                    break;
                case "24":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Logs pressed");
                    if (_LogsStartStopLogs)
                    {
                        printDataLogTimer.Dispose();
                        _ButtonStartStopLogsText = "Start Logs";
                        _stopLogsStateBckg = "#FF808080";
                        _LogsShowDebugEnabled = false;
                        _LogsShowErrorsEnabled = false;
                        _LogsShowInfoEnabled = false;
                        _LogsShowWarningsEnabled = false;

                    }
                    else
                    {
                        PrintDataLog(0);
                        printDataLogTimer = new System.Threading.Timer(_ => PrintDataLog(1), null, 0, 2000);
                        _ButtonStartStopLogsText = "Stop Logs";

                        _stopLogsStateBckg = "#FFD3D3D3";

                        _LogsShowDebugEnabled = true;
                        _LogsShowErrorsEnabled = true;
                        _LogsShowInfoEnabled = true;
                        _LogsShowWarningsEnabled = true;
                    }
                    OnPropertyChanged("ButtonStartStopLogsText");
                    OnPropertyChanged("stopLogsStateBckg");
                    OnPropertyChanged("LogsShowDebugEnabled");
                    OnPropertyChanged("LogsShowErrorsEnabled");
                    OnPropertyChanged("LogsShowInfoEnabled");
                    OnPropertyChanged("LogsShowWarningsEnabled");
                    _LogsStartStopLogs = !_LogsStartStopLogs;
                    break;
                case "25":
                    if (_mapIconVisibility == Visibility.Visible)
                    {
                        //PROVISIONAL MAP HIDDEN/VISIBILITY
                        _mapIconVisibility = Visibility.Hidden;
                        _mapViewVisibility = Visibility.Visible;
                        OnPropertyChanged("MapViewVisibility");
                        OnPropertyChanged("MapIconVisibility");
                        if (_isDebugMode == false)
                        {
                            ClientProxy.DisposeAsyncGPS();
                            flagGPS = false;
                        }
                    }
                    else
                    {
                        //PROVISIONAL MAP HIDDEN/VISIBILITY
                        _mapIconVisibility = Visibility.Visible;
                        _mapViewVisibility = Visibility.Hidden;
                        OnPropertyChanged("MapViewVisibility");
                        OnPropertyChanged("MapIconVisibility");
                        if (_isDebugMode == false)
                        {
                            ClientProxy.connectGPSService();
                            flagGPS = true;
                        }
                    }
                    break;
                case "26":
                    if (_carIconVisibility == Visibility.Visible)
                    {
                        //PROVISIONAL CAR HIDDEN/VISIBILITY
                        _carIconVisibility = Visibility.Hidden;
                        _carViewVisibility = Visibility.Visible;
                        OnPropertyChanged("CarIconVisibility");
                        OnPropertyChanged("CarViewVisibility");
                    }
                    else
                    {
                        //PROVISIONAL CAR HIDDEN/VISIBILITY
                        _carIconVisibility = Visibility.Visible;
                        _carViewVisibility = Visibility.Hidden;
                        OnPropertyChanged("CarIconVisibility");
                        OnPropertyChanged("CarViewVisibility");
                    }
                    break;
                case "27":

                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), $"onPanelClick: {onPanelClick}");
                    if (canChange == true)
                    {
                        onPanelClick = !onPanelClick;
                        if (onPanelClick == true)
                        {
                            _XTransformPanel = "148";
                            _YTransformPanel = "0";
                            OnPropertyChanged("XTransformPanel");
                            OnPropertyChanged("YTransformPanel");
                            _FromTransform2 = "0";
                            _ToTransform2 = "-148";
                            OnPropertyChanged("FromTransform2");
                            OnPropertyChanged("ToTransform2");
                        }
                        else
                        {
                            _XTransformPanel = "0";
                            _YTransformPanel = "148";
                            OnPropertyChanged("XTransformPanel");
                            OnPropertyChanged("YTransformPanel");
                            _FromTransform2 = "-148";
                            _ToTransform2 = "0";
                            OnPropertyChanged("FromTransform2");
                            OnPropertyChanged("ToTransform2");
                        }
                    }
                    OnPanelVisible();
                    break;
                case "28":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Start Latency pressed");
                    flagLatency = true;
                    ClientProxy.connectLatencyService();
                    ButtonCaptureLatency.SetCanExecute(false);
                    ButtonStopLatency.SetCanExecute(true);
                    break;
                case "29":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Stop Latency pressed");
                    flagLatency = false;
                    client.DisposeAsyncLatencyCams();
                    ButtonCaptureLatency.SetCanExecute(true);
                    ButtonStopLatency.SetCanExecute(false);
                    break;
                case "30":
                    HelperLog.Instance.WriteDebug(MethodBase.GetCurrentMethod(), "Button Save Frames pressed");
                    saveFramesReceived = !saveFramesReceived;
                    break;
                default:
                    break;
            }
            return Task.CompletedTask;
        }

        #endregion

        #region property changed
        public event PropertyChangedEventHandler PropertyChanged;

		//pattern by which a property changed in this script, mapped inside the xaml, automatically its UI content is updated
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
