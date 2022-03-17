using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectionTester
{
    public partial class FrmConnectionTester : Form
    {
        private CancellationTokenSource _src;
        private CancellationToken _token;
        public const int DefaultTimeOut = 100;
        private static int _timeOut;
        private static object _lock = new object();
        public static int TimeOut
        {
            get
            {
                return _timeOut;
            }
            private set
            {
                lock (_lock)
                {
                    _timeOut = value;
                }
            }
        }

        public FrmConnectionTester()
        {
            InitializeComponent();
            _timeOut = DefaultTimeOut;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            _src = new CancellationTokenSource();
            _token = _src.Token;
            btnStart.Enabled = false;

            try
            {
                if (!int.TryParse(txtTimeout.Text.Trim(), out int to))
                {
                    TimeOut = DefaultTimeOut;
                    MessageBox.Show($"Timeout could not be read as a number, set to default: {DefaultTimeOut}", "Warning: Timeout");
                    txtTimeout.Text = DefaultTimeOut.ToString();
                }
                else
                {
                    TimeOut = to;
                }

                string ipadd = txtIP.Text.Trim();
                if (string.IsNullOrWhiteSpace(ipadd) || !IPAddress.TryParse(ipadd, out IPAddress addr))
                {
                    MessageBox.Show($"Invalid IP: {ipadd}");
                }
                else
                {
                    Task.Run(() => TestConnection(_token, ipadd), _token);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}");
                btnStart.Enabled = true;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                _src.Cancel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}");
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtOutput.Text = string.Empty;
        }

        public void TestConnection(CancellationToken ct, string IP)
        {
            long num = 0;
            while (!ct.IsCancellationRequested)
            {
                if (!PingHost(IP))
                {
                    Log($"Ping Failed for IP: {IP}");
                }
                else if (num % 10 == 0)
                {
                    Log("Success.");
                }

                if (num == long.MaxValue)
                {
                    num %= 10;
                }
                ++num;
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Pings host
        /// </summary>
        /// <param name="nameOrAddress">IP Address of Device</param>
        /// <returns>True/False == pingable</returns>
        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress, TimeOut);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        public void Log(string message)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    txtOutput.AppendText(DateTime.Now + ": " + message + Environment.NewLine);
                }));
            }
            else
            {
                txtOutput.AppendText(DateTime.Now + ": " + message + Environment.NewLine);
            }
        }
    }
}
