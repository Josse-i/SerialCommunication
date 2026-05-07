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
                    // verbinding -> en gebruik wil verbreken
                    serialPortArduino.Close();
                    radioButtonVerbonden.Checked = false;
                    buttonConnect.Text = "Connect";
                    labelStatus.Text = "Status: Disconnected";
                }
                else
                {
                    // geen verbinding -> wil verbinding 
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
                    string commando = "ping";
                    serialPortArduino.WriteLine(commando);
                    string antwoord = serialPortArduino.ReadLine();
                    antwoord = antwoord.TrimEnd();
                    if (antwoord == "pong")
                    {
                        radioButtonVerbonden.Checked = true;
                        buttonConnect.Text = "Disconnect";
                        labelStatus.Text = "Status : Connected";
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
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Controleer of het geselecteerde tabblad 'tabPageOefening5' is
            if (tabControl.SelectedTab == tabPageOefening5)
            {
                
                timerOefening5.Start();
            }
            else
            {
                timerOefening5.Stop();
            }
        }

        private void timerOefening5_Tick(object sender, EventArgs e)
        {
            try
            {
                // Controleer of de seriële verbinding aanwezig is
                if (!serialPortArduino.IsOpen)
                {
                    labelGewensteTemp.Text = "Geen verbinding";
                    labelHuidigeTemp.Text = "Geen verbinding";
                    return;
                }

                // --- 1. Gewenste temperatuur (analoge pin 0) ---
                serialPortArduino.WriteLine("get a0");
                string antwoordA0 = serialPortArduino.ReadLine().TrimEnd();
                int analogeWaarde0 = Convert.ToInt32(antwoordA0);

                // Herschaal: 0 .. 1023 -> 5 .. 45 °C
                double gewensteTemp = ((40.0 / 1023.0) * analogeWaarde0) + 5.0;
                labelGewensteTemp.Text = gewensteTemp.ToString("0.0") + " °C";

                // --- 2. Huidige temperatuur (analoge pin 1) ---
                serialPortArduino.WriteLine("get a1");
                string antwoordA1 = serialPortArduino.ReadLine().TrimEnd();
                int analogeWaarde1 = Convert.ToInt32(antwoordA1);

                // Herschaal: 0 .. 1023 -> 0 .. 500 °C
                double huidigeTemp = ((500.0 / 1023.0) * analogeWaarde1) + 0.0;
                labelHuidigeTemp.Text = huidigeTemp.ToString("0.0") + " °C";

                // --- 3. Led aansturen (digitale pin 2) ---
                // De led moet branden wanneer de huidige temperatuur lager is dan de gewenste temperatuur
                if (huidigeTemp < gewensteTemp)
                {
                    // LET OP: Gebruik hier het commando dat jouw opstelling verwacht om de LED AAN te zetten (bv. 'set d2 1' of 'set d2 on')
                    serialPortArduino.WriteLine("set d2 1");
                }
                else
                {
                    // LET OP: Gebruik hier het commando dat jouw opstelling verwacht om de LED UIT te zetten (bv. 'set d2 0' of 'set d2 off')
                    serialPortArduino.WriteLine("set d2 0");
                }
            }
            catch (TimeoutException)
            {
                // Negeer tijdelijke time-outs van de leestijd om crashes te voorkomen
            }
            catch (Exception ex)
            {
                // Handel negatief antwoord of exceptions op een correcte manier af
                timerOefening5.Stop();

                if (serialPortArduino.IsOpen)
                {
                    serialPortArduino.Close();
                }

                // Update de UI-elementen uit je bestaande code
                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
                labelStatus.Text = "Status: Disconnected (Error)";

                MessageBox.Show("Fout tijdens communicatie met de Arduino.\nIs de USB kabel uitgetrokken?\n\nFoutmelding: " + ex.Message,
                                "Communicatiefout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    } 
}
