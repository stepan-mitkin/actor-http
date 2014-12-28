using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Actors;
using System.Threading;

namespace ActorGui
{
    public class MainWindow : Window, IMainWindow
    {
        private readonly TextBox _urlEdit;
        private readonly TextBox _primesEdit;
        private readonly Button _startIoButton;
        private readonly Button _startCpuButton;
        private readonly Button _cancelButton;
        private readonly PumpAnimation _animationControl;
        private const string UiThread = "UI Thread";
        private const string CalculationThread = "Calculation Thread";

        private readonly int Logic;

        private readonly Runtime _runtime;

        public MainWindow(Runtime runtime)
        {
            _runtime = runtime;
            _runtime.RegisterExternalThread(UiThread, this.PostWorkItem);
            var logicActor = new GuiMachines.MainWindowLogic();
            logicActor.Window = this;
            Logic = _runtime.AddActorToThread(UiThread, logicActor);

            Width = 600;
            FontSize = 16;
            SizeToContent = System.Windows.SizeToContent.Height;
            Title = "Actor-based GUI";

            StackPanel rootPanel = new StackPanel();
            rootPanel.Orientation = Orientation.Vertical;
            AddChild(rootPanel);

            GroupBox ioBox = AddGroupBox(rootPanel, "IO-bound task");
            Button startIoButton = CreateButton("Download", StartIo);
            _urlEdit = CreateTextBox("http://www.vg.no/");
            FillGroupBox(ioBox, _urlEdit, startIoButton);
            _startIoButton = startIoButton;

            GroupBox cpuBox = AddGroupBox(rootPanel, "CPU-bound task");
            Button startCpuButton = CreateButton("Calculate primes", StartCpu);
            _primesEdit = CreateTextBox("10000000");
            FillGroupBox(cpuBox, _primesEdit, startCpuButton);
            _startCpuButton = startCpuButton;

            GroupBox aniBox = AddGroupBox(rootPanel, "Progress");
            _animationControl = new PumpAnimation();
            _animationControl.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            Button cancelButton = CreateButton("Cancel", Cancel);
            cancelButton.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            _cancelButton = cancelButton;
            FillGroupBox(aniBox, _animationControl, cancelButton);

            _animationControl.Visibility = System.Windows.Visibility.Hidden;
        }

        private void PostWorkItem(Action item)
        {
            Dispatcher.BeginInvoke(item);
        }

        private static TextBox CreateTextBox(string text)
        {
            TextBox textBox = new TextBox();
            textBox.Text = text;
            textBox.Margin = new Thickness(5);
            return textBox;
        }

        private static Button CreateButton(string text, Action callback)
        {
            Button button = new Button();
            button.Content = text;
            button.Margin = new Thickness(5);
            button.Click += (sender, e) => callback();
            return button;
        }

        private void StartCpu()
        {
            string nText = _primesEdit.Text.Trim();
            if (nText == String.Empty)
            {
                ReportError("Please input the maximum number.");
                return;
            }

            int n;
            if (!Int32.TryParse(nText, out n))
            {
                ReportError("Incorrect maximum number.");
                return;
            }

            if (n <= 0)
            {
                ReportError("The maximum number must be posiive.");
                return;
            }

            _runtime.SendMessage(Logic, GuiMachines.Prime, n, 0);
        }

        private void StartIo()
        {
            string url = this._urlEdit.Text.Trim();
            _runtime.SendMessage(Logic, GuiMachines.Download, url, 0);
        }

        private void Cancel()
        {
            _runtime.SendMessage(Logic, GuiMachines.Cancel, null, 0);
        }

        public void SwitchToWorking()
        {
            _animationControl.Visibility = System.Windows.Visibility.Visible;
            _cancelButton.IsEnabled = true;
            _startCpuButton.IsEnabled = false;
            _startIoButton.IsEnabled = false;
            _primesEdit.IsEnabled = false;
            _urlEdit.IsEnabled = false;
            _animationControl.Start();
        }

        public void SwitchToReady()
        {
            _animationControl.Visibility = System.Windows.Visibility.Hidden;
            _cancelButton.IsEnabled = false;
            _startCpuButton.IsEnabled = true;
            _startIoButton.IsEnabled = true;
            _primesEdit.IsEnabled = true;
            _urlEdit.IsEnabled = true;
            _animationControl.Stop();
        }

        private GroupBox AddGroupBox(StackPanel panel, string text)
        {
            Label label = new Label();
            label.Content = text;
            GroupBox group = new GroupBox();
            group.Margin = new Thickness(5);
            group.Header = label;
            panel.Children.Add(group);
            return group;
        }

        private void FillGroupBox(GroupBox groupBox, UIElement left, Button right)
        {
            Grid grid = new Grid();
            groupBox.Content = grid;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.3, GridUnitType.Star) });
            Grid.SetColumn(left, 0);
            Grid.SetColumn(right, 1);
            grid.Children.Add(left);
            grid.Children.Add(right);
        }

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            DefaultErrorHandler errorHandler = new DefaultErrorHandler();
            DebugLogger logger = new DebugLogger();
            using (Runtime runtime = new Runtime(logger, errorHandler))
            {
                MainWindow window = new MainWindow(runtime);
                app.Run(window);
            }
        }

        public void ReportError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ReportResult(string result)
        {
            TextWindow dialog = new TextWindow(result);
            dialog.ShowDialog();
        }
    }
}
