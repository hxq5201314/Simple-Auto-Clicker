using System;
using System.Windows.Forms;

namespace AutoClicker
{
    // 程序入口,所有 WinForms 程序都从这里开始执行
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
