using System.Windows;
using System.Speech.Synthesis;
using Tldr.Objects;

namespace Tldr
{
    /// <summary>
    /// Interaction logic for SummaryWindow.xaml
    /// </summary>
    public partial class SummaryWindow : Window
    {
        public ArticleSummary Article { get; set; }
        public SpeechSynthesizer Reader = new SpeechSynthesizer();

        public SummaryWindow(ArticleSummary article)
        {
            Article = article;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Upon loading page, display corresponding article title, summary, and URL
            LabelTitle.Content = Article.Title;
            TextboxSummary.Text = Article.Summary;
            LabelUrl.Content = Article.Url;
        }

        private void ButtonSpeak_Click(object sender, RoutedEventArgs e)
        {
            if (Reader.State == SynthesizerState.Ready)
            {
                Reader.SpeakAsync(Article.Title);
                Reader.SpeakAsync(Article.Summary);
            }
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            if (Reader.State == SynthesizerState.Speaking)
                Reader.Pause();
        }

        private void ButtonResume_Click(object sender, RoutedEventArgs e)
        {
            if (Reader.State == SynthesizerState.Paused)
                Reader.Resume();
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            Reader.Dispose();
            Reader = new SpeechSynthesizer();
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            var obj = App.Current as App;

            // Hide current window
            this.Hide();

            if (obj.Articles.Count > 0)
            {
                // Initialize new summary window with first article in queue
                var summary = new SummaryWindow(obj.Articles.Dequeue());

                summary.ShowDialog();
            }
            else
            {
                // Loop back to submission window
                var mainWindow = new MainWindow();

                mainWindow.ShowDialog();
            }
        }
    }
}
