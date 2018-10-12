using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FolderWatcher
{

    static class Program
    {
        // Form1 form1 = new Form1();

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());


        }

        //static Form1 getInstance() {
        //    return this.form1;
        //}


    }
}
