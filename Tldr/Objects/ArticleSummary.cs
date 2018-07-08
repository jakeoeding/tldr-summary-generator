namespace Tldr.Objects
{
    public class ArticleSummary
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }

        public ArticleSummary(string url, string title, string summary)
        {
            Url = url;
            Title = title;
            Summary = summary;
        }
    }
}
