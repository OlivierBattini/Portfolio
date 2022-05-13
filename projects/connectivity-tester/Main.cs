using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;

namespace CalypsoConnect
{
    public partial class Main : Form
    {
        public string m_Tasks = "";
        public string m_ServerUrl = "tess.calypso3d.fr";

        private delegate void LogCallback(string entry);

        public Main()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(StartTest)).Start();
        }

        private void StartTest()
        {
            bool testSuccess = true;
            DateTime startTime;
            DateTime endTime;

            DialogResult response = MessageBox.Show(
                "Avant de démarrer ce test de connectivité, assurez-vous d'avoir correctement configuré votre infrastructure informatique et d'être connecté à internet.",
                "Démarrage du test de connectivité Calypso Connect",
                MessageBoxButtons.OKCancel
                );

            if (response != DialogResult.OK)
                return;

            m_Tasks = CalypsoConnect.Properties.Resources.tasks;

            Log("Test de connectivité à Calypso 3D");
            Log("Démarrage : " + DateTime.Now.ToString("G"));

            Log("Ping du serveur...");
            startTime = DateTime.Now;
            Ping pinger = new Ping();
            PingReply reply = pinger.Send(m_ServerUrl);
            endTime = DateTime.Now;
            if (reply.Status == IPStatus.Success)
            {
                Log($"Ping du serveur {m_ServerUrl} : OK ({(endTime - startTime).TotalMilliseconds} ms)");
            }
            else
            {
                Log($"Ping du serveur {m_ServerUrl} : ERREUR ({reply.Status.ToString()})");
            }

            Log("Test de débit depuis le serveur...");
            startTime = DateTime.Now;
            WebClient client = new WebClient();
            string data = client.DownloadString($"http://{m_ServerUrl}/files/testfile");
            endTime = DateTime.Now;
            double mbSize = (double)Encoding.Unicode.GetByteCount(data) / 1048576;
            Log($"Votre connexion au serveur {m_ServerUrl} en download est de : {Math.Round(mbSize / (endTime - startTime).TotalSeconds, 2)} Mb/s");

            foreach (string task in m_Tasks.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                string[] t = task.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries);

                switch (t[0])
                {
                    case "http":
                        bool taskSuccess = TestHTTP(task);
                        if (taskSuccess)
                            Log(task + " : OK ");
                        else
                        {
                            Log(task + " : ERREUR ");
                            testSuccess = false;
                        }
                        break;

                    //case "tcp":
                    //    TestTCP(t[1]);
                    //    break;

                    default:
                        {
                            Log("Erreur : " + task);
                            testSuccess = false;
                        }
                        break;
                }
            }

            Log(Environment.NewLine + "Tous les points de contrôle en protocole TCP ont été vérifiés. Pour communiquer ces résultats de test, faites un copier/coller et transmettez les à un gestionnaire de Calypso 3D.");
            Log(Environment.NewLine + "ATTENTION ! Ce test de connectivité est réussi en TCP, il ne valide qu'une partie de votre configuration réseau. Cela ne signifie pas que votre infrastructure informatique est parfaitement configurée. Les autres protocoles réseau tels que UDP, SIP, STUN, RDP, RTCP ne peuvent être éprouvés via cette application.");
        }

        private void Log(string entry)
        {
            if (this.ReportBox.InvokeRequired)
            {
                LogCallback d = new LogCallback(Log);
                this.Invoke(d, new object[] { entry });
            }
            else
            {
                this.ReportBox.AppendText(Environment.NewLine + entry);
            }
        }

        private bool TestHTTP(string address)
        {
            try
            {
                WebClient client = new WebClient();
                client.DownloadString(address);
                return true;
            }
            catch (WebException wex)
            {
                if (wex.Status == WebExceptionStatus.ProtocolError)
                    return true;
                else
                    return false; 
            }
            catch (Exception ex)
            { return false; }
        }
    }
}
