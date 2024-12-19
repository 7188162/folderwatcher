using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FolderWatcher
{
    public partial class Form1 : Form {
        // メッセージ定数
        const string ESCAPED_MBSPACE = "%E3%80%80";          // 問題の全角スペースのエンコードされたテキスト

        const string MSG_FOLDERNOTFOUND = "パスが見つかりません。";
        const string MSG_CHOOSEFOLDERTEXT = "監視するフォルダを選択してください。";
        const string MSG_STARTWATCHTEXT = "監視を開始します。";
        const string MSG_WATCHENDTEXT = "監視を終了します。";

        const string BALLOONTIPTEXT_MBSPACE = "全角スペース(escaped)が含まれるファイル %1 が作成されました。\nリネームしました。";
        const string BALLOONTIPTEXT_OTHER = "ファイル %1 が作成されました。";
        const string MSG_FILEEXISTS = "ファイルが既に存在します。";

        string APPTITLE = Application.ProductName;

        bool willLog = false;
        bool triggerApp = false;

        System.IO.FileSystemWatcher Watcher {
            get; set;
            //} = null;
        } = new System.IO.FileSystemWatcher();

        public Form1() {
            InitializeComponent();
        }

        // バルーンの抑制条件のクラス
        public static class SuppressWarning {
            // private  bool suppressCond = true;

            // バルーンの抑制条件を設定
            public static bool isSuppressed(string name) {
                bool suppressCond = !name.EndsWith(".tmp")  // 拡張子 tmp
                     && !name.EndsWith(".crdownload")       // 拡張子 crdownload
                     && !name.EndsWith(".part");            // 拡張子 part

                return suppressCond;
            }
        }

        private void ExecApp(String path, String arg) {
        //    bool runprog = Properties.Settings.Default.runApp;
        //    string prog = Properties.Settings.Default.appPath;
            if (triggerApp) {
                using (System.Diagnostics.Process.Start(path, arg)) { }
            }
        }

        // フォームロード時の処理
        private void Form1_Load(object sender, EventArgs e) {

            // ウォッチャの取得
            Watcher = this.Watcher;

            // 設定の読み込み
            txtPath.Text = global::FolderWatcher.Properties.Settings.Default.watchPath;                     // 監視対象パス
            chkSuppressBalloon.Checked = global::FolderWatcher.Properties.Settings.Default.suppress;        // バルーン抑制
            chkShowCreated.Checked = Properties.Settings.Default.showcreate;                                // 作成時表示
            chkAutostart.Checked = global::FolderWatcher.Properties.Settings.Default.autostart;             // 自動開始
            chkTriggerApp.Checked = triggerApp = global::FolderWatcher.Properties.Settings.Default.runApp;  // 他のアプリの実行
            txtAppPath.Text = global::FolderWatcher.Properties.Settings.Default.appPath;                    // アプリのパス
            chkLog.Checked = willLog = global::FolderWatcher.Properties.Settings.Default.willLog;           // ログ            


            String[] triggers = { "Created", "Renamed" };
            triggers.ToList().ForEach(t => triggerWhen.Items.Add(t));

            // 監視対象パスの自動設定：ホームのダウンロードフォルダ
            if (txtPath.Text == "") {
                this.txtPath.Text = Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH") + "\\Downloads";
            }

            // フォームのタイトルの設定
            this.Text = APPTITLE;

            // タスクトレイアイコンの設定
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;   // 右クリックメニュー
            this.notifyIcon1.Visible = true;

            // ログ保存先パスの自動設定
            if (txtLogPath.Text == "") {
                this.txtLogPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + Application.ProductName + "\\log.txt";
            }

            // 自動開始がオンなら自動開始（Startボタンを押す）
            if (chkAutostart.Checked) {
                btnStart.PerformClick();
            }


        }

        private void btnStart_Click(object sender, EventArgs e) {

            // ウォッチャの取得
            Watcher = this.Watcher;

            // ロガー
            willLog = chkLog.Checked;
            MyLogger logger = null;
        
            // 監視対象パスの取得
            string watcherPath = txtPath.Text;
            try {
                try {
                    Watcher.Path = watcherPath; // 監視対象パスの取得：存在しないパスならエラー表示
                } catch {
                    MessageBox.Show(this, MSG_FOLDERNOTFOUND, APPTITLE, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }

                // ロガー
                if (willLog) {
                    logger = MyLogger.GetInstance(txtLogPath.Text);
                }

                // ウォッチャの設定
                Watcher.NotifyFilter = System.IO.NotifyFilters.FileName;

                Watcher.IncludeSubdirectories = false;  // サブディレクトリは監視しない
                Watcher.SynchronizingObject = this;     // 同期するオブジェクトをこのフォームに指定

                // イベントハンドラの設定
                //Watcher.Changed += new System.IO.FileSystemEventHandler(Watcher_Changed);   // フォルダに変更があった時
                Watcher.Created += new System.IO.FileSystemEventHandler(Watcher_Changed);   // ファイルが作成された時
                Watcher.Renamed += new System.IO.RenamedEventHandler(Watcher_Renamed);      // ファイルがリネームされた時

                // 監視開始メッセージの表示
                this.notifyIcon1.BalloonTipText = MSG_STARTWATCHTEXT;
                this.notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                this.notifyIcon1.ShowBalloonTip(5000);  // 5秒間表示

                // 監視開始
                Watcher.EnableRaisingEvents = true;

                // ログ
                if (willLog) {
                    logger.LogInfo("started watching");
                }

                // ボタンの表示を切り替える
                btnStart.Enabled = false;
                btnStop.Enabled = true;

                this.Hide();
                this.WindowState = FormWindowState.Minimized;
              //  this.WindowState = FormWindowState.Minimized;
               

            } catch (Exception ex) {
                // 例外発生時はとりあえずダイアログ表示
                MessageBox.Show(this, ex.Message, APPTITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (willLog) {
                    logger.LogError(ex.Message);
                }
            }

        }

        // フォルダ選択ボタンクリック時の処理
        private void btnBrowse_Click(object sender, EventArgs e) {

            // フォルダ選択ダイアログオブジェクトの取得
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            // メッセージ
            fbd.Description = MSG_CHOOSEFOLDERTEXT;

            // ルートフォルダの設定：デスクトップ
            fbd.RootFolder = Environment.SpecialFolder.Desktop;

            // デフォルトパスの設定：ホーム直下のダウンロードフォルダ
            fbd.SelectedPath = Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH") + "\\Downloads";

            // OKボタンが押された時の処理
            if (fbd.ShowDialog(this) == DialogResult.OK) {
                // 選択されたフォルダのパスをメインフォームのテキストボックスに格納する
                this.txtPath.Text = fbd.SelectedPath;
            }
        }


        // フォームクローズの処理
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {

            // ウォッチャの取得
            Watcher = this.Watcher;

            // 監視中か否か
            bool isRunning = Watcher.EnableRaisingEvents;

            // 監視中なら監視終了
            if (isRunning) {
                Watcher.EnableRaisingEvents = false;
                MyLogger.GetInstance().LogInfo("stop watching");
            }

            // ウォッチャの破棄
            Watcher.Dispose();
            Watcher = null;

            // ログ
            //if (willLog) {
            //    MyLogger.GetInstance().Close();
            //}

            // 設定の保存
            Properties.Settings.Default.watchPath = this.txtPath.Text;
            Properties.Settings.Default.suppress = this.chkSuppressBalloon.Checked;
            Properties.Settings.Default.autostart = this.chkAutostart.Checked;
            Properties.Settings.Default.showcreate = this.chkShowCreated.Checked;
            Properties.Settings.Default.willLog = this.chkLog.Checked;
            Properties.Settings.Default.logPath = this.txtLogPath.Text;
            Properties.Settings.Default.runApp = this.chkTriggerApp.Checked;
            Properties.Settings.Default.appPath = this.txtAppPath.Text;

            Properties.Settings.Default.Save();
        }

        // 停止ボタン押下時の処理
        private void btnStop_Click(object sender, EventArgs e) {
            // 監視中止
            Watcher.EnableRaisingEvents = false;

            // ボタンの切り替え
            btnStart.Enabled = true;
            btnStop.Enabled = false;

            // 監視終了メッセージの表示（非抑制時）
            if (!chkSuppressBalloon.Checked) {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon1.BalloonTipText = MSG_WATCHENDTEXT;
                notifyIcon1.ShowBalloonTip(5000);
            }

            if (willLog) {
                MyLogger.GetInstance().LogInfo("stop watching");
                MyLogger.Dispose();
            }
        }

        // フォルダに変更があった時の処理
        private void Watcher_Changed(System.Object sender, System.IO.FileSystemEventArgs e) {
            
            switch (e.ChangeType) {
                // ファイル作成時の処理
                case System.IO.WatcherChangeTypes.Created:
                    if (this.willLog) {
                        MyLogger logger = MyLogger.GetInstance();
                        logger.LogInfo("File created: " + e.Name);
                    }

                    if (triggerApp) {
                        ExecApp(txtAppPath.Text, e.Name);
                    }

                    if (!chkSuppressBalloon.Checked
                        && SuppressWarning.isSuppressed(e.Name)) {
                        this.notifyIcon1.BalloonTipText = BALLOONTIPTEXT_OTHER.Replace("%1", e.Name);
                        this.notifyIcon1.ShowBalloonTip(5000);
                    }
                    break;
            }
        }

        // 監視対象フォルダの項目名が変更された時
        private void Watcher_Renamed(object sender, System.IO.RenamedEventArgs e) {
            

            if (!chkSuppressBalloon.Checked
                && SuppressWarning.isSuppressed(e.Name)) {

                // リネーム前後のファイル名表示：デバッグ用と言えばデバッグ用
                notifyIcon1.BalloonTipText = "from: " + e.OldFullPath + "\nTo: " + e.FullPath;
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon1.ShowBalloonTip(5000);

                if (willLog) {
                    MyLogger logger = MyLogger.GetInstance();
                    logger.LogInfo("File renamed to: " + e.FullPath + " from: " + e.OldFullPath);                         
                }
            }

            // 全角空白エスケープを含むとき、かつ、抑制テキストを含まないとき
            if (e.Name.Contains(ESCAPED_MBSPACE)
                 && SuppressWarning.isSuppressed(e.Name)) {

                // 当該ファイルをリネームする
                try {
                    System.IO.File.Move(e.FullPath, e.FullPath.Replace(ESCAPED_MBSPACE, "　"));

                    // 結果表示
                    this.notifyIcon1.BalloonTipText = BALLOONTIPTEXT_MBSPACE.Replace("%1", e.Name);
                    this.notifyIcon1.ShowBalloonTip(5000);

                } catch (System.IO.IOException ioe) {
                    MessageBox.Show(this, ioe.Message, APPTITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                } catch (Exception ex) {
                    MessageBox.Show(this, ex.Message, APPTITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            } else {
                if (!chkSuppressBalloon.Checked) {
                    this.notifyIcon1.BalloonTipText = BALLOONTIPTEXT_OTHER.Replace("%1", e.Name);
                    this.notifyIcon1.ShowBalloonTip(5000);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void Form1_ClientSizeChanged(object sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Minimized) {
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e) {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void btnBrowseLog_Click(object sender, EventArgs e) {
            FileDialog ofd = new OpenFileDialog();
              ofd.CheckFileExists
            = ofd.CheckPathExists
            = false;

            if (ofd.ShowDialog() == DialogResult.OK) {
                txtLogPath.Text =  ofd.FileName;
            }
        }

        private void btnBrowseApp_Click(object sender, EventArgs e) {
            FileDialog ofd = new OpenFileDialog();
              ofd.CheckFileExists
            = ofd.CheckPathExists
            = false;

            if (ofd.ShowDialog() == DialogResult.OK) {
                txtAppPath.Text = ofd.FileName;
            }
        }
    }
}
