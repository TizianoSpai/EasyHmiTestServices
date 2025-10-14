#define SIM
using DataAccessShared;
using Microsoft.Extensions.Logging.Abstractions;
using Sharp7;
using Snet.Model.data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Trace;
using XNetClient;

namespace PlcService
{
    public class XNetService
    {
        private readonly XNet _client;
        private DateTime _lastScanTime;
        private readonly CancellationTokenSource _cts = new();
        public int TestReg { get; set; }
        public int TestLen { get; set; }
        public string ValueReaded { get; set; }

        public bool StatoRun { get; set; }
        public bool StatoPause { get; set; }
        public bool StatoStop { get; set; }
        public bool StatoAllarme { get; set; }

        private volatile object _locker = new object();

        public XNetService()
        {
            TraceDbg.TraceON = true;
            //#if NOSIM
            TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\tAttiva Service");
            try
            {
                _client = new XNet();
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\tConfermata attivazione");
            }
            catch (Exception ex)
            {
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + "\t Connection error: " + ex.Message);
            }
            //#endif
        }

        public event EventHandler ValuesRefreshed;

        public ConnectionStates ConnectionState { get; private set; }

        public ushort[] StateArea { get; set; } = new ushort[256];
        public ushort[] VarArea { get; set; } = new ushort[256];
        public ushort[] AllArea { get; set; } = new ushort[8096];
        private int MaccNr { get; set; }


