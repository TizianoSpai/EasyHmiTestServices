using Sharp7;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Trace;

namespace PlcService
{
    public class S7PlcService
    {
        private readonly S7Client _client;
        private readonly System.Timers.Timer _timer;
        private DateTime _lastScanTime;

        private volatile object _locker = new object();

        public S7PlcService()
        {
            _client = new S7Client();
            //_timer = new System.Timers.Timer();
            //_timer.Interval = 100;
            //_timer.Elapsed += OnTimerElapsed;
        }

        public ConnectionStates ConnectionState { get; private set; }

        public bool HighLimit { get; private set; }

        public bool LowLimit { get; private set; }

        public bool PumpState { get; private set; }

        public byte[] StateArea { get; set; } = new byte[256];

        public int TankLevel { get; private set; }

        public TimeSpan ScanTime { get; private set; }

        public event EventHandler ValuesRefreshed;

        public void Connect(string ipAddress, int rack, int slot)
        {
            int result = 0;
            try
            {
                ConnectionState = ConnectionStates.Connecting;
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + $"\tConnect => IP:{ipAddress}, rack:{rack}, slot:{slot} ");
                result = _client.ConnectTo(ipAddress, rack, slot);
                if (result == 0)
                {
                    ConnectionState = ConnectionStates.Online;
                    //_timer.Start();
                }
                else
                {
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "\t Connection error: " + _client.ErrorText(result));
                    TraceDbg.TraceON = true;
                    TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + "\t Connection error: " + _client.ErrorText(result));
                    ConnectionState = ConnectionStates.Offline;
                }
                OnValuesRefreshed();
            }
            catch (Exception ex)
            {
                TraceDbg.TraceON = true;
                TraceDbg.TRACE(DateTime.Now.ToString("HH:mm:ss") + "\t Connection error: " + ex.Message);
                ConnectionState = ConnectionStates.Offline;
                OnValuesRefreshed();
                throw;
            }
        }

        public void Disconnect()
        {
            if (_client.Connected)
            {
                _timer.Stop();
                _client.Disconnect();
                ConnectionState = ConnectionStates.Offline;
                OnValuesRefreshed();
            }
        }


        public async Task WriteBt(string tag, bool val)
        {
            await Task.Run(() =>
            {
                int writeResult = WriteBit(tag, val);
                if (writeResult != 0)
                {
                    string DbgMsg = DateTime.Now.ToString("HH:mm:ss") + "\t Write error: " + _client.ErrorText(writeResult);
                    TraceDbg.DBGLog(DbgMsg);
                    Debug.WriteLine(DbgMsg);
                }
            });
        }

        public async Task WriteInt(string tag, int val)
        {
            await Task.Run((Action)(() =>
            {
                int writeResult = this.WriteInt16(tag, val);
                if (writeResult != 0)
                {
                    string DbgMsg = DateTime.Now.ToString("HH:mm:ss") + "\t Write error: " + _client.ErrorText(writeResult);
                    TraceDbg.DBGLog(DbgMsg);
                    Debug.WriteLine(DbgMsg);
                }
            }));
        }

        public async Task WriteDInt(string tag, int val)
        {
            await Task.Run((Action)(() =>
            {
                int writeResult = this.WriteInt32(tag, val);
                if (writeResult != 0)
                {
                    string DbgMsg = DateTime.Now.ToString("HH:mm:ss") + "\t Write error: " + _client.ErrorText(writeResult);
                    TraceDbg.DBGLog(DbgMsg);
                    Debug.WriteLine(DbgMsg);
                }
            }));
        }

        public async Task WriteMW(string tag, UInt16 val)
        {
            await Task.Run(() =>
            {
                int writeResult = WriteWord(tag, val);
                if (writeResult != 0)
                {
                    string DbgMsg = DateTime.Now.ToString("HH:mm:ss") + "\t Write error: " + _client.ErrorText(writeResult);
                    TraceDbg.DBGLog(DbgMsg);
                    Debug.WriteLine(DbgMsg);
                }
            });
        }

        public async Task WriteStop()
        {
            await Task.Run(() =>
            {
                int writeResult = WriteBit("DB1.DBX2000.1", true);
                if (writeResult != 0)
                {
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "\t Write error: " + _client.ErrorText(writeResult));
                }
                Thread.Sleep(30);
                writeResult = WriteBit("DB1.DBX2000.1", false);
                if (writeResult != 0)
                {
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "\t Write error: " + _client.ErrorText(writeResult));
                }
            });
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _timer.Stop();
                ScanTime = DateTime.Now - _lastScanTime;
                RefreshValues();
                OnValuesRefreshed();
            }
            finally
            {
                _timer.Start();
            }
            _lastScanTime = DateTime.Now;
        }

        private void RefreshValues()
        {
            lock (_locker)
            {
                var buffer = new byte[4];
                int result = _client.DBRead(1, 0, buffer.Length, buffer);
                if (result == 0)
                {
                    PumpState = S7.GetBitAt(buffer, 0, 2);
                    HighLimit = S7.GetBitAt(buffer, 0, 3);
                    LowLimit = S7.GetBitAt(buffer, 0, 4);
                    TankLevel = S7.GetIntAt(buffer, 2);
                }
                else
                {
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "\t Read error: " + _client.ErrorText(result));
                }
            }
        }

        /// <summary>
        /// Read all bytes of a db for a specified length. Es.: DB1.DBL256 read 256 bytes in db 1
        /// </summary>
        /// <param name="address">Es.: DB1.DBL256 read 256 bytes in db 1</param>
        /// <returns> byte [] </returns> return the readed buffer
        public byte[] GetStateArea(string address)
        {
            var strings = address.Split('.');
            int db = Convert.ToInt32(strings[0].Replace("DB", ""));
            int len = Convert.ToInt32(strings[1].Replace("DBL", ""));
            return GetStateArea(db, len);
        }
        private byte[] GetStateArea(int db, int length)
        {
            lock (_locker)
            {
                byte[] buffer = new byte[length];
                int result = _client.DBRead(db, 0, length, buffer);
                if (result != 0)
                {
                    TraceDbg.DBGLog(DateTime.Now.ToString("HH:mm:ss") + "\t Read error: " + _client.ErrorText(result));
                    return new byte[256];
                }
                return buffer;
            }
        }
        public byte[] GetArea(int db, int start, int length)
        {
            lock (_locker)
            {
                byte[] buffer = new byte[length];
                int result = _client.DBRead(db, start, length, buffer);
                if (result != 0)
                {
                    TraceDbg.DBGLog(DateTime.Now.ToString("HH:mm:ss") + "\t Read error: " + _client.ErrorText(result));
                    return new byte[256];
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
        public byte[] SetDBArea(string address, byte[] buf)
        {
            var strings = address.Split('.');
            int db = Convert.ToInt32(strings[0].Replace("DB", ""));
            int len = Convert.ToInt32(strings[1].Replace("DBL", ""));
            return SetDBArea(db, len, buf);
        }
        private byte[] SetDBArea(int db, int length, byte[] buf)
        {
            lock (_locker)
            {
                int result = _client.WriteArea(S7Consts.S7AreaDB, db, 0, buf.Length, S7Consts.S7WLByte, /*S7Consts.S7WLInt,*/ buf);
                if (result != 0)
                {
                    TraceDbg.DBGLog(DateTime.Now.ToString("HH:mm:ss") + "\t Write error: " + _client.ErrorText(result));
                    return new byte[1];
                }
                return buf;
            }
        }

        /// <summary>
        /// Writes a bit at the specified address. Es.: DB1.DBX10.2 writes the bit in db 1, word 10, 3rd bit
        /// </summary>
        /// <param name="address">Es.: DB1.DBX10.2 writes the bit in db 1, word 10, 3rd bit</param>
        /// <param name="value">true or false</param>
        /// <returns></returns>
        private int WriteBit(string address, bool value)
        {
            var strings = address.Split('.');
            int db = Convert.ToInt32(strings[0].Replace("DB", ""));
            int pos = Convert.ToInt32(strings[1].Replace("DBX", ""));
            int bit = Convert.ToInt32(strings[2]);
            return WriteBit(db, pos, bit, value);
        }

        private int WriteBit(int db, int pos, int bit, bool value)
        {
            lock (_locker)
            {
                var buffer = new byte[1];
                S7.SetBitAt(ref buffer, 0, bit, value);
                return _client.WriteArea(S7Consts.S7AreaDB, db, pos * 8 + bit, buffer.Length, S7Consts.S7WLBit, buffer);
            }
        }

        /// <summary>
        /// Writes a word at the specified address. Es.: DB1.DBX10 writes the word 10 in db 1
        /// </summary>
        /// <param name="address">Es.: DB1.DBX10 writes the word 10 in db 1</param>
        /// <param name="value">true or false</param>
        /// <returns></returns>
        private int WriteWord(string address, UInt16 value)
        {
            var strings = address.Split('.');
            int db = Convert.ToInt32(strings[0].Replace("DB", ""));
            int pos = Convert.ToInt32(strings[1].Replace("DBX", ""));
            return WriteWord(db, pos, value);
        }

        private int WriteWord(int db, int pos, UInt16 value)
        {
            lock (_locker)
            {
                var buffer = new byte[4];
                //s7.setlintat(buffer, 0, value);
                //return _client.writearea(s7consts.s7areadb, db, pos * 16, buffer.length, s7consts.s7wlint, buffer);
                S7.SetIntAt(buffer, 0, (Int16)value);
                return _client.WriteArea(S7Consts.S7AreaDB, db, pos * 8, buffer.Length, S7Consts.S7WLByte, buffer);
            }
        }
        /// <summary>
        /// Writes a bit at the specified address. Es.: DB1.DBX10.2 writes the bit in db 1, word 10, 3rd bit
        /// </summary>
        /// <param name="address">Es.: DB1.DBX10.2 writes the bit in db 1, word 10, 3rd bit</param>
        /// <param name="value">true or false</param>
        /// <returns></returns>
        private int WriteInt16(string address, int value)
        {
            var strings = address.Split('.');
            int db = Convert.ToInt32(strings[0].Replace("DB", ""));
            int pos = Convert.ToInt32(strings[1].Replace("DBX", ""));
            return WriteInt(db, pos, value);
        }
        private int WriteInt32(string address, int value)
        {
            var strings = address.Split('.');
            int db = Convert.ToInt32(strings[0].Replace("DB", ""));
            int pos = Convert.ToInt32(strings[1].Replace("DBX", ""));
            return WriteDInt(db, pos, value);
        }

        private int WriteInt(int db, int pos, int value)
        {
            lock (_locker)
            {
                var buffer = new byte[4];
                S7.SetIntAt(buffer, 0, (short)value);
                return _client.WriteArea(S7Consts.S7AreaDB, db, pos, buffer.Length, S7Consts.S7WLByte, buffer);
            }
        }

        private int WriteDInt(int db, int pos, int value)
        {
            lock (_locker)
            {
                var buffer = new byte[4];
                S7.SetDIntAt(buffer, 0, (short)value);
                return _client.WriteArea(S7Consts.S7AreaDB, db, pos, buffer.Length, S7Consts.S7WLByte, buffer);
            }
        }

        private void OnValuesRefreshed()
        {
            ValuesRefreshed?.Invoke(this, new EventArgs());
        }
    }
}

