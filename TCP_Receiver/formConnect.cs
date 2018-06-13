using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;

namespace TCP_Receiver
{
    public partial class FormConnect : Form
    {

        Thread streamThread = null;
        TcpClient client = null;

        // Using JPEG file signature to identify where the JPG 
        // file begins and ends in each packet.
        byte[] jpegHead = new byte[] { 0xff, 0xd8 };
        byte[] jpegTail = new byte[] { 0xff, 0xd9 };

        public FormConnect()
        {
            InitializeComponent();
        }

        // automatically populates with test scenario
        private void FormConnect_Load(object sender, EventArgs e)
        {
            textBoxIp.Text = "localhost";
            textBoxPort.Text = "13000";
            buttonDisconnect.Enabled = false;
        }

        // connect button starts the streaming thread
        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            buttonConnect.Enabled = false;
            buttonDisconnect.Enabled = true;
            textBoxIp.Enabled = false;
            textBoxPort.Enabled = false;

            streamThread = new Thread(new ThreadStart(ClientConnect));
            streamThread.IsBackground = true;
            streamThread.Start();
        }

        private void ClientConnect()
        {
            // databuffer handles all streamed data.
            byte[] dataBuffer;
            int bytesReceived;

            try
            {
                string address = textBoxIp.Text;
                int port = int.Parse(textBoxPort.Text);

                client = new TcpClient(address, port);
                NetworkStream stream = client.GetStream();
                logText("Client connected");

                do
                {
                    dataBuffer = new byte[client.ReceiveBufferSize];
                    bytesReceived = stream.Read(dataBuffer, 0, client.ReceiveBufferSize);
                    logText(bytesReceived.ToString() + " bytes recieved");

                    processData(dataBuffer);

                    //Thread.Sleep(10);
                } while (true);
            }
            catch (SocketException e)
            {
                logText(e.Message);
            }
            catch (IOException e)
            {
                logText(e.Message);
            }
            catch (FormatException e)
            {
                logText(e.Message);
            }
            finally
            {
                if (client != null)
                {
                    client.Close();
                }
            }
        }

        // process each packet of data to produce the images
        private void processData(byte[] data)
        {
            int imgLength;
            int indexJpegHead = findIndex(data, jpegHead);
            int indexJpegTail = findIndex(data, jpegTail);

            if ((indexJpegHead != -1) && (indexJpegTail != -1) && (indexJpegHead < indexJpegTail))
            {
                //total bytes of image ( + 2 for JPEG tail )
                imgLength = (indexJpegTail + 2) - indexJpegHead;
                byte[] imgData = new byte[imgLength];
                Array.Copy(data, indexJpegHead, imgData, 0, imgLength);

                //autodisposes memStream
                using (MemoryStream memStream = new MemoryStream(imgData))
                {
                    // push to picturebox
                    Image image = Image.FromStream(memStream);
                    Invoke((MethodInvoker)delegate () { pictureBoxImage.Image = image; });
                }
            }
        }

        // disconnect stops streming thread
        private void ButtonDisconnect_Click(object sender, EventArgs e)
        {
            streamThread.Abort();
            buttonConnect.Enabled = true;
            buttonDisconnect.Enabled = false;
            textBoxIp.Enabled = true;
            textBoxPort.Enabled = true;
            logText("Client disconnected");
        }

        // Delegate enables asynchronous calls for setting text property of textBox  
        delegate void AddTextToLog(string text);

        // generic logging function to print to screen
        private void logText(string text)
        {
            if (textBoxOut.InvokeRequired)
            {
                AddTextToLog d = new AddTextToLog(logText);
                Invoke(d, new object[] { text });
            }
            else
            {
                textBoxOut.AppendText("[" + DateTime.Now.ToString() + "] - " + text + Environment.NewLine);
            }
        }

        // findIndex used to return index position of a byte pattern in a given byte[]
        private static int findIndex(byte[] searchArray, byte[] patternArray)
        {
            if (patternArray.Length > searchArray.Length)
                return -1;
            for (int i = 0; i < searchArray.Length - patternArray.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < patternArray.Length; j++)
                {
                    if (searchArray[i + j] != patternArray[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
