using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Syntec.Remote;

namespace SyntecRemoteClient
{
  public partial class ExampleForm : Form
  {
    List<SyntecRemoteCNC> m_CNC;
    SyntecRemoteCNC API1 = null;
    System.Windows.Forms.Timer m_tmr300ms;

    public ExampleForm()
    {
      InitializeComponent();
      RemoveUnusedProcess();

      m_tmr300ms = new System.Windows.Forms.Timer();
      m_tmr300ms.Interval = 300;
      m_tmr300ms.Tick += new EventHandler(m_tmr300ms_Tick);
      m_tmr300ms.Enabled = true;
      buttonTimerOn.Enabled = false;
      buttonTimerOff.Enabled = true;

      m_CNC = new List<SyntecRemoteCNC>();

      /*
			// Add your CNCs' host-ip here
			SyntecRemoteCNC cnc = new SyntecRemoteCNC( "192.168.1.211" );
			m_CNC.Add( cnc );

			//cnc = new SyntecRemoteCNC( "10.10.1.207" );
			//m_CNC.Add( cnc );
			//cnc = new SyntecRemoteCNC( "10.10.1.96" );
			//m_CNC.Add( cnc );

			foreach( SyntecRemoteCNC tmp in m_CNC ) {
				listBox1.Items.Add( tmp.Host );
			*/
    }

    private void btnCon_Click(object sender, EventArgs e)
    {
      try
      {
        string sIp = edtIp.Text.Trim();

        SyntecRemoteCNC cnc = new SyntecRemoteCNC( sIp );
        m_CNC.Add(cnc);

        //cnc = new SyntecRemoteCNC( "10.10.1.207" );
        //m_CNC.Add( cnc );
        //cnc = new SyntecRemoteCNC( "10.10.1.96" );
        //m_CNC.Add( cnc );

        listBox1.Items.Clear();
        foreach(SyntecRemoteCNC tmp in m_CNC)
        {
          listBox1.Items.Add(tmp.Host);
        }
      }
      catch(Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }

    private void m_tmr300ms_Tick(object sender, EventArgs e)
    {
      if(m_CNC.Count >= 1)
      {
        SyntecRemoteCNC cnc = m_CNC[ 0 ];
        short DecPoint = 0;
        string[] AxisName = null, Unit = null;
        float[] Mach = null, Abs = null, Rel = null, Dist = null;

        short result = cnc.READ_position( out AxisName, out DecPoint, out Unit, out Mach, out Abs, out Rel, out Dist );
        if(result == 0)
        {
          if(AxisName.Length > 0)
          {
            label1.Text = AxisName[0] + " : " + Mach[0].ToString();
          }
          if(AxisName.Length > 1)
          {
            label2.Text = AxisName[1] + " : " + Mach[1].ToString();
          }
          if(AxisName.Length > 2)
          {
            label3.Text = AxisName[2] + " : " + Mach[2].ToString();
          }
          if(AxisName.Length > 3)
          {
            label4.Text = AxisName[3] + " : " + Mach[3].ToString();
          }
        }
        else
        {
          label1.Text = "Err : " + result.ToString();
          label2.Text = "Err : " + result.ToString();
          label3.Text = "Err : " + result.ToString();
          label4.Text = "Err : " + result.ToString();
        }
      }
    }

    private void btnTimerOn_Click(object sender, EventArgs e)
    {
      m_tmr300ms.Enabled = true;

      buttonTimerOn.Enabled = false;
      buttonTimerOff.Enabled = true;
    }
    private void btnTimerOff_Click(object sender, EventArgs e)
    {
      m_tmr300ms.Enabled = false;

      buttonTimerOn.Enabled = true;
      buttonTimerOff.Enabled = false;
    }

    ~ExampleForm()
    {
      deinit();
      RemoveUnusedProcess();
    }

    private void RemoveUnusedProcess()
    {
      Process[] process = Process.GetProcessesByName( "SyntecRemoteServer" );
      foreach(Process p in process)
      {
        p.Kill();
      }
    }
    private void deinit()
    {
      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        cnc.Close();
      }
    }

