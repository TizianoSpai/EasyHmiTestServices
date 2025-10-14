using Modbus.Device;  // Namespace NSModbus4
using Sharp7;
using Snet.Model.data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Timers;
using Trace;
using XNetClient;


namespace PlcService
{

    public class ModbusService
    {
        private ModbusIpMaster? _client;
        private readonly TcpClient clientTcp;
        //private readonly DataProvider _dataProv;
        private readonly CancellationTokenSource _cts = new();
        public int TestReg { get; set; }
        public int TestLen { get; set; }
        public string? ValueReaded { get; set; }

        private volatile object _locker = new object();
        public event EventHandler ValuesRefreshed;
        public ConnectionStates ConnectionState { get; private set; }

        public ModbusService(/*MainViewModel viewModel*/)
        {
            clientTcp = new TcpClient();
            _client = null;
        }
        public async Task<bool> ConnectAsync(string ip, int port)
        {
            TraceDbg.TraceON = true;
            port = 502;
            try
            {
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t start {ip}:{port}\n");
                await clientTcp.ConnectAsync(ip, port);
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t connected \n");
                _client = ModbusIpMaster.CreateIp(clientTcp);
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t _client {_client}\n");
                ValueReaded = _client.ToString();
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t ValueReaded {ValueReaded}\n");
            }
            catch (Exception ex)
            {
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t Error: {ex.Message}\n");
            }
            return true;
        }
        public bool Disconnect()
        {
            _cts.Cancel();
            return true;
        }

        /// <summary>
        /// Read all bytes of a db for a specified length. Es.: DB1.DBL256 read 256 bytes in db 1
        /// </summary>
        /// <param name="address">Es.: REG1.LEN256 read 256 bytes in db 1</param>
        /// <returns> byte [] </returns> return the readed buffer
        public bool[] GetVarArea(string address)
        {
            var strings = address.Split('.');
            int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
            int len = Convert.ToInt32(strings[1].Replace("LEN", ""));
            bool[] res = null;
            try
            {
                res = GetVarArea((uint)reg, (uint)len);
                ValueReaded = res[0] ? "true" : "false";

            }
            catch (Exception ex)
            {
                TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error {ex.Message}\n");
                ValueReaded = "Errore";
            }
            OnValuesRefreshed();
            return res;
        }
        private bool[] GetVarArea(uint reg, uint length)
        {
            TraceDbg.TraceON = true;
            lock (_locker)
            {
                TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read reg:M {reg}-{length} \n");
                bool[] buffer = new bool[length];
                try
                {
                    if (_client != null)
                        buffer = _client.ReadCoils((ushort)reg, (ushort)length);
                    else
                        buffer[0] = false;
                }
                catch (Exception ex)
                {
                    TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error {ex.Message}\n");
                    throw ex;
                }

                return buffer;
            }
        }

        public async Task StartPollingAsync(string ip, int port, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"ReadHoldingRegisters 430 \n");
                    ushort[] gruppoA = _client.ReadHoldingRegisters(430, 50);
                    TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"ReadHoldingRegisters 330 \n");
                    ushort[] gruppoB = _client.ReadHoldingRegisters(330, 1);
                    ValueReaded = "";
                    foreach (ushort value in gruppoA)
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
                catch
                {
                    // Gestione errori, log o retry
                }

                await Task.Delay(50, token);
            }
        }


        public async void StartTest(string ip)
        {
            await StartPollingAsync(ip, 0, _cts.Token);
        }


        public void StopTest()
        {
            _cts.Cancel();
        }

        private void OnValuesRefreshed()
        {
            ValuesRefreshed?.Invoke(this, new EventArgs());
        }
    }
}

