using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ROOT.WMI;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        private static Form1 Instance;

        private ManagementEventWatcher watcher;
        private ManagementScope scope;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private ContextMenu contextMenu = new ContextMenu();

        public Form1()
        {
            Instance = this;

            InitializeComponent();

            MenuItem item0 = new MenuItem("");
            item0.Enabled = false;

            MenuItem item1 = new MenuItem("&Exit");
            item1.Click += (object sender, EventArgs e) =>
            {
                Close();
            };

            contextMenu.Popup += (object sender, EventArgs e) =>
            {
                byte brightness = 0;
                foreach (WmiMonitorBrightness instance in WmiMonitorBrightness.GetInstances())
                {
                    brightness = instance.CurrentBrightness;
                    break;
                }

                ((ContextMenu)sender).MenuItems[0].Text = String.Format("Brightness = {0}", brightness);
            };

            contextMenu.MenuItems.Add(item0);
            contextMenu.MenuItems.Add(item1);

            notifyIcon1.ContextMenu = contextMenu;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _hookID = SetHook(_proc);

            scope = new ManagementScope("\\\\localhost\\root\\WMI");
            scope.Connect();

            watcher = new ManagementEventWatcher(scope, new EventQuery("SELECT * FROM WmiMonitorBrightnessEvent"));
            watcher.EventArrived += (object sender2, EventArrivedEventArgs e2) =>
            {
                WmiMonitorBrightnessEvent e3 = new WmiMonitorBrightnessEvent(e2.NewEvent);
                //Debug.WriteLine("E: {0}", e3.Brightness);

                //foreach (WmiMonitorBrightness instance in WmiMonitorBrightness.GetInstances())
                //    Debug.WriteLine("F: {0}", instance.CurrentBrightness);

                foreach (WmiMonitorBrightness instance in WmiMonitorBrightness.GetInstances())
                {
                    byte brightness = instance.CurrentBrightness;
                    notifyIcon1.Text = String.Format("Brightness = {0}", brightness);
                    break;
                }
            };

            watcher.Start();

            foreach (WmiMonitorBrightness instance in WmiMonitorBrightness.GetInstances())
            {
                byte brightness = instance.CurrentBrightness;
                notifyIcon1.Text = String.Format("Brightness = {0}", brightness);
                break;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Location = new Point(Int32.MinValue, Int32.MinValue);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            watcher.Stop();
            UnhookWindowsHookEx(_hookID);
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (Control.ModifierKeys == (Keys.Control | Keys.Alt) && (Keys)vkCode == Keys.Left)
                {
                    Instance.BeginInvoke((MethodInvoker)(() =>
                    {
                        foreach (WmiMonitorBrightnessMethods methods in WmiMonitorBrightnessMethods.GetInstances())
                            methods.WmiSetBrightness(0, 0);
                    }));
                }

                if (Control.ModifierKeys == (Keys.Control | Keys.Alt) && (Keys)vkCode == Keys.Right)
                {
                    Instance.BeginInvoke((MethodInvoker)(() =>
                    {
                        foreach (WmiMonitorBrightnessMethods methods in WmiMonitorBrightnessMethods.GetInstances())
                            methods.WmiSetBrightness(100, 0);
                    }));
                }

                if (Control.ModifierKeys == (Keys.Control | Keys.Alt) && (Keys)vkCode == Keys.Up)
                {
                    Instance.BeginInvoke((MethodInvoker)(() =>
                    {
                        byte brightness = 0;

                        foreach (WmiMonitorBrightness instance in WmiMonitorBrightness.GetInstances())
                            brightness = instance.CurrentBrightness;

                        brightness += 5;
                        if (brightness > 100)
                            brightness = 100;

                        foreach (WmiMonitorBrightnessMethods methods in WmiMonitorBrightnessMethods.GetInstances())
                            methods.WmiSetBrightness(brightness, 0);
                    }));
                }

                if (Control.ModifierKeys == (Keys.Control | Keys.Alt) && (Keys)vkCode == Keys.Down)
                {
                    Instance.BeginInvoke((MethodInvoker)(() =>
                    {
                        byte brightness = 0;

                        foreach (WmiMonitorBrightness instance in WmiMonitorBrightness.GetInstances())
                            brightness = instance.CurrentBrightness;

                        brightness -= 5;
                        if (brightness > 100)
                            brightness = 0;

                        foreach (WmiMonitorBrightnessMethods methods in WmiMonitorBrightnessMethods.GetInstances())
                            methods.WmiSetBrightness(brightness, 0);
                    }));
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
