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
using SimpleTCP.Server;
using SimpleTCP;
using EasyModbus;
using System.Threading;

namespace Interface_KM
{
    public partial class Form1 : Form
    {
        private const int READ_BUFFER_SIZE = 2048; //2kB
        private const int WRITE_BUFFER_SIZE = 2048; //2kB
        private byte[] bufferReceiver = null;
        private byte[] bufferSender = null;
        private Socket mSocket = null;
        
        bool btnState;
        bool ambilData;
        string dataApa;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.Text = "Connect";
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
                    connect(txtHost.Text, 502); // connect to device.
                    timer1.Start();
                    ambilData = true;
                    txtSlave.Enabled = false;
                    txtHost.Enabled = false;
                }

                catch (Exception er)
                {
                    btnState = false;
                    button1.Text = "Connect";
                    txtSlave.Enabled = true;
                    txtHost.Enabled = true;
                    MessageBox.Show(er.Message);
                }
            }

            else
            {
                button1.Text = "Connect";
                ambilData = false;
                btnState = false;
                button1.Text = "Connect";
                txtSlave.Enabled = true;
                txtHost.Enabled = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (ambilData)
                GetData();
        }

        private void GetData()
        {
            try
            {
                byte slaveAddress = (byte)txtSlave.Value;
                byte functionCode = 3;
                ushort id = functionCode;
                ushort startAddress = 0;
                ushort numberofPoints = 38;

                int[] readHoldRegisters = read_HoldingRegisters(id, slaveAddress, startAddress, functionCode, numberofPoints);
                int[] readHoldRegistersTotalizer = read_HoldingRegisters(id, slaveAddress, 608, functionCode, 5);

                textBox1.Text = null;
                textBox1.Text = readHoldRegistersTotalizer[0].ToString() + "|" + readHoldRegistersTotalizer[1].ToString();


                switch (dataApa)
                {
                    case "voltage_1ph":
                        strR.Text = (readHoldRegisters[1] * 0.1).ToString();
                        strS.Text = (readHoldRegisters[3] * 0.1).ToString();
                        strT.Text = (readHoldRegisters[5] * 0.1).ToString();
                        break;

                    case "current":
                        strR.Text = (readHoldRegisters[7] * 0.001).ToString();
                        strS.Text = (readHoldRegisters[9] * 0.001).ToString();
                        strT.Text = (readHoldRegisters[11] * 0.001).ToString();
                        break;

                    case "powerFactor":
                        strR.Text = (readHoldRegisters[13] * 0.01).ToString();
                        break;

                    case "frequency":
                        strR.Text = (readHoldRegisters[15] * 0.1).ToString();
                        break;

                    case "power":
                        strR.Text = (readHoldRegisters[17] * 0.1).ToString();
                        strTotal.Text = (readHoldRegisters[1].ToString());
                        break;

                    case "reactivePower":
                        strR.Text = (readHoldRegisters[19] * 0.1).ToString();
                        break;

                    case "voltage_3ph":
                        strR.Text = (readHoldRegisters[21] * 0.1).ToString();
                        strS.Text = (readHoldRegisters[23] * 0.1).ToString();
                        strT.Text = (readHoldRegisters[25] * 0.1).ToString();
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

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
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

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
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

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
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

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
        }
        
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                panel_1.Show();
                panel_2.Hide();
            }
                
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                panel_1.Hide();
                panel_2.Show();
            }
        }

        

        
    }
}