        public void Connect(string ipAddress)
        {
            int result = 0;
            try
            {
                ConnectionState = ConnectionStates.Connecting;
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\tConnect => IP:{ipAddress}");
                //#if NOSIM
                if (_client != null)
                    _client.AdderLink(ipAddress);
                //#endif
                if (result == 0)
                {
                    ConnectionState = ConnectionStates.Online;
                }
                else
                {
                    //TraceDbg.TraceON = true;
                    //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + "\t Connection error: " + _client.ErrorText(result));
                    ConnectionState = ConnectionStates.Offline;
                }
            }
            catch (Exception ex)
            {
                TraceDbg.TraceON = true;
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + "\t Connection error: " + ex.Message);
                ConnectionState = ConnectionStates.Offline;
                throw;
            }
        }

        public void Disconnect()
        {
            //#if NOSIM
            if (_client != null)
                _client.CloseXNet();
            //#endif
            ConnectionState = ConnectionStates.Offline;
        }

        public async void Start(string ipAddress, MaccData mac, int maccNr)
        {
            MaccNr = maccNr;
            await PollXNetAsync(ipAddress, mac);
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public void StartTest(string ipAddress, int reg, int len)
        {
            this.TestReg = reg;
            this.TestLen = len;
            PollXNetTest(ipAddress);
        }

        public void StopTest()
        {
            _cts.Cancel();
        }

        private void OnValuesRefreshed()
        {
            ValuesRefreshed?.Invoke(this, new EventArgs());
        }

        #region Get/Set the bit at Pos.Bit
        public static bool GetBitAt(ushort[] Buffer, int Pos, int Bit)
        {
            byte[] Mask = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
            if (Bit < 0) Bit = 0;
            if (Bit > 7) Bit = 7;
            return (Buffer[Pos] & Mask[Bit]) != 0;
        }
        public static void SetBitAt(ref ushort[] Buffer, int Pos, int Bit, bool Value)
        {
            byte[] Mask = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
            if (Bit < 0) Bit = 0;
            if (Bit > 7) Bit = 7;

            if (Value)
                Buffer[Pos] = (byte)(Buffer[Pos] | Mask[Bit]);
            else
                Buffer[Pos] = (byte)(Buffer[Pos] & ~Mask[Bit]);
        }
        #endregion
        #region Get/Set 16 bit signed value (S7 int) -32768..32767
        public static Int16 GetIntAt(ushort[] Buffer, int Pos)
        {
            return (short)((Buffer[Pos] << 8) | Buffer[Pos + 1]);
        }
        public static void SetIntAt(ushort[] Buffer, int Pos, Int16 Value)
        {
            Buffer[Pos] = (byte)(Value >> 8);
            Buffer[Pos + 1] = (byte)(Value & 0x00FF);
        }
        #endregion


        private async Task PollXNetTest(string ipAddress)
        {
            int retryDelay = 1000;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    Connect(ipAddress);

                    ValueReaded = "Inizio Lettura";
                    OnValuesRefreshed();
                    await Task.Delay(2000, _cts.Token);
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        ushort val1 = (ushort)GetVarArea((uint)1210, "HD");
                        ushort val2 = (ushort)GetVarArea((uint)1214, "HD");
                        ushort val3 = (ushort)GetVarArea((uint)600, "HD");
                        ushort val4 = (ushort)GetVarArea((uint)601, "HD");
                        ValueReaded = $"\nHD 1210={val1}   HD 1214={val2}         HD 601-600={val4}-{val3}";
                        ushort[] response = GetStateAreaVal((uint)TestReg, (uint)TestLen);
                        ValueReaded += $"\n{response.Count()}:";
                        foreach (ushort value in response)
                        {
                            if (value == 0)
                                continue;
                            ValueReaded += $" {(char)(value & 0xff)}";
                            ValueReaded += $" {(char)(value >> 8 & 0xff)}";
                            //ValueReaded += (char)(value & 0xff);
                            //ValueReaded += (char)(value >> 8 & 0xff);
                        }
                        OnValuesRefreshed();

                        await Task.Delay(1000, _cts.Token);
                    }
                }
                catch (Exception ex)
                {
                    //vm.ReportError("XNet error: " + ex.Message);
                    //_logger.LogWarning(ex, "XNet error for {ip}", vm.IpAddress);
                    ValueReaded = $"{ex.Message}";
                    await Task.Delay(retryDelay, _cts.Token);
                    OnValuesRefreshed();
                    retryDelay = Math.Min(retryDelay * 2, 10000);
                }
            }
        }

        // Ottimizzata
        private async Task PollXNetAsync(string ipAddress, MaccData mac)
        {
            ushort[] buf500 = new ushort[500];
            int retryDelay = 1000;
            Connect(ipAddress);

            TraceDbg.TraceON = true;
            TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t Start PollXNetAsync {_client}");

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var stopwatch = new Stopwatch();

                    while (!_cts.Token.IsCancellationRequested)
                    {
                        stopwatch.Restart();

                        if (_client == null) // Simulazione
                        {
                            Random random = new Random();
                            StatoRun = random.Next(2) != 0;
                            StatoPause = random.Next(2) != 0;
                            StatoStop = random.Next(2) != 0;
                            StatoAllarme = random.Next(2) != 0;

                            mac.SetStatoRun(StatoRun);
                            mac.SetStatoPause(StatoPause);
                            mac.SetStatoStop(StatoStop);
                            mac.SetStatoAllarme(StatoAllarme);
                            mac.SetCounter(random.Next(10));
                            mac.SetRecordDati("CODE_TEST#1000x600.3x18");
                            AllArea[500] = (ushort)random.Next(2);
                            mac.SetAllarmsData(mac.canaleCom.ConvertUshort2ByteArray(AllArea));
                        }
                        else
                        {
                            // Lettura stati in parallelo
                            var statoTasks = new[]
                            {
                            Task.Run(() => (ushort)GetStateVal(mac.canaleCom.TAG_STATO_RUN)),
                            Task.Run(() => (ushort)GetStateVal(mac.canaleCom.TAG_STATO_PAUSE)),
                            Task.Run(() => (ushort)GetStateVal(mac.canaleCom.TAG_STATO_STOP)),
                            Task.Run(() => (ushort)GetStateVal(mac.canaleCom.TAG_STATO_ALARM)),
                            Task.Run(() => (ushort)GetVarArea(((mac.ID == 15) ? 601u : 1210u),"HD"))
                            };

                            await Task.WhenAll(statoTasks);

                            mac.SetStatoRun(statoTasks[0].Result != 0);
                            mac.SetStatoPause(statoTasks[1].Result != 0);
                            mac.SetStatoStop(statoTasks[2].Result != 0);
                            bool statoAllarme = statoTasks[3].Result != 0;
                            mac.SetStatoAllarme(statoAllarme);
                            int counter = statoTasks[4].Result;
                            mac.SetCounter(counter);

                            if (statoAllarme)
                            {
                                if (mac.ID == 15)
                                {
                                    var allarmeTasks = new[]
                                    {
                                    Task.Run(() => GetVarArea(0, 200)),
                                    Task.Run(() => GetVarArea(1000, 10)),
                                    Task.Run(() => GetVarArea(5100, 10))
                                    };

                                    await Task.WhenAll(allarmeTasks);

                                    Array.ConstrainedCopy(allarmeTasks[0].Result, 0, AllArea, 0, 200);
                                    Array.ConstrainedCopy(allarmeTasks[1].Result, 0, AllArea, 1000, 10);
                                    Array.ConstrainedCopy(allarmeTasks[2].Result, 0, AllArea, 5100, 10);
                                }
                                else
                                {
                                    var letture = new List<Task<ushort[]>>();
                                    var offsetList = new List<int>();

                                    for (uint i = 1; i < 13; i++)
                                    {
                                        if (i is 6 or 7 or 8 or 9 or 11) continue;

                                        letture.Add(Task.Run(() => GetVarArea(500 * i, 500)));
                                        offsetList.Add((int)(500 * (i + 1)));
                                    }

                                    var results = await Task.WhenAll(letture);

                                    for (int i = 0; i < results.Length; i++)
                                    {
                                        Array.ConstrainedCopy(results[i], 0, AllArea, offsetList[i], 500);
                                    }
                                }

                                mac.SetAllarmsData(mac.canaleCom.ConvertUshort2ByteArray(AllArea));
                            }

                            // Lettura area dati
                            var strings = mac.canaleCom.TAG_RECORD_DATI.Split('.');
                            int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
                            int len = Convert.ToInt32(strings[1].Replace("LEN", ""));
                            VarArea = await Task.Run(() => GetArea((uint)reg, 0, (uint)len, XNetRegs.D));

                            mac.SetRecordDati(mac.canaleCom.ConvertUshort2ByteArray(VarArea));
                        }

                        // Delay adattivo per evitare loop troppo veloci
                        stopwatch.Stop();
                        int delay = Math.Max(50 - (int)stopwatch.ElapsedMilliseconds, 10);
                        await Task.Delay(delay, _cts.Token);
                    }
                }
                catch (Exception ex)
                {
                    TraceDbg.TRACE($"XNet Exception: {ex.Message}");
                    await Task.Delay(retryDelay, _cts.Token);
                    retryDelay = Math.Min(retryDelay * 2, 10000);
                }
            }
        }


        private async Task PollXNetAsyncOrig(string ipAddress, MaccData mac)
        {
            ushort[] buf500 = new ushort[500];
            int retryDelay = 1000;
            Connect(ipAddress);
            TraceDbg.TraceON = true;
            TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t Start PollXNetAsync {_client}");

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {

                    while (!_cts.Token.IsCancellationRequested)
                    {
                        if (_client == null) // simulazione
                        {
                            Random random = new Random();
                            StatoRun = random.Next(2) != 0;
                            StatoPause = random.Next(2) != 0;
                            StatoStop = random.Next(2) != 0;
                            StatoAllarme = random.Next(2) != 0;

                            //mac.DispatcherService.Invoke(() =>
                            //{
                            mac.SetStatoRun(StatoRun);
                            mac.SetStatoPause(StatoPause);
                            mac.SetStatoStop(StatoStop);
                            mac.SetStatoAllarme(StatoAllarme);
                            mac.SetRecordDati("CODE_TEST#1000x600.3x18");
                            AllArea[500] = (ushort)random.Next(2);
                            mac.SetAllarmsData(mac.canaleCom.ConvertUshort2ByteArray(AllArea));
                            //});
                        }
                        else
                        {
                            //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t TAG_STATO_RUN: Get val {mac.canaleCom.TAG_STATO_RUN}");
                            bool response = (ushort)GetStateVal(mac.canaleCom.TAG_STATO_RUN) != 0;
                            //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t TAG_STATO_RUN: {response}");
                            mac.SetStatoRun(response);
                            //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t TAG_STATO_RUN: after set . Next :{mac.canaleCom.TAG_STATO_PAUSE}");
                            response = (ushort)GetStateVal(mac.canaleCom.TAG_STATO_PAUSE) != 0;
                            mac.SetStatoPause(response);
                            //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t TAG_STATO_PAUSE: {response}. Next :{mac.canaleCom.TAG_STATO_STOP}");
                            response = (ushort)GetStateVal(mac.canaleCom.TAG_STATO_STOP) != 0;
                            mac.SetStatoStop(response);
                            //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t TAG_STATO_STOP: {response}. Next :{mac.canaleCom.TAG_STATO_ALARM}");
                            bool StateAllarm = (ushort)GetStateVal(mac.canaleCom.TAG_STATO_ALARM) != 0;
                            mac.SetStatoAllarme(StateAllarm);
                            //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t TAG_STATO_ALARM  {mac.canaleCom.TAG_STATO_ALARM}: {ipAddress}-{response}.");

                            //mac.GetAllarms();
                            if (StateAllarm)
                            {
                                if (mac.ID == 15)
                                {
                                    buf500 = GetVarArea(0, 200);
                                    Array.ConstrainedCopy(buf500, 0, AllArea, 0, 200);
                                    buf500 = GetVarArea(1000, 10);
                                    Array.ConstrainedCopy(buf500, 0, AllArea, 1000, 10);
                                    buf500 = GetVarArea(5100, 10);
                                    Array.ConstrainedCopy(buf500, 0, AllArea, 5100, 10);
                                }
                                else
                                {
                                    for (uint i = 1; i < 13; i++)
                                    {
                                        if (i == 6 || i == 7 || i == 8 || i == 9 || i == 11)
                                            continue;
                                        buf500 = GetVarArea(500 * i, 500);
                                        Array.ConstrainedCopy(buf500, 0, AllArea, (int)(500 * (i + 1)), 500);
                                    }
                                }
                                mac.SetAllarmsData(mac.canaleCom.ConvertUshort2ByteArray(AllArea));
                            }

                            var strings = mac.canaleCom.TAG_RECORD_DATI.Split('.');
                            int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
                            int len = Convert.ToInt32(strings[1].Replace("LEN", ""));
                            VarArea = GetArea((uint)reg, 0, (uint)len, XNetRegs.D);
                            mac.SetRecordDati(mac.canaleCom.ConvertUshort2ByteArray(VarArea));
                            //TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\t SetRecordDati: {VarArea}");
                            //response = (ushort)GetStateArea(mac.canaleCom.TAG_RECORD_DATI) != 0;
                            //mac.SetRecordDati(response);        // Completare

                            //mac.DispatcherService.Invoke(() =>
                            //{
                            //    mac.GruppoA = gruppoA;
                            //    mac.GruppoB = gruppoB;
                            //});
                        }
                        await Task.Delay(50, _cts.Token);
                    }
                }
                catch (Exception ex)
                {
                    //vm.ReportError("XNet error: " + ex.Message);
                    //_logger.LogWarning(ex, "XNet error for {ip}", vm.IpAddress);
                    await Task.Delay(retryDelay, _cts.Token);
                    retryDelay = Math.Min(retryDelay * 2, 10000);
                }
            }
        }
        /// <summary>
        /// Read all bytes of a db for a specified length. Es.: DB1.DBL256 read 256 bytes in db 1
        /// </summary>
        /// <param name="address">Es.: REG1.LEN256 read 256 bytes in db 1</param>
        /// <returns> byte [] </returns> return the readed buffer
        public object GetStateVal(string address)
        {
            ushort varn = 0;
            if (string.IsNullOrEmpty(address))
                return varn;
            var strings = address.Split('.');
            int reg = Convert.ToInt32(strings[1]);
            return GetVarArea((uint)reg, "M");
        }
        private ushort[] GetStateAreaVal(uint reg, uint length)
        {
            TraceDbg.TraceON = true;
            lock (_locker)
            {
                //TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read reg:D {reg}-{length} ");
                ushort[] buffer = new ushort[length];
                try
                {
                    if (_client != null)
                        _client.ReadRegs(XNetRegs.D, reg, buffer);
                    else
                    {
                        buffer[2] = 48;
                        buffer[5] = 49;
                        buffer[7] = 50;
                        buffer[8] = 51;
                        buffer[9] = 53;
                    }
                }
                catch (Exception ex)
                {
                    TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error REG M:{reg} Messagge:{ex.Message}");
                    buffer[0] = 50;
                }
                return buffer;
            }
        }
        /// <summary>
        /// Read a bool registry
        /// </summary>
        /// <param name="address">Es.: REG1
        /// <returns> byte [] </returns> return the readed buffer
        public ushort[] GetVarArea(string address)
        {
            var strings = address.Split('.');
            int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
            ushort[] res = GetVarArea((uint)reg, 1);

            ValueReaded += $" {(res[0] & 0xff)}";
            OnValuesRefreshed();
            return res;
        }
        /// <summary>
        /// Read a bool registry
        /// </summary>
        /// <param name="address">Es.: REG1
        /// <returns> byte [] </returns> return the readed buffer
        public object GetVarArea(uint address, string type)
        {
            ushort[] res = null;
            switch (type)
            {
                case "HD": res = GetArea(address, 0, 20, XNetRegs.HD); break;
                case "HM": res = GetVarArea(address, 1, XNetCoils.HM); break;
                default: res = GetVarArea(address, 1); break;
            }
            return res[0];
        }
        private ushort[] GetVarArea(uint reg, uint length, XNetCoils t = XNetCoils.M)
        {
            TraceDbg.TraceON = true;
            ushort[] buffer = new ushort[length];
            lock (_locker)
            {
                //TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read reg:M {reg}-{length} \n");
                try
                {
                    if (_client != null)
                    {
                        //TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read init REG M:{reg} \n");
                        _client.ReadCoils(t, reg, buffer);
                        //TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read end REG M:{reg} \n");
                    }
                }
                catch (Exception ex)
                {
                    TraceDbg.DBGTraceLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error REG M:{reg} {ex.Message}\n");
                    buffer[0] = 50;
                }
                return buffer;
            }
        }

        public ushort[] GetArea(uint reg, uint start, uint length, XNetRegs t)
        {
            lock (_locker)
            {
                ushort[] buffer = new ushort[length];
                try
                {
                    if (_client != null)
                        _client.ReadRegs(t, reg, buffer);
                }
                catch (Exception ex)
                {
                    TraceDbg.DBGLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Read error: {ex.Message}\n");
                    return new ushort[256];
                }
                return buffer;
            }
        }


        /// <summary>
        /// Write all bytes in a db for a specified length. Es.: DB1.DBL256 read 256 bytes in db 1
        /// </summary>
        /// <param name="address">Es.: DB1.DBL256 write 256 bytes in db 1</param>
        /// <param name="buf">bytes to write
        /// <returns> byte [] </returns> return the readed buffer
        public ushort[] SetArea(string address, ushort[] buf)
        {
            var strings = address.Split('.');
            int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
            int len = Convert.ToInt32(strings[1].Replace("LEN", ""));
            return SetArea((uint)reg, (uint)len, buf);
        }
        private ushort[] SetArea(uint reg, uint length, ushort[] buf)
        {
            lock (_locker)
            {
                try
                {
                    _client.WriteRegs(XNetRegs.D, reg, buf);
                }
                catch (Exception ex)
                {
                    TraceDbg.DBGLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Write error: {ex.Message}\n");
                    return new ushort[1];
                }
                return buf;
            }
        }

        /// <summary>
        /// Writes a bit at the specified address. Es.: REG1.X10.2 writes the bit in db 1, word 10, 3rd bit
        /// </summary>
        /// <param name="address">Es.: REG1.X10.2 writes the bit in REG 1, word 10, 3rd bit</param>
        /// <param name="value">true or false</param>
        /// <returns></returns>
        private ushort[] WriteBit(string address, bool value)
        {
            var strings = address.Split('.');
            int reg = Convert.ToInt32(strings[0].Replace("REG", ""));
            int pos = Convert.ToInt32(strings[1].Replace("X", ""));
            int bit = Convert.ToInt32(strings[2]);
            return WriteBit((uint)reg, pos, bit, value);
        }

        private ushort[] WriteBit(uint reg, int pos, int bit, bool value)
        {
            lock (_locker)
            {
                var buffer = new ushort[1];
                SetBitAt(ref buffer, 0, bit, value);
                try
                {
                    _client.WriteRegs(XNetRegs.D, reg, buffer);
                }
                catch (Exception ex)
                {
                    TraceDbg.DBGLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Write error: {ex.Message}\n");
                    return new ushort[1];
                }
                return buffer;
            }
        }

        /// <summary>
        /// Writes a word at the specified address. Es.: DB1.DBX10 writes the word 10 in db 1
        /// </summary>
        /// <param name="address">Es.: DB1.DBX10 writes the word 10 in db 1</param>
        /// <param name="value">true or false</param>
        /// <returns></returns>
        private void WriteWord(string address, UInt16 value)
        {
            var strings = address.Split('.');
            ushort reg = (ushort)Convert.ToInt32(strings[0].Replace("DB", ""));
            int pos = Convert.ToInt32(strings[1].Replace("DBX", ""));
            WriteWord(reg, pos, value);
        }

        private void WriteWord(ushort reg, int pos, UInt16 value)
        {
            lock (_locker)
            {
                var buffer = new ushort[4];
                SetIntAt(buffer, 0, (Int16)value);
                try
                {
                    _client.WriteRegs(XNetRegs.D, reg, buffer);
                }
                catch (Exception ex)
                {
                    TraceDbg.DBGLog(DateTime.Now.ToString("HH:mm:ss") + $"\t Write error: {ex.Message}\n");
                }
            }
        }
    }
}

