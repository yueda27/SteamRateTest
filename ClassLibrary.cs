using System;
using System.IO.Ports;

namespace WindowsFormsApp3
{
    class data
    {
        public static double w0;
        public static double w1;
        public static double w2;
        public static double w3;
        public static double SteamRateVal;
        public static double WeightLoss1;
        public static double WeightLoss2;
        public static double WeightLoss3;
    }

    class TestInformation
    {
        public static string TestRequestID;
        public static string SampleNumber;
        public static string ProjectName;
        public static string ProductName;
        public static string SampleSize;
        public static string TestID;
        public static string Requester;
        public static string SampleType;
    }
    class IOCommand
    {
        public static SerialPort controller = new SerialPort("COM5", 9600, Parity.None, 8, StopBits.One);
        public static byte[] Ch1On = { 0x55, 0x56, 0x00, 0x00, 0x00, 0x01, 0x01, 0xAD };
        public static byte[] Ch2On = { 0x55, 0x56, 0x00, 0x00, 0x00, 0x02, 0x01, 0xAE };
        public static byte[] Ch1Off = { 0x55, 0x56, 0x00, 0x00, 0x00, 0x01, 0x02, 0xAE };
        public static byte[] Ch2Off = { 0x55, 0x56, 0x00, 0x00, 0x00, 0x02, 0x02, 0xAF };
    }
    
}
