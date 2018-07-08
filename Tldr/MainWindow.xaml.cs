using System.Windows;
using Tldr.Objects;

namespace Tldr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (TextboxUrlInput.Text.Length > 0)
            {
                // Grab URLs entered by user
                var rawUrls = TextboxUrlInput.Text;
                string[] urls = rawUrls.Split('\n');
                var obj = App.Current as App;

                // Generate a summary for each URL given and add to queue
                foreach (var url in urls)
                {
                    ArticleSummary tempArticle = ArticleSummarizer.Generate(url.Trim('\r'));
                    obj.Articles.Enqueue(tempArticle);
                }

                // Launch the first summary window
                OpenNewWindow();
            }
        }

        private void OpenNewWindow()
        {
            var obj = App.Current as App;

            // Hide submit window
            this.Hide();

            // Initialize new summary window with first article in queue
            var summary = new SummaryWindow(obj.Articles.Dequeue());

            // Display summary window
            summary.ShowDialog();
        }
    }
}
