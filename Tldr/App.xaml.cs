using System.Collections.Generic;
using System.Windows;
using Tldr.Objects;

namespace Tldr
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public Queue<ArticleSummary> Articles = new Queue<ArticleSummary>();
    }
}
