using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorGui
{
    public class TextWindow : Window
    {
        public TextWindow(string text)
        {
            Width = 400;
            this.SizeToContent = System.Windows.SizeToContent.Height;
            StackPanel rootStack = new StackPanel();
            this.Content = rootStack;
            rootStack.Orientation = Orientation.Vertical;

            TextBox textControl = new TextBox();
            textControl.TextWrapping = TextWrapping.Wrap;
            textControl.AcceptsReturn = true;
            textControl.Text = text;
            textControl.Height = 300;
            textControl.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            rootStack.Children.Add(textControl);

            Button closeButton = new Button();
            closeButton.Content = "Close";
            closeButton.Margin = new Thickness(5);
            closeButton.Padding = new Thickness(5);
            closeButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            closeButton.Click += new RoutedEventHandler((a, b) => this.Close());
            rootStack.Children.Add(closeButton);
        }
    }
}
