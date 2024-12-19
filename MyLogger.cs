using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FolderWatcher
{
    class MyLogger
    {
        public enum LogType
        {
            DEBUG = 0,
            INFO = 1,
            WARN = 2,
            CRITICAL = 3,
            ERROR = 4
        }

        private MyLogger(String path) {
            this.LogPath = path;
        //    this.stream = new System.IO.StreamWriter(this.LogPath, true, Encoding.UTF8);
        }

        private MyLogger() {
        //    this.stream = new System.IO.StreamWriter(this.LogPath,true,Encoding.UTF8);
        }

        private static MyLogger logger = null;
        private System.IO.StreamWriter stream = null;

        private String LogPath;

        public static MyLogger GetInstance(String path) {
            if (MyLogger.logger == null) {
                MyLogger.logger = new MyLogger(path);
            }
            return MyLogger.logger;
        }

        public static MyLogger GetInstance() {
            if (MyLogger.logger == null) {
                MyLogger.logger = new MyLogger();
            }
            return MyLogger.logger;
        }

        public void LogDebug(String message) { this.Log(LogType.DEBUG, message); }
        public void LogInfo(String message) { this.Log(LogType.INFO, message); }
        public void LogWarn(String message) { this.Log(LogType.WARN, message); }
        public void LogCritical(String message) { this.Log(LogType.CRITICAL, message); }    
        public void LogError(String message) { this.Log(LogType.ERROR, message); }

        private void Log(LogType logType, String message) {
            String myMessage = String.Format("[{0}] [{1}] {2}" + Environment.NewLine, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), logType, message);
            //    this.stream.WriteLine(myMessage);            
            System.IO.File.AppendAllText(this.LogPath, myMessage);
        }

        public void Close() {
            this.stream.Close();
        }

        public static void Dispose() {
            MyLogger.logger = null;
        }
        
    }    
}