    private void button10_Click(object sender, EventArgs e)
    {
      if(API1.isConnected())
      {

        short Axes = 0, MaxAxes = 0;
        string CncType = "", Series = "", Nc_Ver = "";
        string[] AxisName = null;

        short result = API1.READ_information( out Axes, out CncType, out MaxAxes, out Series, out Nc_Ver, out AxisName );
        if(result < 0)
        {
          MessageBox.Show("Error");
        }

        float[] data = new float[ Axes ];

        for(int i = 0; i < Axes; i++)
        {
          data[i] = (float)Convert.ToDouble(textBox4.Text);
        }

        result = API1.WRITE_work_coord_single("EXT", data);
        if(result == 0)
        {
          MessageBox.Show("Done");
        }
        else
        {
          MessageBox.Show("Error" + result.ToString());
        }
      }
    }
    private void button13_Click(object sender, EventArgs e)
    {
      if(API1.isConnected())
      {
        if(API1.WRITE_macro_single(1, Convert.ToDouble(textBox5.Text)) == 0)
        {
          MessageBox.Show("Done");
        }
        else
        {
          MessageBox.Show("Error");
        }
      }
    }
    private void button24_Click(object sender, EventArgs e)
    {
      if(API1.isConnected())
      {

        string[] title = null;
        if(API1.READ_offset_title(out title) != 0)
        {
          return;
        }

        m_ListBoxMessageDisplay.Items.Clear();
        double[] offset = new double[ title.Length ];

        for(int i = 0; i < offset.Length; i++)
        {
          offset[i] = Convert.ToDouble(textBox10.Text) + 0.001 * i;
        }

        if(API1.WRITE_offset_single(45, offset) == 0)
        {
          MessageBox.Show("Done");
        }
        else
        {
          MessageBox.Show("Error");
        }
      }
    }
    private void button25_Click(object sender, EventArgs e)
    {
      if(API1.isConnected())
      {

        string[] title = null;
        if(API1.READ_offset_title(out title) != 0)
        {
          return;
        }

        m_ListBoxMessageDisplay.Items.Clear();
        double[][] offset = new double[ 11 ][];

        for(int k = 0; k < 11; k++)
        {
          offset[k] = new double[title.Length];
          for(int i = 0; i < offset[k].Length; i++)
          {
            offset[k][i] = Convert.ToDouble(textBox11.Text);
          }
        }

        if(API1.WRITE_offset_all(offset) == 0)
        {
          MessageBox.Show("Done");
        }
        else
        {
          MessageBox.Show("Error");
        }
      }
    }

    private void btnREAD_Information_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        short nAxes = 0, nMaxAxes = 0;
        string szCncType = "", szSeries = "", szNc_Ver = "";
        string[] szAxesName = null;

        // get information from cnc
        short result = cnc.READ_information( out nAxes, out szCncType, out nMaxAxes, out szSeries, out szNc_Ver, out szAxesName );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_information: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display information
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Axes : " + nAxes.ToString());
        m_ListBoxMessageDisplay.Items.Add("CncType : " + szCncType.ToString());
        m_ListBoxMessageDisplay.Items.Add("MaxAxes : " + nMaxAxes.ToString());
        m_ListBoxMessageDisplay.Items.Add("Series : " + szSeries.ToString());
        m_ListBoxMessageDisplay.Items.Add("NC_Ver : " + szNc_Ver.ToString());

