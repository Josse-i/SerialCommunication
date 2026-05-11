using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SerialCommunication
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timerOefening5.Tick += timerOefening5_Tick;
            timerOefening5.Interval = 1000;
            timerOefening5.Start();
            try
            {
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();
                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;
                comboBoxBaudrate.SelectedIndex = comboBoxBaudrate.Items.IndexOf("115200");
            }
            catch (Exception)
            { }
        }

        private void cboPoort_DropDown(object sender, EventArgs e)
        {
            try
            {
                string selected = (string)comboBoxPoort.SelectedItem;
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();

                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);

                comboBoxPoort.SelectedIndex = comboBoxPoort.Items.IndexOf(selected);
            }
            catch (Exception)
            {
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {

                if (serialPortArduino.IsOpen)
                {
                    // ik heb een verbinding --> de gebruiker wil deze verbreken
                    serialPortArduino.Close();
                    radioButtonVerbonden.Checked = false;
                    buttonConnect.Text = "Connect";
                    labelStatus.Text = "Status: Disconnected";
                }
                else
                {
                    // ik heb geen verbinding --> de gebruiker wil een verbinding maken
                    serialPortArduino.PortName = (string)comboBoxPoort.SelectedItem;
                    serialPortArduino.BaudRate = Int32.Parse((string)comboBoxBaudrate.SelectedItem);
                    serialPortArduino.DataBits = (int)numericUpDownDatabits.Value;

                    if (radioButtonParityEven.Checked) serialPortArduino.Parity = Parity.Even;
                    else if (radioButtonParityOdd.Checked) serialPortArduino.Parity = Parity.Odd;
                    else if (radioButtonHandshakeNone.Checked) serialPortArduino.Parity = Parity.None;
                    else if (radioButtonParityMark.Checked) serialPortArduino.Parity = Parity.Mark;
                    else if (radioButtonParitySpace.Checked) serialPortArduino.Parity = Parity.Space;

                    if (radioButtonStopbitsNone.Checked) serialPortArduino.StopBits = StopBits.None;
                    else if (radioButtonStopbitsOne.Checked) serialPortArduino.StopBits = StopBits.One;
                    else if (radioButtonStopbitsOnePointFive.Checked) serialPortArduino.StopBits = StopBits.OnePointFive;
                    else if (radioButtonStopbitsTwo.Checked) serialPortArduino.StopBits = StopBits.Two;

                    if (radioButtonHandshakeNone.Checked) serialPortArduino.Handshake = Handshake.None;
                    else if (radioButtonHandshakeRTS.Checked) serialPortArduino.Handshake = Handshake.RequestToSend;
                    else if (radioButtonHandshakeRTSXonXoff.Checked) serialPortArduino.Handshake = Handshake.RequestToSendXOnXOff;
                    else if (radioButtonHandshakeXonXoff.Checked) serialPortArduino.Handshake = Handshake.XOnXOff;

                    serialPortArduino.RtsEnable = checkBoxRtsEnable.Checked;
                    serialPortArduino.DtrEnable = checkBoxDtrEnable.Checked;

                    serialPortArduino.Open();
                    serialPortArduino.ReadTimeout = 1000;
                    string commando = "ping";
                    serialPortArduino.WriteLine(commando);
                    string antwoord = serialPortArduino.ReadLine();
                    antwoord = antwoord.TrimEnd();
                    if (antwoord == "pong")
                    {
                        radioButtonVerbonden.Checked = true;
                        buttonConnect.Text = "Disconnect";
                        labelStatus.Text = "Status: Connected";
                        timerOefening5.Interval = 1000;
                        timerOefening5.Start();
                    }
                    else
                    {
                        serialPortArduino.Close();
                        labelStatus.Text = "Error: verkeerd antwoord";
                    }
                }
            }
            catch (Exception exception)
            {
                labelStatus.Text = "Error: " + exception.Message;
                serialPortArduino.Close();
                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
            }
        }

        private void timerOefening5_Tick(object sender, EventArgs e)
        {
            if (!serialPortArduino.IsOpen)
            {
                labelGewensteTemp.Text = "Geen verbinding";
                labelHuidigeTemp.Text = "Geen verbinding";
                return;
            }

            try
            {
                serialPortArduino.DiscardInBuffer();
                serialPortArduino.DiscardOutBuffer();


                // Gewenste temperatuur - A0
                double rc_gewenst = (45.0 - 5.0) / (1023.0 - 0.0);
                double offset_gewenst = 5.0;

                serialPortArduino.WriteLine("get a0");
                string antwoordA0 = serialPortArduino.ReadLine().Trim();
                // antwoord is "a0: 512"
                int waardeA0 = int.Parse(antwoordA0.Split(':')[1].Trim());
                double tempGewenst = rc_gewenst * waardeA0 + offset_gewenst;
                labelGewensteTemp.Text = $"{Math.Round(tempGewenst, 1)} Â°C";

                // Huidige temperatuur - A1
                double rc_huidig = (500.0 - 0.0) / (1023.0 - 0.0);

                serialPortArduino.WriteLine("get a1");
                string antwoordA1 = serialPortArduino.ReadLine().Trim();
                int waardeA1 = int.Parse(antwoordA1.Split(':')[1].Trim());
                double tempHuidig = rc_huidig * waardeA1;
                labelHuidigeTemp.Text = $"{Math.Round(tempHuidig, 1)} Â°C";

                // LED aansturen - pin 2
                if (tempHuidig < tempGewenst)
                    serialPortArduino.WriteLine("set d2 1");
                else
                    serialPortArduino.WriteLine("set d2 0");
            }
            catch (TimeoutException)
            {
                labelGewensteTemp.Text = "Timeout";
                labelHuidigeTemp.Text = "Timeout";
            }
            catch (Exception ex)
            {
                labelGewensteTemp.Text = "Error";
                labelHuidigeTemp.Text = $"Error: {ex.Message}";
            }
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPageOefening5)
            {
                timerOefening5.Interval = 1000;
                timerOefening5.Start();
            }
            else
            {
                timerOefening5.Stop();
            }
        }
    }
}
