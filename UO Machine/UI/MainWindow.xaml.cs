using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using System.Reflection;
using System.IO;
using Microsoft.Win32;
using UOMachine.Tree;
using UOMachine.Utility;
using UOMachine.Data;
using UOMachine.Resources;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;

namespace UOMachine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : System.Windows.Window
    {
        private About aboutWindow;
        private OptionsWindow optionsWindow;
        private AssemblyPicker assemblyPicker;
        private static OptionsData myCurrentOptions;
        public static OptionsData CurrentOptions
        {
            get { return myCurrentOptions; }
        }
        private const string titleString = "UOMachine";
        private string fileTitleString = "";
        private string fileName = "";
        private delegate void dUpdateButtonStatus( Button button, bool IsEnabled );
        private delegate void dUpdateLabel( Label label, string content );
        private CSharpCompletion codeCompletion;

        private object myOptionWaitObject = new object();
        private bool myIsWaitingForOptions = false;

        private FoldingManager foldingManager;

        private RelayCommand mySaveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (mySaveCommand == null)
                {
                    mySaveCommand = new RelayCommand( param => this.Save(), param => this.CanSave );
                }
                return mySaveCommand;
            }
        }

        private bool CanSave
        {
            get
            {
                MenuItem parent = (MenuItem) menu1.Items[0];
                MenuItem child = (MenuItem) parent.Items[2];
                return child.IsEnabled;
            }
        }

        private void Save()
        {
            FileSave_Click( this, null );
        }

        private bool waitingForOptions
        {
            set { myIsWaitingForOptions = value; }
            get { return ThreadHelper.VolatileRead( ref myIsWaitingForOptions ); }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Initialize();
        }

        private void Initialize()
        {
            UpdateButtonStatus( stopButton, false );
            UpdateButtonStatus( razorButton, false );
            UpdateButtonStatus( addButton, false );

            Closing += new CancelEventHandler( MainWindow_Closing );
            TreeViewUpdater.Initialize( clientTreeView );
            OptionsWindow.OptionsChangedEvent += new OptionsWindow.dOptionsChanged( OptionsWindow_OptionsChangedEvent );
            OptionsWindow.OptionsCancelledEvent += new OptionsWindow.dOptionsCancelled( OptionsWindow_OptionsCancelledEvent );
            ScriptCompiler.ScriptFinishedEvent += new ScriptCompiler.dScriptFinished( ScriptCompiler_ScriptFinished );
            aboutWindow = new About();
            optionsWindow = new OptionsWindow();
            myCurrentOptions = OptionsData.Deserialize( "options.xml" );
            CheckOptions( myCurrentOptions );
            if (!UOM.Initialize( this ))
            {
                MessageBox.Show( Strings.Errorinitializing, Strings.Error );
                UOM.Dispose();
                UOM.ShutDown();
                return;
            }
            PrepareTextEditor();
            UpdateButtonStatus( addButton, true );
        }

        private void OptionsWindow_OptionsCancelledEvent()
        {
            if (waitingForOptions)
            {
                MessageBox.Show( Strings.Youmustentervalidoptions, Strings.Error );
                waitingForOptions = false;
            }
        }

        private void CheckOptions( OptionsData optionsData )
        {
            if (!optionsData.IsValid())
            {
                waitingForOptions = true;
                MessageBox.Show( Strings.Invalidvaluepresentinoptions, Strings.Error );
                optionsWindow.LoadOptions( OptionsData.CreateDefault() );
                optionsWindow.Show();
                while (waitingForOptions)
                {
                    try { Application.Current.Dispatcher.Invoke( DispatcherPriority.Background, new ThreadStart( delegate { } ) ); }
                    catch { }
                    Thread.Sleep( 50 );
                }
            }

            UpdateButtonStatus( razorButton, false );
            UpdateButtonStatus( steamButton, false );

            if (optionsData.IsRazorValid())
            {
                UpdateButtonStatus( razorButton, true );
            }

            if (optionsData.IsSteamValid())
            {
                UpdateButtonStatus( steamButton, true );
            }
        }

        private void MainWindow_Closing( object sender, CancelEventArgs e )
        {
            UOM.SetStatusLabel( Strings.Closing );
            UOM.Dispose();
            aboutWindow.CancelEnabled = false;
            optionsWindow.CancelEnabled = false;
            aboutWindow.Close();
            optionsWindow.Close();
            if (assemblyPicker != null)
            {
                assemblyPicker.CancelEnabled = false;
                assemblyPicker.Close();
            }
            App.Current.Shutdown();
        }

        private void PrepareTextEditor()
        {
            codeCompletion = new ICSharpCode.CodeCompletion.CSharpCompletion();
            codeCompletion.AddAssembly( "UOMachine.exe" );
            scriptTextBox.Completion = codeCompletion;
            scriptTextBox.FontFamily = new FontFamily( "Consolas" );
            scriptTextBox.FontSize = 12;
            scriptTextBox.TextArea.DefaultInputHandler.NestedInputHandlers.Add( new SearchInputHandler( scriptTextBox.TextArea ) );
            FileNew_Click( null, null );
            scriptTextBox.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition( "C#" );
            scriptTextBox.Options = myCurrentOptions.TextEditorOptions;
            foldingManager = FoldingManager.Install( scriptTextBox.TextArea );
            new BraceFoldingStrategy().UpdateFoldings( foldingManager, scriptTextBox.Document );
        }

        private void OptionsWindow_OptionsChangedEvent( OptionsData optionsData )
        {
            if (waitingForOptions)
            {
                if (optionsData.IsValid())
                {
                    waitingForOptions = false;
                }
                else
                {
                    MessageBox.Show( Strings.Invalidvaluepresentinoptions, Strings.Error );
                    optionsWindow.Show();
                }
            }
            if (optionsData.CacheLevel != myCurrentOptions.CacheLevel)
                Map.Initialize( optionsData.UOFolder, optionsData.CacheLevel );
            myCurrentOptions = optionsData;
            CheckOptions( optionsData );
            scriptTextBox.Options = optionsData.TextEditorOptions;
        }

        private void ScriptCompiler_ScriptFinished()
        {
            UpdateButtonStatus( startButton, true );
            UpdateButtonStatus( stopButton, false );
            UOM.SetStatusLabel( Strings.Scriptstopped );
        }

        private static void myUpdateLabel( Label label, string content ) { label.Content = content; }

        public static void UpdateLabel( Label label, string content )
        {
            if (label.CheckAccess()) myUpdateLabel( label, content );
            else label.Dispatcher.BeginInvoke( new dUpdateLabel( myUpdateLabel ), new object[] { label, content } );
        }

        private void myUpdateButtonStatus( Button button, bool IsEnabled )
        {
            button.IsEnabled = IsEnabled;
            button.Opacity = IsEnabled == true ? 1 : 0.5;
        }

        private void UpdateButtonStatus( Button button, bool IsEnabled )
        {
            if (button.CheckAccess()) myUpdateButtonStatus( button, IsEnabled );
            else button.Dispatcher.BeginInvoke( new dUpdateButtonStatus( myUpdateButtonStatus ), new object[] { button, IsEnabled } );
        }

        private void startButton_Click( object sender, RoutedEventArgs e )
        {
            if (ScriptCompiler.Compile( Assembly.GetExecutingAssembly().Location, scriptTextBox.Text ))
            {
                UOM.SetStatusLabel( Strings.Scriptrunning );
                UpdateButtonStatus( startButton, false );
                UpdateButtonStatus( stopButton, true );
            }
            else UOM.SetStatusLabel( Strings.Compilererror );
        }

        private void addButton_Click( object sender, RoutedEventArgs e )
        {
            int index;
            if (ClientLauncher.Launch( MainWindow.CurrentOptions, out index ))
            {
                UOM.SetStatusLabel( Strings.Clientstarted );
            }
            else
            {
                UOM.SetStatusLabel( Strings.Errorstartingclient );
                MessageBox.Show( Strings.Errorstartingclient, Strings.Error );
            }

        }

        private void process_Exited( object sender, EventArgs e )
        {
            UOM.SetStatusLabel( Strings.Clientexited );
        }

        private void stopButton_Click( object sender, RoutedEventArgs e )
        {
            UOM.SetStatusLabel( Strings.Stoppingscript );
            ScriptCompiler.StopScript();
            Events.General.ClearEvents();
            Events.IncomingPackets.ClearEvents();
            Events.OutgoingPackets.ClearEvents();
            Events.LowLevel.ClearEvents();
            UpdateButtonStatus( startButton, true );
            UpdateButtonStatus( stopButton, false );
            UOM.SetStatusLabel( Strings.Ready );
        }

        private void razorButton_Click( object sender, RoutedEventArgs e )
        {
            int index;
            UpdateButtonStatus( razorButton, false );
            UpdateButtonStatus( addButton, false );
            RazorLauncher.Launch( myCurrentOptions, out index );
            UpdateButtonStatus( razorButton, true );
            UpdateButtonStatus( addButton, true );
        }

        private void FileNew_Click( object sender, RoutedEventArgs e )
        {
            scriptTextBox.Text = Properties.Resources.DefaultScript;
            fileName = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) + "\\Untitled.cs";
            fileTitleString = "Untitled.cs";
            MenuItem parent = (MenuItem) menu1.Items[0];
            MenuItem child = (MenuItem) parent.Items[2];
            child.IsEnabled = false;
            UpdateWindowText();
            scriptTextBox.Document.FileName = fileName;
        }

        private void FileOpen_Click( object sender, RoutedEventArgs e )
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.CheckFileExists = true;
            OFD.CheckPathExists = true;
            OFD.Filter = "(*.txt, *.cs)|*.txt;*.cs|(*.*)|*.*";
            OFD.FileOk += new CancelEventHandler( OFD_FileOk );
            OFD.ShowDialog();
        }

        private void OFD_FileOk( object sender, CancelEventArgs e )
        {
            OpenFileDialog OFD = (OpenFileDialog) sender;
            if (OFD.FileName != "")
            {
                try
                {
                    //scriptTextBox.Text = File.ReadAllText(OFD.FileName);
                    scriptTextBox.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition( "C#" );
                    scriptTextBox.OpenFile( OFD.FileName );
                    new BraceFoldingStrategy().UpdateFoldings( foldingManager, scriptTextBox.Document );
                }
                catch (IOException)
                {
                    MessageBox.Show( Strings.Erroropeningfile );
                    return;
                }
                MenuItem parent = (MenuItem) menu1.Items[0];
                MenuItem child = (MenuItem) parent.Items[2];
                child.IsEnabled = true;
                fileName = OFD.FileName;
                fileTitleString = OFD.SafeFileName;
                UpdateWindowText();
            }
        }

        private void FileSave_Click( object sender, RoutedEventArgs e )
        {
            if (fileName != "")
            {
                try { File.WriteAllText( fileName, scriptTextBox.Text, Encoding.UTF8 ); }
                catch (IOException)
                {
                    MessageBox.Show( Strings.Errorsavingfile );
                }
            }
            else FileSaveAs_Click( null, null );
        }

        private void FileSaveAs_Click( object sender, RoutedEventArgs e )
        {
            SaveFileDialog SFD = new SaveFileDialog();
            SFD.CheckPathExists = true;
            if (fileName != "") SFD.FileName = fileName;
            SFD.DefaultExt = ".cs";
            SFD.Filter = "(*.cs)|*.cs|(*.*)|*.*";
            SFD.FileOk += new CancelEventHandler( SFD_FileOk );
            SFD.ShowDialog();
        }

        private void SFD_FileOk( object sender, CancelEventArgs e )
        {
            SaveFileDialog SFD = (SaveFileDialog) sender;
            if (SFD.FileName != "")
            {
                try { File.WriteAllText( SFD.FileName, scriptTextBox.Text, Encoding.UTF8 ); }
                catch (IOException)
                {
                    MessageBox.Show( Strings.Errorsavingfile );
                    return;
                }
                MenuItem parent = (MenuItem) menu1.Items[0];
                MenuItem child = (MenuItem) parent.Items[2];
                child.IsEnabled = true;
                fileName = SFD.FileName;
                fileTitleString = SFD.SafeFileName;
                UpdateWindowText();
            }
        }

        private void FileExit_Click( object sender, RoutedEventArgs e )
        {
            Application.Current.Shutdown();
        }

        private void UpdateWindowText()
        {
            Title = titleString + " (" + fileTitleString + ")";
        }

        private void HelpAbout_Click( object sender, RoutedEventArgs e )
        {
            aboutWindow.Show();
        }

        private void EditOptions_Click( object sender, RoutedEventArgs e )
        {
            optionsWindow.LoadOptions( myCurrentOptions );
            optionsWindow.Show();
        }

        private void FormatDocument_Click( object sender, RoutedEventArgs e )
        {
            CSharpFormattingOptions policy = FormattingOptionsFactory.CreateAllman();
            CSharpParser parser = new CSharpParser();
            SyntaxTree tree = parser.Parse( scriptTextBox.Text );
            StringWriter writer = new StringWriter();
            CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor( writer, policy );
            outputVisitor.VisitSyntaxTree( tree );

            scriptTextBox.Text = writer.ToString();
        }

        private void AddReference_Click( object sender, RoutedEventArgs e )
        {
            if (assemblyPicker == null)
            {
                assemblyPicker = new AssemblyPicker();
                assemblyPicker.AssemblySelectedEvent += new AssemblyPicker.dAssemblySelected( assemblyPicker_AssemblySelectedEvent );
            }
            assemblyPicker.Show();
        }

        private void assemblyPicker_AssemblySelectedEvent( string fileName )
        {
            scriptTextBox.Text = "/* <AREF = \"" + fileName + "\"> */\r\n" + scriptTextBox.Text;
        }

        private void steamButton_Click( object sender, RoutedEventArgs e )
        {
            int index;
            UpdateButtonStatus( steamButton, false );
            UpdateButtonStatus( addButton, false );
            Task.Factory.StartNew( () =>
            {
                SteamLauncher.Launch( myCurrentOptions, out index );
            } );
            UpdateButtonStatus( steamButton, true );
            UpdateButtonStatus( addButton, true );
        }

        private void checkUpdate_Click( object sender, RoutedEventArgs e )
        {

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
            startInfo.FileName = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ), "Updater.exe" );
            NativeMethods.SafeProcessHandle hProcess;
            NativeMethods.SafeThreadHandle hThread;
            uint pid, tid;
            UOM.SetStatusLabel( "Launching Updater" );
            NativeMethods.CreateProcess( startInfo, false, out hProcess, out hThread, out pid, out tid );
        }

        private void apiDoc_Click( object sender, RoutedEventArgs e )
        {
            string path = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ), @"UOMachine.chm" );
            if (System.IO.File.Exists( path ))
            {
                System.Diagnostics.Process.Start( path );
            }
        }

        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public RelayCommand( Action<object> execute ) : this( execute, null )
            {
            }

            public RelayCommand( Action<object> execute, Predicate<object> canExecute )
            {
                if (execute == null)
                    throw new ArgumentNullException( "execute" );

                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute( object parameter )
            {
                return _canExecute == null ? true : _canExecute( parameter );
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute( object parameter )
            {
                _execute( parameter );
            }
        }
    }
}
