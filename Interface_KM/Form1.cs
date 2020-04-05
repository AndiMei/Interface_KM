using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Interface_KM
{
    public partial class Form1 : Form
    {
        string Clock;
        private const int READ_BUFFER_SIZE = 2048; //2kB
        private const int WRITE_BUFFER_SIZE = 2048; //2kB
        private byte[] bufferReceiver = null;
        private byte[] bufferSender = null;
        private Socket mSocket = null;
        
        bool btnState;
        bool ambilData;
        string dataApa, dataApa2;
        uint time;
        bool clrChart = false;

        /* Global variable data */
        double vR, vS, vT;
        double vRS, vST, vTR;
        double cR, cS, cT;
        double freq;
        double PF;
        double Ptot, Qtot;
        double kWhtot, KVARtot;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.Text = "Connect";
            dataApa = "current";
            dataApa2 = "total_kWh";
            strUnit.Text = "A";
            str_RR.Visible = true;
            str_SS.Visible = true;
            str_TT.Visible = true;
            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;
            strR.Visible = true;
            strS.Visible = true;
            strT.Visible = true;
            timer3.Start();
            radioButton2.Checked = true;
            btn_I.Enabled = false;
            clearChart();
        }

        public void connect(string IP, int Port)
        {
            this.mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.bufferReceiver = new byte[READ_BUFFER_SIZE];
            this.bufferSender = new byte[WRITE_BUFFER_SIZE];
            this.mSocket.SendBufferSize = READ_BUFFER_SIZE;
            this.mSocket.ReceiveBufferSize = WRITE_BUFFER_SIZE;
            IPEndPoint server = new IPEndPoint(IPAddress.Parse(IP), Port);
            this.mSocket.Connect(server);
        }

        private int write(byte[] frame)
        {
            return (this.mSocket.Send(frame, frame.Length, SocketFlags.None));
        }

        private byte[] read()
        {
            byte[] result = new byte[READ_BUFFER_SIZE];
            NetworkStream netStream = new NetworkStream(this.mSocket);
            if (netStream.CanRead)
            {
                this.mSocket.Receive(result, result.Length, SocketFlags.None);
            }
            return result;
        }

        private int[] read_HoldingRegisters(ushort id, byte slaveAddress, ushort startAddress, byte functionCode, ushort numberOfPoint)
        {
            var frame = new byte[12]; // Total 12 Bytes
            frame[0] = Convert.ToByte(id / (double)256); // Transaction Identifier High
            frame[1] = Convert.ToByte(id % 256); // Transaction Identifier Low
            frame[2] = 0; // Protocol Identifier High
            frame[3] = 0; // Protocol Identifier Low
            frame[4] = 0; // Message Length High.
            frame[5] = 6; // Message Length Low(6 bytes to follow)
            frame[6] = slaveAddress; // The Unit Identifier(slave Address/Slave Id).
            frame[7] = functionCode; // Function.
            frame[8] = Convert.ToByte(startAddress / (double)256); // Starting Address High.
            frame[9] = Convert.ToByte(startAddress % 256); // Starting Address Low.
            frame[10] = Convert.ToByte(numberOfPoint / (double)256); // Quantity of Registers High
            frame[11] = Convert.ToByte(numberOfPoint % 256); // Quantity of Registers Low

            this.write(frame);  //Send message to device.
            Thread.Sleep(100);  //Delay 100ms.

            /*  Receive data    */
            byte[] buffRX = this.read();
            int sizeByte = buffRX[8];
            byte[] byteData = new byte[sizeByte];
            int[] temp = new int[(byteData.Length / 2)];

            if (functionCode == buffRX[7])
            {
                Array.Copy(buffRX, 9, byteData, 0, byteData.Length);

                /* Convert byte[] to ushort[] */
                int j = 0;
                for (int i = 1; i < byteData.Length; i += 2)
                {
                    temp[j] = ((byteData[i - 1] << 8) | byteData[i]);
                    j++;
                }
            }
            return temp;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            btnState = !(btnState);
            if (btnState)
            {
                try
                {
                    button1.Text = "Disconnect";
                    button1.BackColor = Color.Green;
                    connect(txtHost.Text, 502); // connect to device.
                    timer1.Start();
                    timer2.Start();
                    timer4.Start();
                    txtAddr.Text = txtSlave.Value.ToString("00");
                    ambilData = true;
                    txtSlave.Enabled = false;
                    txtHost.Enabled = false;
                }

                catch (Exception er)
                {
                    btnState = false;
                    button1.Text = "Connect";
                    button1.BackColor = Color.Red;
                    txtSlave.Enabled = true;
                    txtHost.Enabled = true;
                    timer1.Stop();
                    timer2.Stop();
                    timer4.Stop();
                    MessageBox.Show(er.Message);
                }
            }

            else
            {
                button1.Text = "Connect";
                button1.BackColor = Color.Red;
                ambilData = false;
                btnState = false;
                button1.Text = "Connect";
                txtSlave.Enabled = true;
                txtHost.Enabled = true;
                timer1.Stop();
                timer2.Stop();
                timer4.Stop();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            calculate();
            if (ambilData)
                GetData();
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            showChart(dataApa, Convert.ToDouble(strR.Text), Convert.ToDouble(strS.Text), Convert.ToDouble(strT.Text));
        }
        private void timer3_Tick(object sender, EventArgs e)
        {
            Jam.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            string time = DateTime.Now.ToString("hh:mm:ss");
            string date = DateTime.Now.ToString("ddMMyyyy");

            string headerTeks = "Time,cR,cS,CT,vR,vS,vT,vRS,vST,vTR,freq,PF,pTot,qTot,kWhtot\n";
            string logTeks = time + "," + cR.ToString("#,##0.00") + "," + cS.ToString("#,##0.00") + "," + cT.ToString("#,##0.00") + "," + vR.ToString("#,##0.0") + "," + vS.ToString("#,##0.0") + "," + vT.ToString("#,##0.0") + "," + vRS.ToString("#,##0.0") + "," + vST.ToString("#,##0.0") + "," + vTR.ToString("#,##0.0") + "," + freq.ToString("#,##0.00") + "," + PF.ToString("#,##0.00") + "," + Ptot.ToString("#,##0.000") + "," + Qtot.ToString("#,##0.000") + "," + kWhtot.ToString("#,#0.0") + Environment.NewLine;
            string namaFile = date;

            writeCSV(namaFile, logTeks, headerTeks);
        }

        private void GetData()
        {
            try
            {
                byte slaveAddress = (byte)txtSlave.Value;
                byte functionCode = 3;  //3 = holding register.
                ushort id = functionCode;

                /* read holding register to buffer */
                int[] readHoldRegisters = read_HoldingRegisters(id, slaveAddress, 0, functionCode, 27);
                int[] readHoldRegistersTotalizer = read_HoldingRegisters(id, slaveAddress, 608, functionCode, 11);

                /* Copy buffer to variable */
                vR = (readHoldRegisters[1] * 0.1);
                vS = (readHoldRegisters[3] * 0.1);
                vT = (readHoldRegisters[5] * 0.1);
                vRS = (readHoldRegisters[21] * 0.1);
                vST = (readHoldRegisters[23] * 0.1);
                vTR = (readHoldRegisters[25] * 0.1);
                cR = (readHoldRegisters[7] * 0.001);
                cS = (readHoldRegisters[9] * 0.001);
                cT = (readHoldRegisters[11] * 0.001);
                PF = (readHoldRegisters[13] * 0.01);
                freq = (readHoldRegisters[15] * 0.1);
                Ptot = (((readHoldRegisters[16] << 16) + readHoldRegisters[17]) * 0.0001);
                Qtot = (((readHoldRegisters[18] << 16) + readHoldRegisters[19]) * 0.0001);
                kWhtot = (((readHoldRegistersTotalizer[0] << 16) + readHoldRegistersTotalizer[1]));
                KVARtot = (((readHoldRegistersTotalizer[8] << 16) + readHoldRegistersTotalizer[9]));

                /* Show data */
                switch (dataApa)
                {
                    case "voltage_1ph":
                        strR.Text = vR.ToString("#,##0.0");
                        strS.Text = vS.ToString("#,##0.0");
                        strT.Text = vT.ToString("#,##0.0");
                        break;

                    case "current":
                        strR.Text = cR.ToString("#,##0.00");
                        strS.Text = cS.ToString("#,##0.00");
                        strT.Text = cT.ToString("#,##0.00");
                        break;

                    case "powerFactor":
                        strR.Text = PF.ToString("#,##0.00");
                        break;

                    case "frequency":
                        strR.Text = freq.ToString("#,##0.00");
                        break;

                    case "power":
                        strR.Text = Ptot.ToString("#,##0.000");
                        strTotal.Text =kWhtot.ToString("#,#0.0");
                        strUnit2.Text = "kWh";
                        dataApa2 = "total_kWh";
                        break;

                    case "reactivePower":
                        strR.Text = Qtot.ToString("#,##0.000");
                        strTotal.Text = KVARtot.ToString("#,#0.0");
                        strUnit2.Text = "kVARh";
                        dataApa2 = "total_kVARh";
                        break;

                    case "voltage_3ph":
                        strR.Text = vRS.ToString("#,##0.0");
                        strS.Text = vST.ToString("#,##0.0");
                        strT.Text = vTR.ToString("#,##0.0");
                        break;
                }

                switch(dataApa2)
                {
                    case "total_kWh":
                        strTotal.Text = kWhtot.ToString("#,#0.0");
                        strUnit2.Text = "kWh";
                        break;

                    case "total_kVARh":
                        strTotal.Text = KVARtot.ToString("#,#0.0");
                        strUnit2.Text = "kVARh";
                        break;

                }

                
            }
            catch(Exception ex)
            {
                ambilData = false;
                btnState = false;
                button1.Text = "Connect";
                txtSlave.Enabled = true;
                txtHost.Enabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void showChart(string selector, double valR, double valS, double valT)
        {
            double max = 0, min = 65535;
            switch (selector)
            {
                case "voltage_1ph":
                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    };

                    chart1.Series[0].LegendText = "R";
                    chart1.Series[1].LegendText = "S";
                    chart1.Series[2].LegendText = "T";
                    /* auto scale X axis */
                    if ((valR > max) || (valS > max) || (valT > max))
                        max = ((valR + valS + valT) / 3) + 10;
                    if ((valR < min) || (valS < min) || (valT < min))
                        min = ((valR + valS + valT) / 3) - 10;
                    if (min < 1)
                        min = 0;

                    chart1.ChartAreas[0].AxisY.Maximum = (int) max;
                    chart1.ChartAreas[0].AxisY.Minimum = (int) min;

                    if(chart1.Series[0].Points.Count > 10)
                    {
                        
                        chart1.ChartAreas[0].AxisX.Minimum = time;
                        chart1.ChartAreas[0].AxisX.Maximum = 10 + time;
                        time++;
                    }
                    chart1.Series[0].Points.AddY(valR);
                    chart1.Series[1].Points.AddY(valS);
                    chart1.Series[2].Points.AddY(valT);
                    break;

                case "current":
                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    }
                    chart1.Series[0].LegendText = "R";
                    chart1.Series[1].LegendText = "S";
                    chart1.Series[2].LegendText = "T";
                    /* auto scale X axis */
                    if ((valR > max) || (valS > max) || (valT > max))
                        max = ((valR + valS + valT) / 3) + 10;
                    if ((valR < min) || (valS < min) || (valT < min))
                        min = ((valR + valS + valT) / 3) - 10;
                    if (min < 1)
                        min = 0;

                    chart1.ChartAreas[0].AxisY.Maximum = (int) max;
                    chart1.ChartAreas[0].AxisY.Minimum = (int) min;
                    if (chart1.Series[0].Points.Count > 10)
                    {
                        chart1.ChartAreas[0].AxisX.Minimum = time;
                        chart1.ChartAreas[0].AxisX.Maximum = 8 + time;
                        time++;
                    }
                    chart1.Series[0].Points.AddY(valR);
                    chart1.Series[1].Points.AddY(valS);
                    chart1.Series[2].Points.AddY(valT);
                    break;

                case "powerFactor":
                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    }
                    /* auto scale X axis */
                    chart1.Series[0].LegendText = "PF";
                    chart1.Series[1].LegendText = "0";
                    chart1.Series[2].LegendText = "0";
                    chart1.ChartAreas[0].AxisY.Maximum = 1;
                    chart1.ChartAreas[0].AxisY.Minimum = 0;
                    if (chart1.Series[0].Points.Count > 10)
                    {
                        chart1.ChartAreas[0].AxisX.Minimum = time;
                        chart1.ChartAreas[0].AxisX.Maximum = 8 + time;
                        time++;
                    }
                    chart1.Series[0].Points.AddY(valR);
                    chart1.Series[1].Points.AddY(0);
                    chart1.Series[2].Points.AddY(0);
                    break;

                case "frequency":
                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    }
                    chart1.Series[0].LegendText = "Freq";
                    chart1.Series[1].LegendText = "0";
                    chart1.Series[2].LegendText = "0";
                    /* auto scale X axis */
                    if (valR > max)
                        max = valR + 5;
                    if (valR < min)
                        min = valR - 5;
                    if (min < 1)
                        min = 0;

                    chart1.ChartAreas[0].AxisY.Maximum = (int) max;
                    chart1.ChartAreas[0].AxisY.Minimum = (int) min; 
                    if (chart1.Series[0].Points.Count > 10)
                    {
                        chart1.ChartAreas[0].AxisX.Minimum = time;
                        chart1.ChartAreas[0].AxisX.Maximum = 8 + time;
                        time++;
                    }
                    chart1.Series[0].Points.AddY(valR);
                    chart1.Series[1].Points.AddY(0);
                    chart1.Series[2].Points.AddY(0);
                    break;

                case "power":
                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    }
                    chart1.Series[0].LegendText = "P";
                    chart1.Series[1].LegendText = "0";
                    chart1.Series[2].LegendText = "0";
                    /* auto scale X axis */
                    if (valR > max)
                        max = valR + 10;
                    if (valR < min)
                        min = valR - 10;
                    if (min < 1)
                        min = 0;

                    chart1.ChartAreas[0].AxisY.Maximum = (int)max;
                    chart1.ChartAreas[0].AxisY.Minimum = (int)min;
                    if (chart1.Series[0].Points.Count > 10)
                    {
                        chart1.ChartAreas[0].AxisX.Minimum = time;
                        chart1.ChartAreas[0].AxisX.Maximum = 8 + time;
                        time++;
                    }
                    chart1.Series[0].Points.AddY(valR);
                    chart1.Series[1].Points.AddY(0);
                    chart1.Series[2].Points.AddY(0);
                    break;

                case "reactivePower":
                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    }
                    chart1.Series[0].LegendText = "Q";
                    chart1.Series[1].LegendText = "0";
                    chart1.Series[2].LegendText = "0";
                    /* auto scale X axis */
                    if (valR > max)
                        max = valR + 5;
                    if (valR < min)
                        min = valR - 5;

                    chart1.ChartAreas[0].AxisY.Maximum = (int)max;
                    chart1.ChartAreas[0].AxisY.Minimum = (int)min;
                    if (chart1.Series[0].Points.Count > 10)
                    {
                        chart1.ChartAreas[0].AxisX.Minimum = time;
                        chart1.ChartAreas[0].AxisX.Maximum = 8 + time;
                        time++;
                    }
                    chart1.Series[0].Points.AddY(valR);
                    chart1.Series[1].Points.AddY(0);
                    chart1.Series[2].Points.AddY(0);

                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    }
                    break;

                case "voltage_3ph":
                    if (clrChart)
                    {
                        clrChart = false;
                        clearChart();
                    }
                    chart1.Series[0].LegendText = "RS";
                    chart1.Series[1].LegendText = "ST";
                    chart1.Series[2].LegendText = "TR";
                    /* auto scale X axis */
                    if ((valR > max) || (valS > max) || (valT > max))
                        max = ((valR + valS + valT) / 3) + 10;
                    if ((valR < min) || (valS < min) || (valT < min))
                        min = ((valR + valS + valT) / 3) - 10;
                    if (min < 1)
                        min = 0;

                    chart1.ChartAreas[0].AxisY.Maximum = (int)max;
                    chart1.ChartAreas[0].AxisY.Minimum = (int)min;

                    if (chart1.Series[0].Points.Count > 10)
                    {

                        chart1.ChartAreas[0].AxisX.Minimum = time;
                        chart1.ChartAreas[0].AxisX.Maximum = 8 + time;
                        time++;
                    }
                    chart1.Series[0].Points.AddY(valR);
                    chart1.Series[1].Points.AddY(valS);
                    chart1.Series[2].Points.AddY(valT);
                    break;
            }
        }

        private void clearChart()
        {
            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();
            chart1.Series[2].Points.Clear();
            chart1.ChartAreas[0].AxisX.Maximum = 10;
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            time = 0;
        }

        private void writeCSV(string nama, string teks,string header)
        {
            string fileName = nama + ".csv";
            string dir = Directory.GetCurrentDirectory();
            string cureFile = @dir + @"\" + fileName;
            if (!File.Exists(cureFile))//if file doesn't exist
            {
                File.Create(cureFile).Close();
                File.AppendAllText(cureFile, header);
            }
            try
            {
                File.AppendAllText(cureFile, teks);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + Environment.NewLine + "Can't access" + cureFile + Environment.NewLine + "This file maybe used by another application.", "Write File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

       

        private void btn_I_Click(object sender, EventArgs e)
        {
            dataApa = "current";
            strUnit.Text = "A";
            str_RR.Visible = true;
            str_SS.Visible = true;
            str_TT.Visible = true;
            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;
            strR.Visible = true;
            strS.Visible = true;
            strT.Visible = true;
            clrChart = true;
            /* Button */
            btn_I.Enabled = false;
            btn_1ph.Enabled = true;
            btn_3ph.Enabled = true;
            btn_f.Enabled = true;
            btn_pf.Enabled = true;
            btn_p.Enabled = true;
            btn_q.Enabled = true;
        }

        private void btn_1ph_Click(object sender, EventArgs e)
        {
            dataApa = "voltage_1ph";
            strUnit.Text = "V";
            str_RR.Visible = true;
            str_SS.Visible = true;
            str_TT.Visible = true;
            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;
            strR.Visible = true;
            strS.Visible = true;
            strT.Visible = true;
            clrChart = true;
            /* Button */
            btn_I.Enabled = true;
            btn_1ph.Enabled = false;
            btn_3ph.Enabled = true;
            btn_f.Enabled = true;
            btn_pf.Enabled = true;
            btn_p.Enabled = true;
            btn_q.Enabled = true;

        }

        private void btn_3ph_Click(object sender, EventArgs e)
        {
            dataApa = "voltage_3ph";
            strUnit.Text = "V";
            str_RR.Visible = true;
            str_SS.Visible = true;
            str_TT.Visible = true;
            str_RS.Visible = true;
            str_ST.Visible = true;
            str_TR.Visible = true;
            strR.Visible = true;
            strS.Visible = true;
            strT.Visible = true;
            clrChart = true;
            /* Button */
            btn_I.Enabled = true;
            btn_1ph.Enabled = true;
            btn_3ph.Enabled = false;
            btn_f.Enabled = true;
            btn_pf.Enabled = true;
            btn_p.Enabled = true;
            btn_q.Enabled = true;
        }

        private void btn_f_Click(object sender, EventArgs e)
        {
            dataApa = "frequency";
            strUnit.Text = "Hz";
            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;
            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
            clrChart = true;
            /* Button */
            btn_I.Enabled = true;
            btn_1ph.Enabled = true;
            btn_3ph.Enabled = true;
            btn_f.Enabled = false;
            btn_pf.Enabled = true;
            btn_p.Enabled = true;
            btn_q.Enabled = true;
        }

        private void btn_pf_Click(object sender, EventArgs e)
        {
            dataApa = "powerFactor";
            strUnit.Text = "PF";
            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;
            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
            clrChart = true;
            /* Button */
            btn_I.Enabled = true;
            btn_1ph.Enabled = true;
            btn_3ph.Enabled = true;
            btn_f.Enabled = true;
            btn_pf.Enabled = false;
            btn_p.Enabled = true;
            btn_q.Enabled = true;
        }

        private void btn_p_Click(object sender, EventArgs e)
        {
            dataApa = "power";
            strUnit.Text = "kW";
            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;
            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
            clrChart = true;
            /* Button */
            btn_I.Enabled = true;
            btn_1ph.Enabled = true;
            btn_3ph.Enabled = true;
            btn_f.Enabled = true;
            btn_pf.Enabled = true;
            btn_p.Enabled = false;
            btn_q.Enabled = true;
        }

        private void btn_q_Click(object sender, EventArgs e)
        {
            dataApa = "reactivePower";
            strUnit.Text = "kvar";
            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;
            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
            clrChart = true;
            /* Button */
            btn_I.Enabled = true;
            btn_1ph.Enabled = true;
            btn_3ph.Enabled = true;
            btn_f.Enabled = true;
            btn_pf.Enabled = true;
            btn_p.Enabled = true;
            btn_q.Enabled = false;
        }
        
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                panel_1.Show();
                panel_2.Show();
                panel_3.Show();
             }

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                panel_1.Show();
                panel_2.Show();
                panel_3.Hide();
            }
        }

        private void calculate()
        {
            try
            {
                double a = Convert.ToDouble(Clock);
                double b = Convert.ToDouble(lwbp_jjAwal.Value.ToString() + lwbp_mmAwal.Value.ToString());
                double c = Convert.ToDouble(lwbp_jjAkhir.Value.ToString() + lwbp_mmAkhir.Value.ToString());
                double d = Convert.ToDouble(wbp_jjAwal.Value.ToString() + wbp_mmAwal.Value.ToString());
                double e = Convert.ToDouble(wbp_jjAkhir.Value.ToString() + wbp_mmAkhir.Value.ToString());
                double ConvCC_LWBP = Convert.ToDouble(CC_LWBP.Text);
                double ConvRp_LWBP = Convert.ToDouble(Rp_LWBP.Text);
                double ConvCC_WBP = Convert.ToDouble(CC_WBP.Text);
                double ConvRp_WBP = Convert.ToDouble(Rp_WBP.Text);
                if (a <= c && a >= b)
                {
                    CC_LWBP.Text = kWhtot.ToString("#,##0.0");

                }
                else if (a >= d && a <= e)
                {
                    CC_WBP.Text = kWhtot.ToString("#,##0.0");
                }
                double lwbpTot = ConvRp_LWBP * ConvCC_LWBP;
                LWBP_Duwek.Text = "Rp. " + lwbpTot.ToString("#,##0.00");
                double wbpTot = ConvRp_WBP * ConvCC_WBP;
                WBP_Duwek.Text = "Rp. " + wbpTot.ToString("#,##0.00");

                double duwektotal = lwbpTot + wbpTot;
                Total_Duwek.Text = "Rp. " + duwektotal.ToString("#,##0.00");
            }
            catch(Exception er)
            {
                

            }
            
        }
        /* wekke apsaccgcg */


        
    }
}
