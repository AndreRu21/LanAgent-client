using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DllStruct;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using Microsoft.Win32;
using System.Drawing.Imaging;

namespace LanAgent_клиент
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static byte[] StructToByte(info comp)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, comp);
            return ms.ToArray();
        }

        private void Connect(string server,int port,info comp)
        {
            try
            {
                TcpClient client = new TcpClient(server, port);
                NetworkStream stream = client.GetStream();
                byte[] data = StructToByte(comp);

                stream.Write(data, 0, data.Length);

                stream.Close();
                client.Close();
                tslStatus.Text = DateTime.Now + " данные на сервер успешно отправлены";
            }
            catch
            {
                tslStatus.Text = DateTime.Now + " ошибка отправки данных на сервер";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
            info comp;
            //список запущенных процессов
            Process[] prc = Process.GetProcesses();
            // сформировать массив строк, в котором перечисляются именя процессов
            // и объем памяти в килобайтах, который они занимают
            comp.process = new string[prc.Length];
            for (int i = 0; i <= prc.Length - 1; i++)
                comp.process[i] = prc[i].ProcessName + " / " + prc[i].PrivateMemorySize64 / 1024;
            //список логических дисков
            comp.allDrives = DriveInfo.GetDrives();
            //имя пользователя
            comp.username = Environment.UserName;
            // сетевое имя компьютера
            comp.netname = Environment.MachineName;
            // текущая дата и время
            comp.dt = DateTime.Now;
            // IP-адрес компьютера
            comp.ip = System.Net.Dns.GetHostEntry(Environment.MachineName).AddressList[0].ToString();
            //скриншоты
            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graph = Graphics.FromImage(bmp);
            graph.CopyFromScreen(0, 0, 0, 0, bmp.Size);
            // поток в ОЗУ
            MemoryStream ms = new MemoryStream();
            // сохранить в поток скриншот в формате JPEG
            bmp.Save(ms, ImageFormat.Jpeg);
            // поставить указатель на начало потока
            ms.Position = 0;
            // сохранить изображение из потока в структуру данных
            comp.scr = Image.FromStream(ms);

            int port = int.Parse(tbxPort.Text);
            Connect(tbxAdress.Text, port, comp);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            RegistryKey k = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\LanAgentSettings");
            k.SetValue("IpAdress", tbxAdress.Text);
            k.SetValue("Port", tbxPort.Text);
            k.SetValue("Time", tbxTime.Text);
            k.Close();
        }

        private void tbxPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar)) e.Handled = true;
            if(e.KeyChar == (char)Keys.Back) e.Handled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            cbxAutorun.Checked = key.GetValue("LanAgent") != null;

            String[] arg = Environment.GetCommandLineArgs();
            if(arg.Length >1)
                if(arg[1] == "/min")
                {
                    this.Hide();
                    ShowInTaskbar = false;
                    WindowState = FormWindowState.Minimized;
                    notifyIcon1.Visible = true;
                }

            try
            {
                RegistryKey k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\LanAgentSettings");
                tbxAdress.Text = (string)k.GetValue("IpAdress");
                tbxPort.Text = (string)k.GetValue("Port");
                tbxTime.Text = (string)k.GetValue("Time");
                k.Close();
            }
            catch
            {
                tbxAdress.Text = "127.0.0.1";
                tbxPort.Text = "9595";
                tbxTime.Text = "10";
            }
            timer1_Tick(sender,e);
        }

        private void tbxTime_TextChanged(object sender, EventArgs e)
        {
            if (tbxTime.Text == "" || tbxTime.Text == "0")
                timer1.Stop();
            else
            {
                int a = Convert.ToInt32(tbxTime.Text) * 60000;
                timer1.Interval = a;
                timer1.Start();
            }
            
        }

        public void SetAutorun(string name ,bool autorun)
        {
            string exepath = Application.ExecutablePath;
            RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            try
            {
                if (autorun)
                    k.SetValue(name, exepath + " /min");
                else
                    k.DeleteValue(name);
                k.Close();
            }
            catch { }
        }

        private void cbxAutorun_CheckedChanged(object sender, EventArgs e)
        {
            SetAutorun("LanAgent", cbxAutorun.Checked);
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
            ShowInTaskbar = true;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if(WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
        }
    }
}
