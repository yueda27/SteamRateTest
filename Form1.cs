using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;



namespace WindowsFormsApp3
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            string[] port = SerialPort.GetPortNames();
            comboPortSelection.Items.AddRange(port);                                         //Search for available serial ports
            IOCommand.controller.Open();
            if (IOCommand.controller.IsOpen == true)
            {
                richSerialOut.Text = "IO Controller connected.";
            }
            else
            {
                richSerialOut.Text = "Unable to connect to IO Controller";
                IOCommand.controller.Open();
            }
            
            

        }


        private void button1_Click(object sender, EventArgs e)
        {
            richSerialOut.Text = "";
            richSerialOut.ForeColor = Color.Black;
            richSerialOut.Text += serialPort1.ReadExisting();                              // Allow data to be pulled when "button1" is pressed. 
            richSerialOut.Select(richSerialOut.Text.Length, 0);
            richSerialOut.ScrollToCaret();                                                 // Data is contunuously transmitted and is built up in buffer
        }

        private void richSerialOut_TextChanged(object sender, EventArgs e)
        {
        }

        private void buttonOpenPort_Click(object sender, EventArgs e)
        {
            if (comboPortSelection.Text == "")
            {
                richSerialOut.ForeColor = Color.Red;
                richSerialOut.Text += "There is no Serial Port selected.\n";                //Catch if there is no serial port detected
                richSerialOut.Select(richSerialOut.Text.Length, 0);
                richSerialOut.ScrollToCaret();
            }
            else
            {
                serialPort1.PortName = comboPortSelection.Text;
                serialPort1.BaudRate = 1200;
                serialPort1.Open();                                                          //Initialise and open serial port
                button1.Enabled = true;
                buttonOpenPort.Enabled = false;
                buttonClosePort.Enabled = true;
                buttonSend.Enabled = true;
                buttonSubmitInformation.Enabled = true;
            }
        }

        private void buttonClosePort_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            buttonClosePort.Enabled = false;
            buttonOpenPort.Enabled = true;
            button1.Enabled = false;
            buttonSteamRateTest.Enabled = false;
            buttonSubmitInformation.Enabled = false;
            buttonSend.Enabled = false;
            richSerialOut.Text = "";
            richInput.Text = "";
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            serialPort1.Write(richInput.Text.ToUpper() + "\r");
            System.Threading.Thread.Sleep(1300);     //Wait for data to buffer. Some command requires buffer time
            richInput.Text = "";                     //Reset Command Box to ""
            richSerialOut.Text += serialPort1.ReadExisting();
            richSerialOut.Select(richSerialOut.Text.Length, 0);
            richSerialOut.ScrollToCaret();
        }

        private void buttonSubmitInformation_Click(object sender, EventArgs e)
        {
            TestInformation.TestRequestID = textTestRequestID.Text;
            TestInformation.ProjectName = textProjectName.Text;
            TestInformation.ProductName = textProductName.Text;
            TestInformation.SampleSize = textSampleSize.Text;
            TestInformation.SampleNumber = textSampleNumber.Text;
            TestInformation.Requester = textRequester.Text;
            TestInformation.SampleType = textSampleType.Text;
            TestInformation.TestID = textTestID.Text;

            buttonSteamRateTest.Enabled = true;
            richSerialOut.Text = "";
        }

        private void buttonSteamRateTest_Click(object sender, EventArgs e)   //for loop with time delay based on protocol
        {
            List<string> database = new List<string>();
            try
            {
                WriteToController(IOCommand.Ch1Off);
                WriteToController(IOCommand.Ch2Off);    //Set plunger off
               

                richSerialOut.Text = "";               //clear serial text box
                progressSteamTest.Maximum = 37;        //Set up progress bar
                progressSteamTest.Minimum = 1;
                progressSteamTest.Step = 1;
                //Initiate Start-up protocol
                StartUpProtocol();


                richSerialOut.Text = "";
                richSerialOut.Text = "Base weight w0 ";    // Start measurement for w0 Base weight. 
                serialPort1.Write("D07" + "\r");
                Thread.Sleep(1300);
                richSerialOut.Text = serialPort1.ReadExisting();
                richSerialOut.Update();

                StabilitySelect();      // Call sub-function StabilitySelect

                int[] cycle_variable = new int[] { 12, 12, 12 };     //Define different cycle time in array

                for (int test = 0; test < 3; test++)
                {

                    for (int cycle = 0; cycle < cycle_variable[test]; cycle++)
                    {
                        richSerialOut.Text = "Test: w" + (test + 1) + "   " + String.Format("Cycle: {0}/{1}", (cycle + 1) + (test * 12), (cycle_variable[test] * (test + 1)));      //Offset for w0 measurement. Further calculation to show further iteration beyond 12
                        richSerialOut.Update();
                        progressSteamTest.PerformStep();                                           // Update Progress Bar for test progress
                        WriteToController(IOCommand.Ch2On);                                        //ACTIVATE PLUNGER FOR 5 SEC
                        Thread.Sleep(5000);                                                         // Delay for test cycle ON
                        WriteToController(IOCommand.Ch2Off);                                       //OFF PLUNGER FOR 15 SEC
                        Thread.Sleep(15000);                                                        // Delay for test cycle OFF
                    }

                    serialPort1.Write("D07" + "\r");                                              // Command to scale for single data with stability
                    Thread.Sleep(1300);
                    richSerialOut.Text = serialPort1.ReadExisting();
                    richSerialOut.Update();

                    StabilitySelect();                                                              // Call sub-function StabilitySelect

                }
                richSerialOut.Text = "";                                                           //Clear richSerialOut from last data
                for (int i = 0; i < database.Count; i++)                                           //Format string to be converted to double
                {
                    database[i] = (database[i].Replace(" ", "").Replace("kg", "").Replace("S", ""));
                }
                //Turn off iron
                WriteToController(IOCommand.Ch1Off);

                data.w0 = Convert.ToDouble(database[0]);       // Split database into respective values
                data.w1 = Convert.ToDouble(database[1]);
                data.w2 = Convert.ToDouble(database[2]);
                data.w3 = Convert.ToDouble(database[3]);
                data.SteamRateVal = Steam_Rate(data.w0, data.w1, data.w2, data.w3);     // Call for Steam Rate Calculation function
                data.WeightLoss1 = data.w0 - data.w1;
                data.WeightLoss2 = data.w1 - data.w2;
                data.WeightLoss3 = data.w2 - data.w3;
                //Export file to csv by calling sub function Export_CSV()
                Export_CSV();

                //Show collected data
                richSerialOut.Text = "TEST REQUEST ID: " + TestInformation.TestRequestID + "    " + "    " + "PROJECT NAME: " + TestInformation.ProjectName +
                    "    " + "PRODUCT NAME: " + TestInformation.ProductName + Environment.NewLine + Environment.NewLine;

                richSerialOut.Text += "SAMPLE SIZE: " + TestInformation.SampleSize + "    " + "SAMPLE NUMBER: " + TestInformation.SampleNumber +
                    "    " + "REQUESTER: " + TestInformation.Requester + Environment.NewLine + Environment.NewLine;


                richSerialOut.Text += "SAMPLE TYPE: " + TestInformation.SampleType + "    " +
                    "TEST ID: " + TestInformation.TestID + Environment.NewLine + Environment.NewLine;

                richSerialOut.Text += "w0: " + data.w0 + "Kg" + "    " + "w1: " + data.w1 + "Kg" + "    " + "w2: " + data.w2 + "Kg" + "    " +
                    "w3: " + data.w3 + "Kg" + Environment.NewLine;
                richSerialOut.Text += "STEAM RATE VALUE: " + data.SteamRateVal + "Kg/Min";
                progressSteamTest.Value = 1;

                textTestRequestID.Text = "";
                textProjectName.Text = "";
                textProductName.Text = "";
                textSampleSize.Text = "";
                textSampleNumber.Text = "";
                textRequester.Text = "";
                textSampleType.Text = "";
                textTestID.Text = "";
                buttonSteamRateTest.Enabled = false;
            }   //End of try block
            catch (System.IO.IOException ex)
            {
                richSerialOut.Text = "Exception caught: Please close the CSV file log.";
            }
            catch(Exception ex)
            {
                richSerialOut.Text = String.Format("Exception caught: {0}", ex);
            }



            //SUB-FUNCTION LIBRARY

            void WriteToController(byte[] binputMsg)    //Subfunction to write to Controller
            {
                IOCommand.controller.Write(binputMsg, 0, binputMsg.Length);
                IOCommand.controller.Write("\n");
            }

            void StartUpProtocol()
            {
                richSerialOut.Text = "Initialising Iron Start up\n";
                //CALL STARTUP SUB-FUNCTION: 
                WriteToController(IOCommand.Ch1On);
                richSerialOut.Text += "Iron on";
                richSerialOut.Update();
                richSerialOut.Text += "Waiting 120 seconds...";
                richSerialOut.Update();
                //MODE SELECTION
                //AUTO WATER TOP UP IF LOW LEVEL DETECTED. CUT OFF AT HIGH LEVEL
                Thread.Sleep (12);
                richSerialOut.Text = "Initialisation Complete";
                richSerialOut.Update();
            }

            double Steam_Rate(double w0_, double w1_, double w2_, double w3_)                  //Sub function to calculate steam rate
            {
                
                double dblSteamRateVal = ((w0_ - w1_) + (w1_ - w2_) + (w2_ - w3_))/3;
                double dblSteamRateValTruncate = Math.Truncate(dblSteamRateVal * 100) / 100;   //Truncate data to 2.D.P
                return dblSteamRateValTruncate;
            }

            void StabilitySelect()                                                             // Sub function to only accept stable data
            {
                while (richSerialOut.Text.Contains("U") || richSerialOut.Lines.Count() > 3 )   // While Loop to only accept data when data is stable and only 1 data
                {
                    Thread.Sleep(2000);
                    serialPort1.Write("D07" + "\r");
                    Thread.Sleep(1300);
                    richSerialOut.Text = serialPort1.ReadExisting();
                    richSerialOut.Update();
                }

                if (richSerialOut.Text.Contains("S") == true)
                {
                    database.Add(richSerialOut.Text);
                    Thread.Sleep(1000);
                }
            }
            
            void Export_CSV()                                                                  // Sub function to export data to CSV file
            {
                StringBuilder exportCSV = new StringBuilder();
                string csvpath = "C:\\Users\\320035623\\Desktop\\serialtest.csv";
                exportCSV.AppendLine(String.Format("{8},{10},{11},{12},{9},{13},{14},{15},{0},{1},{2},{3},{4},{5},{6},{7}"
                    , data.w0, data.w1, data.w2, data.w3, data.WeightLoss1, data.WeightLoss2, data.WeightLoss3, data.SteamRateVal, 
                    TestInformation.TestRequestID, TestInformation.SampleNumber, TestInformation.ProjectName, TestInformation.ProductName, TestInformation.SampleSize, TestInformation.Requester, TestInformation.SampleType, TestInformation.TestID));
                File.AppendAllText(csvpath, exportCSV.ToString());
                exportCSV.Clear();
            }


            
        }
    }
}