        string szAxisName = "";
        if(szAxesName != null && szAxesName.Length > 0)
        {
          szAxisName = szAxesName[0];
          for(int i = 1; i < szAxesName.Length; i++)
          {
            szAxisName += ":" + szAxesName[i];
          }
        }
        m_ListBoxMessageDisplay.Items.Add("AxisName : " + szAxisName);
      }
    }

    private void btnREAD_status_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int nCurSeq = 0;
        string szMainProg = "", szCurProg = "", szMode = "", szStatus = "", szAlarm = "", szEMG = "";

        // get status from cnc
        short result = cnc.READ_status( out szMainProg, out szCurProg, out nCurSeq, out szMode, out szStatus, out szAlarm, out szEMG );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_status: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display status
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("MainProg : " + szMainProg.ToString());
        m_ListBoxMessageDisplay.Items.Add("CurrentProg : " + szCurProg.ToString());
        m_ListBoxMessageDisplay.Items.Add("CurrentSeq : " + nCurSeq.ToString());
        m_ListBoxMessageDisplay.Items.Add("Mode : " + szMode.ToString());
        m_ListBoxMessageDisplay.Items.Add("Status : " + szStatus.ToString());
        m_ListBoxMessageDisplay.Items.Add("Alarm : " + szAlarm.ToString());
        m_ListBoxMessageDisplay.Items.Add("EMG : " + szEMG.ToString());
      }
    }

    private void btnREAD_position_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        short nDecPoint = 0;
        string[] szAxesName = null, szUnits = null;
        float[] Mach = null, Abs = null, Rel = null, Dist = null;

        // get position info. from cnc
        short result = cnc.READ_position( out szAxesName, out nDecPoint, out szUnits, out Mach, out Abs, out Rel, out Dist );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_position: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display position info.
        string szAxisName = szAxesName[ 0 ];
        for(int i = 1; i < szAxesName.Length; i++)
        {
          szAxisName += ":" + szAxesName[i];
        }
        m_ListBoxMessageDisplay.Items.Insert(0, "AxisName : " + szAxisName);

        m_ListBoxMessageDisplay.Items.Insert(0, "DecPoint : " + nDecPoint.ToString());

        szAxisName = szUnits[0];
        for(int i = 1; i < szUnits.Length; i++)
        {
          szAxisName += ":" + szUnits[i];
        }
        m_ListBoxMessageDisplay.Items.Insert(0, "Unit : " + szAxisName);

        szAxisName = Mach[0].ToString();
        for(int i = 1; i < Mach.Length; i++)
        {
          szAxisName += ":" + Mach[i].ToString();
        }
        m_ListBoxMessageDisplay.Items.Insert(0, "Machine : " + szAxisName);

        szAxisName = Abs[0].ToString();
        for(int i = 1; i < Abs.Length; i++)
        {
          szAxisName += ":" + Abs[i].ToString();
        }
        m_ListBoxMessageDisplay.Items.Insert(0, "Absolute : " + szAxisName);

        szAxisName = Rel[0].ToString();
        for(int i = 1; i < Rel.Length; i++)
        {
          szAxisName += ":" + Rel[i].ToString();
        }
        m_ListBoxMessageDisplay.Items.Insert(0, "Relative : " + szAxisName);

        szAxisName = Dist[0].ToString();
        for(int i = 1; i < Dist.Length; i++)
        {
          szAxisName += ":" + Dist[i].ToString();
        }
        m_ListBoxMessageDisplay.Items.Insert(0, "Distance : " + szAxisName);

        m_ListBoxMessageDisplay.Items.Insert(0, "===" + cnc.Host + "===");
      }
    }

    private void btnREAD_gcode_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string[] szGCodeList = null;

        // get gcode list from cnc
        short result = cnc.READ_gcode( out szGCodeList );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_gcode: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display gcode list
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        for(int i = 0; i < szGCodeList.Length; i++)
        {
          m_ListBoxMessageDisplay.Items.Add(szGCodeList[i]);
        }
      }
    }

    private void btnRead_othercode_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int D = 0, H = 0, S = 0, T = 0, M = 0, F = 0;

        // get othercode list from cnc
        short result = cnc.READ_othercode( out H, out D, out T, out M, out F, out S );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_othercode: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display othercode list
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("H:" + H.ToString());
        m_ListBoxMessageDisplay.Items.Add("D:" + D.ToString());
        m_ListBoxMessageDisplay.Items.Add("T:" + T.ToString());
        m_ListBoxMessageDisplay.Items.Add("M:" + M.ToString());
        m_ListBoxMessageDisplay.Items.Add("F:" + F.ToString());
        m_ListBoxMessageDisplay.Items.Add("S:" + S.ToString());
      }
    }

    private void btnRead_feed_spindle_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        float OvFeed = 0.0F, OvSpindle = 0.0F, ActFeed = 0.0F;
        int nActSpindle = 0;

        // get speeds and feeds from cnc
        short result = cnc.READ_spindle( out OvFeed, out OvSpindle, out ActFeed, out nActSpindle );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_spindle: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display speeds and feeds
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Override Feedrate : " + OvFeed.ToString());
        m_ListBoxMessageDisplay.Items.Add("Override Spindle : " + OvSpindle.ToString());
        m_ListBoxMessageDisplay.Items.Add("Actual Feedrate : " + ActFeed.ToString());
        m_ListBoxMessageDisplay.Items.Add("Actual Spindle : " + nActSpindle.ToString());
      }
    }

    private void btnRead_part_count_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int nPartCount = 0;
        int nRequiredPartCount = 0;
        int nTotalPartCount = 0;

        // get part count from cnc
        short result = cnc.READ_part_count( out nPartCount, out nRequiredPartCount, out nTotalPartCount );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_part_count: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display part count info.
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("PartCount : " + nPartCount.ToString());
        m_ListBoxMessageDisplay.Items.Add("RequirePartCount : " + nPartCount.ToString());
        m_ListBoxMessageDisplay.Items.Add("TotalPartCount : " + nPartCount.ToString());
      }
    }

    private void btnRead_Time_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int nPowerOnTime = 0, nCuttingTime = 0, nCycleTime = 0, nWorkTime = 0;

        // get time info. from cnc
        short result = cnc.READ_time( out nPowerOnTime, out nCuttingTime, out nCycleTime, out nWorkTime );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_time: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display time info.
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("PowerOnTime : " + nPowerOnTime.ToString());
        m_ListBoxMessageDisplay.Items.Add("CuttingTime : " + nCuttingTime.ToString());
        m_ListBoxMessageDisplay.Items.Add("CycleTime" + nCycleTime.ToString());
      }
    }

    private void btnREAD_alm_current_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        bool isAlarm = false;
        string[] szMsg = null;
        DateTime[] time = null;

        // get current alarm from cnc
        short result = cnc.READ_alm_current( out isAlarm, out szMsg, out time );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_alm_current: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display current alarm
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("HasAlarm : " + isAlarm.ToString());
        if(szMsg != null)
        {
          for(int i = 0; i < szMsg.Length; i++)
          {
            m_ListBoxMessageDisplay.Items.Add("Time : " + time[i].ToString("yyyyMMdd HHmmss"));
            m_ListBoxMessageDisplay.Items.Add("MSG : " + szMsg[i]);
          }
        }

      }
    }

    private void btnREAD_alm_history_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string[] szMsg = null;
        DateTime[] time = null;

        // get history alarm from cnc
        short result = cnc.READ_alm_history( out szMsg, out time );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_alm_history: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display history alarm
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(szMsg != null)
        {
          for(int i = 0; i < szMsg.Length; i++)
          {
            m_ListBoxMessageDisplay.Items.Add("Time : " + time[i].ToString("yyyyMMdd HHmmss"));
            m_ListBoxMessageDisplay.Items.Add("MSG : " + szMsg[i]);
          }
        }
      }
    }

    private void btnREAD_offset_title_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string[] szTitles = null;

        // get tool offset title from cnc
        short result = cnc.READ_offset_title( out szTitles );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_offset_title: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display offset title
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(szTitles != null)
        {
          for(int i = 0; i < szTitles.Length; i++)
          {
            m_ListBoxMessageDisplay.Items.Add(szTitles[i]);
          }
        }
      }
    }

    private void btnREAD_offset_all_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        double[][] offset = null;

        // get tool offset data from cnc
        short result = cnc.READ_offset_all( out offset );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_offset_all: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display tool offset data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(offset != null)
        {
          for(int i = 0; i < offset.Length; i++)
          {
            string szTmp = "";
            for(int k = 0; k < offset[i].Length; k++)
            {
              szTmp += offset[i][k].ToString() + " ";
            }
            m_ListBoxMessageDisplay.Items.Add("T" + ((int)(i + 1)).ToString() + " : " + szTmp);
          }
        }
      }
    }

    private void btnREAD_offset_scope_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        double[][] offset = null;

        // get tool offset scope from cnc
        short result = cnc.READ_offset_scope( 20, 30, out offset );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_offset_scope: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display tool offset scope
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(offset != null)
        {
          for(int i = 0; i < offset.Length; i++)
          {
            string szTmp = "";
            for(int k = 0; k < offset[i].Length; k++)
            {
              szTmp += offset[i][k].ToString() + " ";
            }
            m_ListBoxMessageDisplay.Items.Add("T" + ((int)(i + 20)).ToString() + " : " + szTmp);
          }
        }
      }
    }

    private void btnREAD_offset_single_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        double[] offset = null;

        // get one tool offset data from cnc
        short result = cnc.READ_offset_single( 45, out offset );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_offset_single: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display one tool offset data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        string szTmp = "";
        if(offset != null)
        {
          for(int k = 0; k < offset.Length; k++)
          {
            szTmp += offset[k].ToString() + " ";
          }
        }
        m_ListBoxMessageDisplay.Items.Add("T45 : " + szTmp);
      }
    }

    private void btnREAD_work_coord_all_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string szTmp = "";
        string[] szName = null;
        float[][] data = null;

        // get all workpiece offset data from cnc
        short result = cnc.READ_work_coord_all( out szName, out data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_offset_single: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display all workpiece offset data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(szName != null && data != null)
        {
          for(int k = 0; k < szName.Length; k++)
          {
            m_ListBoxMessageDisplay.Items.Add(szName[k]);
            szTmp = "";
            for(int i = 0; i < data[0].Length; i++)
            {
              szTmp += data[k][i].ToString() + " | ";
            }
            m_ListBoxMessageDisplay.Items.Add(szTmp);
          }
        }
      }
    }

    private void btnREAD_work_coord_axis_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string[] szWorkCoordTitleArray = null;
        string szTmp = "";

        // get axes name from cnc
        short result = cnc.READ_work_coord_axis( out szWorkCoordTitleArray );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_work_coord_axis: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display axes name
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(szWorkCoordTitleArray != null)
        {
          for(int k = 0; k < szWorkCoordTitleArray.Length; k++)
          {
            szTmp += szWorkCoordTitleArray[k] + " ";
          }
          m_ListBoxMessageDisplay.Items.Add(szTmp);
        }
      }
    }

    private void btnREAD_work_coord_scope_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string szTmp = "";
        string[] szName = null;
        float[][] data = null;

        // get specific workpiece offset data from cnc
        short result = cnc.READ_work_coord_scope( 5, 10, out szName, out data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_work_coord_scope: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display specific workpiece offset data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(szName != null && data != null)
        {
          for(int k = 0; k < szName.Length; k++)
          {
            m_ListBoxMessageDisplay.Items.Add(szName[k]);
            szTmp = "";
            for(int i = 0; i < data[0].Length; i++)
            {
              szTmp += data[k][i].ToString() + " | ";
            }
            m_ListBoxMessageDisplay.Items.Add(szTmp);
          }
        }
      }
    }

    private void btnREAD_work_coord_single_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        float[] data = null;

        // get one workpiece offset data from cnc
        short result = cnc.READ_work_coord_single( "G54", out data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_work_coord_single: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display one workpiece offset data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("G54");
        string szTmp = "";
        for(int i = 0; i < data.Length; i++)
        {
          szTmp += data[i].ToString() + " | ";
        }
        m_ListBoxMessageDisplay.Items.Add(szTmp);
      }
    }

    private void btnREAD_work_coord_count_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        short nCount = 0;

        // get workpiece count from cnc
        short result = cnc.READ_work_coord_count( out nCount );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_work_coord_count: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display workpiece count
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("WorkPiece Count : " + nCount.ToString());
      }
    }

    private void btnREAD_macro_all_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int[] nIndexArray = null;
        double[] Data = null;

        // get all global data from cnc
        short result = cnc.READ_macro_all( out nIndexArray, out Data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_macro_all: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display all global data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(nIndexArray != null && Data != null)
        {
          for(int i = 0; i <= 20; i++)
          {
            m_ListBoxMessageDisplay.Items.Add(nIndexArray[i].ToString() + " : " + Data[i].ToString());
          }
        }
      }
    }

    private void btnREAD_macro_variable_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int[][] Data = null;

        // get global data start~end index from cnc
        short result = cnc.READ_macro_variable( out Data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_macro_variable: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display global data start~end index
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(Data != null)
        {
          for(int i = 0; i < Data.Length; i++)
          {
            m_ListBoxMessageDisplay.Items.Add(Data[i][0].ToString() + " ~ " + Data[i][1].ToString());
          }
        }
      }
    }

    private void btnREAD_macro_scope_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int[] Name = null;
        double[] Data = null;

        // get specific global data from cnc (@100~@120)
        short result = cnc.READ_macro_scope( 100, 120, out Name, out Data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_macro_scope: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display specific global data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(Name != null && Data != null)
        {
          for(int i = 0; i <= 20; i++)
          {
            m_ListBoxMessageDisplay.Items.Add(Name[i].ToString() + " : " + Data[i].ToString());
          }
        }
      }
    }

    private void btnREAD_macro_single_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        double Data = 0;

        // specific global data from cnc (@110)
        short result = cnc.READ_macro_single( 110, out Data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_macro_single: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display specific global data
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("110 : " + Data.ToString());
      }
    }

    private void btnREAD_param_max_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int nData = 0;

        // get maximum number of parameter from cnc
        short result = cnc.READ_param_max( out nData );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_param_max: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display maximum number of parameter
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Param MAX : " + nData.ToString());
      }
    }

    private void btnREAD_param_data_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int[] data = null;

        // get cnc parameters (P21~P30) from cnc
        short result = cnc.READ_param_data( 21, 30, out data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_param_data: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display cnc parameters (P21~P30)
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(data != null)
        {
          for(int i = 0; i < data.Length; i++)
          {
            m_ListBoxMessageDisplay.Items.Add("Param[" + ((int)(21 + i)).ToString() + "] : " + data[i].ToString());
          }
        }
      }
    }

    private void btnREAD_plc_addr_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        byte[] plcdataB = null;
        int[] plcdataI = null;
        short[] plcdataS = null;
        short type;

        // get R26~R30 from cnc
        short result = cnc.READ_plc_addr( "R", 26, 30, out type, out plcdataB, out plcdataS, out plcdataI );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_plc_addr: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display R26~R30
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(type == 0)
        {
          if(plcdataB != null)
          {
            for(int i = 0; i < plcdataB.Length; i++)
            {
              m_ListBoxMessageDisplay.Items.Add("R" + ((int)(26 + i)).ToString() + " : " + plcdataB[i].ToString());
            }
          }
        }
        else if(type == 2)
        {
          if(plcdataI != null)
          {
            for(int i = 0; i < plcdataI.Length; i++)
            {
              m_ListBoxMessageDisplay.Items.Add("R" + ((int)(26 + i)).ToString() + " : " + plcdataI[i].ToString());
            }
          }
        }
      }
    }

    private void btnREAD_plc_type_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        short data = -1;

        // get "R" storage type from cnc
        short result = cnc.READ_plc_type( "R", out data );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_plc_type: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display "R" storage type
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(data >= 0)
        {
          string msg = "";
          switch(data)
          {
            case 0:
              msg = "Byte";
              break;
            case 1:
              msg = "Short";
              break;
            case 2:
              msg = "Int";
              break;
            default:
              msg = "Unknown";
              break;
          }
          m_ListBoxMessageDisplay.Items.Add("PLC R Type : " + msg);
        }
      }
    }

    private void btnREAD_plc_ver_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string szData = null;

        // get plc version from cnc
        short result = cnc.READ_plc_ver( out szData );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_plc_ver: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display plc version
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(szData != null)
        {
          m_ListBoxMessageDisplay.Items.Add("PLC Version : " + szData);
        }
      }
    }

    private void btnREAD_nc_mem_list_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string[][] files = null;

        // get nc files info. from cnc
        short result = cnc.READ_nc_mem_list( out files );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_nc_mem_list: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display ncfiles info.
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        if(files != null)
        {
          for(int i = 0; i < files.Length; i++)
          {
            m_ListBoxMessageDisplay.Items.Add(files[i][0]);
          }
        }
      }
    }

    private void btnUPLOAD_nc_mem_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();
      OpenFileDialog ofd = new OpenFileDialog();
      if(ofd.ShowDialog() == DialogResult.OK)
      {
        foreach(SyntecRemoteCNC cnc in m_CNC)
        {

          // upload selected file to cnc
          short result = cnc.UPLOAD_nc_mem( ofd.FileName );

          if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
          {
            string szErr = String.Format( "{0}: Error: UPLOAD_nc_mem: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
            m_ListBoxMessageDisplay.Items.Add(szErr);
            continue;
          }

          WaitForUploadFile(cnc);
        }
      }
    }

    private void WaitForUploadFile(SyntecRemoteCNC cnc)
    {
      while(!cnc.isFileUploadDone)
      {
        Thread.Sleep(500);
      }
      if(cnc.FileUploadErrorCode == (short)Syntec.Remote.SyntecRemoteCNC.ErrorCode.NormalTermination)
      {
        MessageBox.Show(cnc.Host + " Upload File Success");
      }
      else
      {
        MessageBox.Show(cnc.Host + " Upload File Failed");
      }
    }

    private void btnDOWNLOAD_nc_mem_Click(object sender, EventArgs e)
    {
      if(m_ListBoxMessageDisplay.SelectedIndex < 0 || m_ListBoxMessageDisplay.Items[m_ListBoxMessageDisplay.SelectedIndex].ToString().IndexOf("===") >= 0)
      {
        MessageBox.Show("please select file");
        return;
      }

      string host = "";
      // find host
      for(int i = m_ListBoxMessageDisplay.SelectedIndex - 1; i >= 0; i--)
      {
        if(m_ListBoxMessageDisplay.Items[i].ToString().IndexOf("===") >= 0)
        {
          host = m_ListBoxMessageDisplay.Items[i].ToString().Replace("===", "");
          break;
        }
      }

      if(host == "")
      {
        MessageBox.Show("didn't find host");
      }

      SyntecRemoteCNC cnc = null;
      //mapping cnc
      foreach(SyntecRemoteCNC tmp in m_CNC)
      {
        if(tmp.Host == host)
        {
          cnc = tmp;
          break;
        }
      }

      if(cnc == null)
      {
        MessageBox.Show("didn't find cnc");
      }

      FolderBrowserDialog fbd = new FolderBrowserDialog();
      if(fbd.ShowDialog() == DialogResult.OK)
      {
        short result = cnc.DOWNLOAD_nc_mem( m_ListBoxMessageDisplay.Items[ m_ListBoxMessageDisplay.SelectedIndex ].ToString(), fbd.SelectedPath );
        if(result == (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          WaitForDownloadFile(cnc);
        }
        else
        {
          m_ListBoxMessageDisplay.Items.Add(cnc.Host + " : Error:" + result.ToString());
        }
      }
    }

    private void WaitForDownloadFile(SyntecRemoteCNC cnc)
    {
      while(!cnc.isFileDownloadDone)
      {
        Thread.Sleep(500);
      }
      if(cnc.FileDownloadErrorCode == (short)Syntec.Remote.SyntecRemoteCNC.ErrorCode.NormalTermination)
      {
        MessageBox.Show(cnc.Host + " Download File Success");
      }
      else
      {
        MessageBox.Show(cnc.Host + " Download File Failed");
      }
    }
    private void btnWRITE_plc_addr_Click(object sender, EventArgs e)
    {
      m_ListBoxMessageDisplay.Items.Clear();

      if(tbWRITE_plc_addr.Text.Length <= 0)
      {
        MessageBox.Show("fill the textbox");
      }

      int start = 10000;
      int end = 10010;

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        byte[] plcdataB = null;
        int[] plcdataI = null;
        short[] plcdataS = null;

        short type;

        int result = cnc.READ_plc_type( "R", out type );
        if(result == 0)
        {
          switch(type)
          {
            case 0:
              plcdataB = new byte[end - start + 1];
              for(int i = start; i <= end; i++)
              {
                plcdataB[i-start] = Convert.ToByte(tbWRITE_plc_addr.Text);
              }
              break;
            case 1:
              plcdataS = new short[end - start + 1];
              for(int i = start; i <= end; i++)
              {
                plcdataS[i - start] = Convert.ToInt16(tbWRITE_plc_addr.Text);
              }
              break;
            case 2:
              plcdataI = new int[end - start + 1];
              for(int i = start; i <= end; i++)
              {
                plcdataI[i - start] = Convert.ToInt32(tbWRITE_plc_addr.Text);
              }
              break;
            default:
              MessageBox.Show("Error");
              return;
          }

          result = cnc.WRITE_plc_addr("R", start, end, type, plcdataB, plcdataS, plcdataI);
        }
        if(result == (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
          m_ListBoxMessageDisplay.Items.Add("Done");
        }
        else
        {
          m_ListBoxMessageDisplay.Items.Add(cnc.Host + " : Error:" + result.ToString());
        }
      }
    }

    private void btnDEL_nc_mem_Click(object sender, EventArgs e)
    {
      if(m_ListBoxMessageDisplay.SelectedIndex < 0 || m_ListBoxMessageDisplay.Items[m_ListBoxMessageDisplay.SelectedIndex].ToString().IndexOf("===") >= 0)
      {
        MessageBox.Show("please select file");
        return;
      }

      string host = "";
      // find host
      for(int i = m_ListBoxMessageDisplay.SelectedIndex - 1; i >= 0; i--)
      {
        if(m_ListBoxMessageDisplay.Items[i].ToString().IndexOf("===") >= 0)
        {
          host = m_ListBoxMessageDisplay.Items[i].ToString().Replace("===", "");
          break;
        }
      }

      if(host == "")
      {
        MessageBox.Show("didn't find host");
      }

      SyntecRemoteCNC cnc = null;
      //mapping cnc
      foreach(SyntecRemoteCNC tmp in m_CNC)
      {
        if(tmp.Host == host)
        {
          cnc = tmp;
          break;
        }
      }

      if(cnc == null)
      {
        MessageBox.Show("didn't find cnc");
      }

      short result = cnc.DEL_nc_mem( m_ListBoxMessageDisplay.Items[ m_ListBoxMessageDisplay.SelectedIndex ].ToString() );
      if(result == (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
      {
        MessageBox.Show(cnc.Host + " :Done");
      }
      else
      {
        MessageBox.Show(cnc.Host + " : Error:" + result.ToString());
      }
    }

    private void btnREAD_nc_pointer_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        int nLineNo = 0;

        // get current program line no. from cnc
        short result = cnc.READ_nc_pointer( out nLineNo );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_nc_pointer: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display current program line no.
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("LineNo : " + nLineNo.ToString());
      }
    }

    private void btnWRITE_relpos_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {

        // set X relative position to cnc
        short result = cnc.WRITE_relpos( "X", Convert.ToDouble( tbWRITE_relpos.Text ) );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: WRITE_relpos: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display results
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Write relpos Success");
      }
    }

    private void btnIsUSBExist_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("isUSBExist: " + cnc.isUSBExist().ToString());
      }
    }

    private void btnMainBoard_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("MainBoard: " + cnc.MainBoardPlatformName);
      }
    }

    private void btn_ReadUseTime_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        string szStatus = "";
        string szTimeStart = "";
        string szTimeExpire = "";
        int nTimeRemain = -1;

        // get expiration date from cnc
        short result = cnc.READ_useTime( out szStatus, out szTimeStart, out szTimeExpire, out nTimeRemain );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_useTime: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display expiration date
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Status: " + szStatus);
        m_ListBoxMessageDisplay.Items.Add("Time Start: " + szTimeStart);
        m_ListBoxMessageDisplay.Items.Add("Time Expire: " + szTimeExpire);
        m_ListBoxMessageDisplay.Items.Add("Time Remain: " + nTimeRemain.ToString() + " hr");
      }
    }

    private void btnReadRemoteTime_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        DateTime dt = new DateTime();

        // get system time from cnc
        short result = cnc.READ_remoteTime( out dt );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_remoteTime: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display system time of cnc
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Remote Date Time: " + dt.ToString("yyyy/MM/dd HH:mm:ss"));
      }
    }

    private void btnWriteRemoteDate_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {

        // set system date to cnc
        short result = cnc.WRITE_remoteDate( Convert.ToInt32( tbYear.Text ), Convert.ToInt32( tbMonth.Text ), Convert.ToInt32( tbDay.Text ) );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: WRITE_remoteDate: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Write Remote Date Success!");
      }
    }

    private void btnWriteRemoteTime_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {

        // set system time to cnc
        short result = cnc.WRITE_remoteTime( Convert.ToInt32( tbHour.Text ), Convert.ToInt32( tbMinute.Text ), Convert.ToInt32( tbSecond.Text ) );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: WRITE_remoteTime: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("Write Remote Time Success!");
      }
    }

    private void btnReadDiskCFreeSpace_Click(object sender, EventArgs e)
    {
      // clear message display
      m_ListBoxMessageDisplay.Items.Clear();

      foreach(SyntecRemoteCNC cnc in m_CNC)
      {
        long nSpace = -1;

        // get diskC free space from cnc
        short result = cnc.READ_diskCFreeSpace( out nSpace );

        if(result != (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          string szErr = String.Format( "{0}: Error: READ_diskCFreeSpace: {1}({2})", cnc.Host, (SyntecRemoteCNC.ErrorCode)result, result );
          m_ListBoxMessageDisplay.Items.Add(szErr);
          continue;
        }

        // display diskC free space
        m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
        m_ListBoxMessageDisplay.Items.Add("DiskC Free Space: " + nSpace.ToString());
      }
    }

    private void btnUpgrade_Click(object sender, EventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "Software(package_*.zip)|package_*.zip|Parameter(param.dat)|param.dat|Ladder(cnc.lad)|cnc.lad";

      if(ofd.ShowDialog() != DialogResult.OK)
      {
        return;
      }

      m_ListBoxMessageDisplay.Items.Clear();
      foreach(SyntecRemoteCNC cnc in m_CNC)
      {

        short result = -1;

        string EXT = System.IO.Path.GetExtension( ofd.FileName ).ToUpper();

        switch(EXT)
        {
          case ".ZIP":
            result = cnc.UPLOAD_software(ofd.FileName);
            break;
          case ".DAT":
            result = cnc.UPLOAD_param_file(ofd.FileName);
            break;
          case ".LAD":
            result = cnc.UPLOAD_plc_file(ofd.FileName);
            break;
          default:
            break;
        }

        if(result == (short)SyntecRemoteCNC.ErrorCode.NormalTermination)
        {
          m_ListBoxMessageDisplay.Items.Add("===" + cnc.Host + "===");
          m_ListBoxMessageDisplay.Items.Add("Upload " + System.IO.Path.GetFileName(ofd.FileName) + " Success!");
          m_ListBoxMessageDisplay.Items.Add("Please Reboot to Upgrade!");
        }
        else
        {
          m_ListBoxMessageDisplay.Items.Add(cnc.Host + " : Error:" + result.ToString());
        }
      }
    }
  }
}
