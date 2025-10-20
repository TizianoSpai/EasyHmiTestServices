using System.Net.Sockets;
using Modbus.Device;
using Trace;


namespace PlcService
{

  public class ModbusService
  {
    private ModbusIpMaster? _client;
    private /*readonly*/ TcpClient clientTcp;
    //private readonly DataProvider _dataProv;
    private readonly CancellationTokenSource _cts = new();
    public int TestReg { get; set; }
    public int TestLen { get; set; }
    public string? ValueReaded { get; set; }

    private volatile object _locker = new object();
    // Modifica la dichiarazione dell'evento ValuesRefreshed per renderlo nullable
    public event EventHandler? ValuesRefreshed;
    public ConnectionStates ConnectionState { get; private set; }
    //public bool ReqStop { get; private set; }
    public int MultiReadLoopState { get; private set; }

    public ModbusService(/*MainViewModel viewModel*/)
    {
      clientTcp = new TcpClient();
      _client = null;
      MultiReadLoopState = 0; // 0=Ready; 1=ReqStopOn; 2=InLoop
    }

    public async Task<bool> ConnectAsync(string ip, int port)
    {
      TraceDbg.TraceON = true;
      port = 502;

      try
      {
        // ensure we have a fresh, usable TcpClient
        if(clientTcp == null || clientTcp.Client == null)
          clientTcp = new TcpClient();

        if(!clientTcp.Connected)
        {
          TraceDbg.TRACE($"{DateTime.Now:HH:mm:ss}\t start {ip}:{port}\n");
          await clientTcp.ConnectAsync(ip, port);
          TraceDbg.TRACE($"{DateTime.Now:HH:mm:ss}\t connected \n");

          _client = ModbusIpMaster.CreateIp(clientTcp);
          TraceDbg.TRACE($"{DateTime.Now:HH:mm:ss}\t _client {_client}\n");
          ValueReaded = _client?.ToString();
        }
      }
      catch(Exception ex)
      {
        TraceDbg.TRACE($"{DateTime.Now:HH:mm:ss}\t Error: {ex.Message}\n");
        return false;
      }
      ConnectionState = ConnectionStates.Online;
      return true;
    }

    public bool Disconnect()
    {
      if(MultiReadLoopState != 0)
        return false;

      //_cts.Cancel();

      if(_client != null)
      {
        _client.Dispose();
        _client = null;
        ConnectionState = ConnectionStates.Offline;
      }
      return true;
    }

    /// <summary>
    /// Read all bytes of a db for a specified length. Es.: DB1.DBL256 read 256 bytes in db 1
    /// </summary>
    /// <param name="address">Es.: REG1.LEN256 read 256 bytes in db 1</param>
    /// <param name="pars">Es.: IdArea+"-"+Lunghezza</param>
    /// <returns> byte [] </returns> return the readed buffer
    public bool[] GetCoilsVarArea(string pars)
    {
      var strings = pars.Split('-');
      int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
      int len = Convert.ToInt32(strings[1].Replace("LEN", ""));
      bool[] res = new bool[len];
      try
      {
        if(ConnectionState == ConnectionStates.Offline)
        {
          ValueReaded = "Not Connected !";
          OnValuesRefreshed();
          return res;
        }

        ValueReaded = "";
        res = GetCoils((uint)reg, (uint)len);
        for(int i = 0; i < len; i++)
          ValueReaded += (res[i] ? "true" : "false") + " ";
      }
      catch(Exception ex)
      {
        TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error {ex.Message}\n");
        ValueReaded = "Errore";
      }
      OnValuesRefreshed();
      return res;
    }

    private bool[] GetCoils(uint reg, uint length)
    {
      TraceDbg.TraceON = true;
      lock(_locker)
      {
        TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read reg:M {reg}-{length} \n");
        bool[] buffer = new bool[length];
        try
        {
          if(_client != null)
          {
            buffer = _client.ReadCoils((ushort)reg, (ushort)length);
          }
          else
            buffer[0] = false;
        }
        catch(Exception ex)
        {
          TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error {ex.Message}\n");
          throw ex;
        }

        return buffer;
      }
    }

    public ushort[] GetHoldingRegsVarArea(string par)
    {
      var strings = par.Split('-');
      int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
      int len = Convert.ToInt32(strings[1].Replace("LEN", ""));
      ushort[] res = null;
      try
      {
        if(ConnectionState == ConnectionStates.Offline)
        {
          ValueReaded = "Not Connected !";
          OnValuesRefreshed();
          return res;
        }

        ValueReaded = "";
        res = GetHoldingRegs((uint)reg, (uint)len);
        for(int i = 0; i<len; i++)
          ValueReaded = ValueReaded + res[i].ToString() + " ";
      }
      catch(Exception ex)
      {
        TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error {ex.Message}\n");
        ValueReaded = "Errore";
      }
      OnValuesRefreshed();
      return res;
    }

    private ushort[] GetHoldingRegs(uint reg, uint length)
    {
      TraceDbg.TraceON = true;
      lock(_locker)
      {
        TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read reg:M {reg}-{length} \n");
        ushort[] buffer = new ushort[length];
        try
        {
          if(_client != null)
          {
            buffer = _client.ReadHoldingRegisters((ushort)reg, (ushort)length);
          }
          else
            buffer[0] = 0;
        }
        catch(Exception ex)
        {
          TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error {ex.Message}\n");
          throw ex;
        }

        return buffer;
      }
    }

    public async Task StartPollingAsync(string ip, int port, CancellationToken token)
    {
      if(MultiReadLoopState != 0)
        return;
      MultiReadLoopState = 2;

      //while(!token.IsCancellationRequested)
      while(MultiReadLoopState == 2)
      {
        try
        {
          TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"ReadHoldingRegisters 430 \n");
          ushort[] gruppoA = _client.ReadHoldingRegisters(430, 50);
          TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"ReadHoldingRegisters 330 \n");
          ushort[] gruppoB = _client.ReadHoldingRegisters(330, 1);
          ValueReaded = "";
          foreach(ushort value in gruppoA)
          {
            ValueReaded += (char)(value & 0xff);
            ValueReaded += (char)(value >> 8 & 0xff);
          }
          TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"fine conversione \n");

          //_viewModel.DispatcherService.Invoke(() =>
          //{
          //    _viewModel.GruppoA = gruppoA;
          //    _viewModel.GruppoB = gruppoB;
          //});
          OnValuesRefreshed();
        }
        catch(Exception ex) when(!(ex is OperationCanceledException))
        {
          // Gestione errori, log o retry
        }

        try
        {
          await Task.Delay(50, token);
        }
        catch(OperationCanceledException)
        {
          // expected on cancel — break out to finish gracefully
          break;
        }
      }
      MultiReadLoopState = 0;
    }


    public async void StartTest(string ip)
    {
      if(ConnectionState == ConnectionStates.Online)
        await StartPollingAsync(ip, 0, _cts.Token);
    }


    public void StopTest()
    {
      //_cts.Cancel();

      if(ConnectionState == ConnectionStates.Online)
        MultiReadLoopState = 1;
    }

    private void OnValuesRefreshed()
    {
      ValuesRefreshed?.Invoke(this, new EventArgs());
    }
  }
}

