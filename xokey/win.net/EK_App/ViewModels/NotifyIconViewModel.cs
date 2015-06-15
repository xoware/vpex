using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;


namespace EK_App.NotifyIconViewModels
{
    /// <summary>
    /// Provides bindable properties and commands for the NotifyIcon. In this sample, the
    /// view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
    /// in App.xaml.cs could have created this view model,
    public class NotifyIconViewModel : System.ComponentModel.INotifyPropertyChanged 
    {
        System.ComponentModel.BackgroundWorker CreateStartupWorker = null;
        System.ComponentModel.BackgroundWorker DeleteStartupWorker = null;


        private void Exec_Schtasks(String Args)
        {
            // Start the child process.
            Process enable_proc = new Process();
            // Redirect the output stream of the child process.
            enable_proc.StartInfo.UseShellExecute = false;
            enable_proc.StartInfo.RedirectStandardOutput = true;
            enable_proc.StartInfo.FileName = "schtasks";
            enable_proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            enable_proc.StartInfo.CreateNoWindow = true;
            enable_proc.StartInfo.Arguments = Args;

            enable_proc.Start();
            // Do not wait for the child process to exit befor
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            string output = enable_proc.StandardOutput.ReadToEnd();
            enable_proc.WaitForExit();
            System.Console.WriteLine("schtasks" + output);
        }

        // This event handler is where the time-consuming work is done. 
        private void Enable_Startup_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            String Args = "/create /sc onlogon /F /tn XOkey /rl highest /tr \"\\\"" +
                 System.Reflection.Assembly.GetExecutingAssembly().Location + "\\\" --log c:\\XOkey.log \" ";

            Exec_Schtasks(Args);
            Exec_Schtasks("/delete /F /TN ExoKey");  // Try to delete old name
        }

        private void Disable_Startup_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {


            Exec_Schtasks("/delete /F /TN XOkey");
            Exec_Schtasks("/delete /F /TN ExoKey");  // Try to delete old name
          
        }
        public void Enable_Startup()
        {
            if (CreateStartupWorker == null)
            {
                CreateStartupWorker = new System.ComponentModel.BackgroundWorker();
                CreateStartupWorker.DoWork += Enable_Startup_DoWork;
            }

            if (CreateStartupWorker.IsBusy)
                return;

            CreateStartupWorker.RunWorkerAsync();
        }

        public void Disable_Startup()
        {
            if (DeleteStartupWorker == null)
            {
                DeleteStartupWorker = new System.ComponentModel.BackgroundWorker();
                DeleteStartupWorker.DoWork += Disable_Startup_DoWork;
            }

            if (CreateStartupWorker.IsBusy)
                return;

            DeleteStartupWorker.RunWorkerAsync();
        }

        private bool CanShowWindow()
        {
            if (Application.Current == null)
                return false;

            if (Application.Current.MainWindow == null)
                return true;

            return Application.Current.MainWindow.Visibility != Visibility.Visible;
        }

        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => CanShowWindow(),
                    CommandAction = () =>
                    {
                        App.Raise_Window();
                        //Application.Current.MainWindow.Visibility = Visibility.Visible;
                    }
                };
                /*
                return new DelegateCommand
                {
                    CanExecuteFunc = () => Application.Current.MainWindow == null,
                    CommandAction = () =>
                    {
                        Application.Current.MainWindow = new MainWindow();
                        Application.Current.MainWindow.Show();
                    }
                };
                 * */
            }
        }
        private bool CanHideWindow()
        {
            if (Application.Current == null || Application.Current.MainWindow == null)
                return false;

            return Application.Current.MainWindow.Visibility == Visibility.Visible;
        }


        /// <summary>
        /// Hides the main window. This command is only enabled if a window is open.
        /// </summary>
        public ICommand HideWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>  Application.Current.MainWindow.Visibility = Visibility.Hidden,
                    CanExecuteFunc = () => CanHideWindow()
                };
                /*
                return new DelegateCommand
                {
                    CommandAction = () => Application.Current.MainWindow.Close(),
                    CanExecuteFunc = () => Application.Current.MainWindow != null
                };
                 */
            }
        }


        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                
                return new DelegateCommand { CommandAction = () => {
                    try {
                        Xoware.IpcAnonPipe.PipeClient.Send_Msg("EXIT");
                    } catch {
                    }
                    Application.Current.Shutdown();
                } };
            }
        }

        /// <summary>
        /// gets check
        /// </summary>
        public Boolean StartupIsChecked
        {
            get
            {
                bool run = EK_App.Settings1.Default.RunAtStartup;
                if (run)
                {
                    Enable_Startup();
                }
                else
                {
                    Disable_Startup();
                } 

                return run;
            }
            set
            {
                EK_App.Settings1.Default.RunAtStartup = value;
                EK_App.Settings1.Default.Save();
                OnPropertyChanged("StartupIsChecked"); 
            }
        }

        #region INotifyPropertyChanged Members
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this,
                    new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion 
    }


    /// <summary>
    /// Simplistic delegate command for the demo.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        public Action CommandAction { get; set; }
        public Func<bool> CanExecuteFunc { get; set; }

        public void Execute(object parameter)
        {
            CommandAction();
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteFunc == null || CanExecuteFunc();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}

