using PlcService;
using Prism.Commands;
using Prism.Mvvm;
using Sharp7;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Trace;

namespace SimpleHmi.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        public string IpAddress
        {
            get { return _ipAddress; }
            set { SetProperty(ref _ipAddress, value); }
        }
        private string _ipAddress;
        public string StartReg
        {
            get { return _startReg; }
            set { SetProperty(ref _startReg, value); }
        }
        private string _startReg;
        public string AreaLength
        {
            get { return _areaLength; }
            set { SetProperty(ref _areaLength, value); }
        }
        private string _areaLength;
        public string MReg
        {
            get { return _mReg; }
            set { SetProperty(ref _mReg, value); }
        }
        private string _mReg;

        public string ValueReaded
        {
            get { return _valueReaded; }
            set { SetProperty(ref _valueReaded, value); }
        }
        private string _valueReaded;

        public bool HighLimit
        {
            get { return _highLimit; }
            set { SetProperty(ref _highLimit, value); }
        }
        private bool _highLimit;

        public bool LowLimit
        {
            get { return _lowLimit; }
            set { SetProperty(ref _lowLimit, value); }
        }
        private bool _lowLimit;

        public bool PumpState
        {
            get { return _pumpState; }
            set { SetProperty(ref _pumpState, value); }
        }
        private bool _pumpState;

        public int TankLevel
        {
            get { return _tankLevel; }
            set { SetProperty(ref _tankLevel, value); }
        }
        private int _tankLevel;

        public ConnectionStates ConnectionState
        {
            get { return _connectionState; }
            set { SetProperty(ref _connectionState, value); }
        }
        private ConnectionStates _connectionState;

        public TimeSpan ScanTime
        {
            get { return _scanTime; }
            set { SetProperty(ref _scanTime, value); }
        }
        private TimeSpan _scanTime;

        public ICommand ConnectCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand StartCommand { get; private set; }

        public ICommand StopCommand { get; private set; }

        public ICommand ReadRegMCommand { get; private set; }

        //#define USAS7
#if USAS7
        PlcService.S7PlcService _plcService;
#else
        PlcService.ModbusService _plcService;
#endif

        public MainWindowViewModel()
        {
#if USAS7
            _plcService = new PlcService();
#else
            _plcService = new PlcService.ModbusService();
#endif
            ConnectCommand = new DelegateCommand(Connect);
            DisconnectCommand = new DelegateCommand(Disconnect);
            StartCommand = new DelegateCommand(async () => { await Start(); });
            StopCommand = new DelegateCommand(async () => { await Stop(); });
            ReadRegMCommand = new DelegateCommand(async () => { await ReadRegM(); });

            IpAddress = "10.10.10.11"; 
            StartReg = "430"; 
            AreaLength = "50";

            OnPlcServiceValuesRefreshed(null, null);
            _plcService.ValuesRefreshed += OnPlcServiceValuesRefreshed;
        }

        private void OnPlcServiceValuesRefreshed(object sender, EventArgs e)
        {
            ConnectionState = _plcService.ConnectionState;
            ValueReaded = _plcService.ValueReaded;
        }

        private async void Connect()
        {
            await _plcService.ConnectAsync(IpAddress, 501);
        }

        private void Disconnect()
        {
            _plcService.Disconnect();
        }

        private async Task Start()
        {
            int Reg;
            int.TryParse(StartReg, out Reg);
            int Len;
            int.TryParse(AreaLength, out Len);
            TraceDbg.TraceON = true;
            TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t {IpAddress}-{Reg}:{Len} ");
            _plcService.StartTest(IpAddress);

        }

        private async Task Stop()
        {
            TraceDbg.TraceON = true;
            TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\tRichiesta stop ");
            _plcService.StopTest();
        }

        private async Task ReadRegM()
        {
            int Reg;
            int.TryParse(MReg, out Reg);
            string TAG_TO_READ = $"REG{Reg}.LEN{1}";
            _plcService.GetVarArea(IpAddress);
        }



    }
}
