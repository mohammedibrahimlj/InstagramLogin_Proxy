using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
namespace ProxyBrowser
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string[] temp = new string[2];
            if(args.Length>0)
            {
                 temp = Strings.Split(args[0], "|", -1, CompareMethod.Binary);
            }
            else
            {
                temp[0] = "NONE";
                temp[1] = "NONE";
            }
            
            Application.Run(new BrowserForm(temp));
        }
    }
}
