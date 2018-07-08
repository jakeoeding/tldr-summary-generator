using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Tldr.Objects
{
    public static class ArticleSummarizer
    {
        public static int SummariesGenerated { get; set; }
        public static List<string> StopWords;


        /// <summary>
        /// Loads stop words (e.g. a/an/the...) from resource file for exclusion from analysis
        /// </summary>
        public static void LoadStopWords()
        {
            // Load all text from resource file
            string stopWordsBlob = Tldr.StopWords.stop_words;

            // Split the text on newline character
            string[] individualStopWords = stopWordsBlob.Split(new[] {"\r\n"}, StringSplitOptions.None);

            StopWords = individualStopWords.ToList<string>();
        }


        /// <summary>
        /// Grabs specified nodes from given URL
        /// </summary>
        public static HtmlNodeCollection GrabNodes(string url, string nodeSelection)
        {
            // Grab the HTML tags that fit the criteria (nodeSelection) from the given url
            var web = new HtmlWeb();

            var doc = web.Load(url);
            
            return doc.DocumentNode.SelectNodes(nodeSelection);
        }


        /// <summary>
        /// Checks whether or not given URL is of a valid format
        /// </summary>
        public static bool ValidateUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri tempUri);
        }


        /// <summary>
        /// Pulls text from HTML nodes and combines into one string
        /// </summary>
        public static string ExtractText(HtmlNodeCollection tags)
        {
            // Given a set of tags, this method will return the text from within
            // them in a single string 'text'.

            // We will remove the articles metadata tags as they do not pertain
            // to the article's text

            StringBuilder text = new StringBuilder();

            // Starting at 1 because 0 is 'published info'
            for (int i = 1; i < tags.Count; i++)
            {
                // The next element after the published info to contain a class
                // attribute is the 'topics' tag, which is directly after the
                // last <p> in the article's main text.

                // Once we find this, we will stop the loop because the have
                // reached the end of the article
                if (tags[i].Attributes.Contains("class"))
                    break;
                text.Append(tags[i].InnerText);
            }

            return text.ToString();
        }


        /// <summary>
        /// Removes punctuation and case
        /// </summary>
        public static string CleanText(string text)
        {
            string textWithoutPunctuation = Regex.Replace(text, @"(\p{P})", "");

            return textWithoutPunctuation.ToLower();
        }


        /// <summary>
        /// Find the number of occurances of each non-stop word
        /// </summary>
        public static Dictionary<string, int> CountWords(List<string> words)
        {
            var wordCounts = new Dictionary<string, int>();

            // Traverse the word list, incrementing for each non-stop word's occurance
            foreach (var word in words)
            {
                if (!wordCounts.ContainsKey(word))
                {
                    // Check to see if current word is a stop word or not
                    if (!StopWords.Contains(word))
                        wordCounts[word] = 1;
                }
                else
                {
                    wordCounts[word] += 1;
                }
            }

            return wordCounts;
        }


        /// <summary>
        /// Sorts all words that occur more than once by count descending
        /// </summary>
        public static string[] SortWords(Dictionary<string, int> wordCounts)
        {
            var sortedWords = from word in wordCounts
                              where word.Value > 1
                              orderby word.Value descending
                              select word.Key;

            return sortedWords.ToArray();
        }


        /// <summary>
        /// Assigns a score for each word based on what percentile
        /// it falls in when ordered by count descending
        /// </summary>
        public static Dictionary<string, int> ScoreWords(string[] sortedWords)
        {
            var wordScores = new Dictionary<string, int>();
            int count = sortedWords.Length;
            int score;

            for (int i = 0; i < count; i++)
            {
                if (i < 0.05 * count)
                    score = 5;
                else if (i < 0.2 * count)
                    score = 4;
                else if (i < 0.4 * count)
                    score = 3;
                else if (i < 0.6 * count)
                    score = 2;
                else if (i < 0.8 * count)
                    score = 1;
                else
                    break;

                wordScores.Add(sortedWords[i], score);
            }

            return wordScores;
        }


        /// <summary>
        /// Scores sentences
        /// </summary>
        public static Dictionary<string, int> ScoreSentences(string[] sentences, Dictionary<string, int> wordScores)
        {
            var sentenceScores = new Dictionary<string, int>();

            for (int i = 0; i < sentences.Length; i++)
            {
                // Remove punctuation and case from the sentence
                string cleanSentence = CleanText(sentences[i]);

                // Separate words
                string[] cleanSentenceWords = cleanSentence.Split();

                int score = 0;

                // Tally the score of sentence by summing scores of each word in sentence
                foreach (var word in cleanSentenceWords)
                {
                    if (wordScores.ContainsKey(word))
                        score += wordScores[word];
                }

                // Make sure no duplicate additions are attempted, e.g. two emtpy strings
                if (!sentenceScores.ContainsKey(sentences[i]))
                    sentenceScores.Add(sentences[i], score);
            }

            return sentenceScores;
        }


        /// <summary>
        /// Returns only the top XX% of sentences specified by percentToKeep
        /// </summary>
        public static string[] SortSentences(Dictionary<string, int> sentenceScores, double percentToKeep = 0.4)
        {
            // Find the top XX% of sentences when ranked by score descending
            var topSentences = (from sentence in sentenceScores
                                orderby sentence.Value descending
                                select sentence.Key).Take(Convert.ToInt16(percentToKeep * sentenceScores.Count));

            return topSentences.ToArray();
        }


        /// <summary>
        /// Builds an ordered summary from the final sentences
        /// </summary>
        public static string BuildSummary(string[] rawSentences, string[] finalSentences)
        {
            StringBuilder summary = new StringBuilder();

            foreach (var sentence in rawSentences)
            {
                if (finalSentences.Contains(sentence))
                {
                    summary.Append(sentence);
                    summary.Append(". \n\n");
                }
            }

            return summary.ToString();
        }


        /// <summary>
        /// Generate an ArticleSummary object from a URL
        /// </summary>
        public static ArticleSummary Generate(string url)
        {
            // Load stop words if a summary has not been generated yet
            if (SummariesGenerated == 0)
            {
                LoadStopWords();
            }
            
            // Validate URL and return dummy summary if invalid
            if (!ValidateUrl(url))
                return new ArticleSummary(url, "UNKNOWN", "Invalid URL - No summary could be generated.");
            
            // Set title from specified webpage header, otherwise assign default
            string title;

            try
            {
                // Pull all h1 tags from url
                var h1Tags = GrabNodes(url, "//h1");

                // Title is the last h1 on the page
                title = h1Tags[h1Tags.Count-1].InnerText;                
            }
            catch (NullReferenceException)
            {
                title = "UNAVAILABLE";
            }

            // Pull the HTML tags specified by nodeSelection from given url
            var pTags = GrabNodes(url, "//p");

            // Grab the inner text from the HTML tags and combine into one string
            string text = ExtractText(pTags);

            // Remove punctuation and case
            string cleanText = CleanText(text);

            // Find each individual word from the text and remove blanks
            var words = new List<string>(cleanText.Split());
            words.RemoveAll(word => word == "");

            // Tally up word counts for non-stop words
            var wordCounts = CountWords(words);

            // Sort words that occur more than once by word count, descending
            var sortedWords = SortWords(wordCounts);

            // Assign a point value to each word based on its percentile
            var wordScores = ScoreWords(sortedWords);

            // Obtain each indiviual sentence, separating on periods
            string[] rawSentences = text.Split('.');

            // Score each individual sentence
            var sentenceScores = ScoreSentences(rawSentences, wordScores);

            // Score and sort sentences by highest score, only keep top XX%
            var finalSentences = SortSentences(sentenceScores);

            // Traverse the sentences array. If the sentence falls in
            // the top XX% (finalSentences), then add it to the summary
            var summary = BuildSummary(rawSentences, finalSentences);

            SummariesGenerated++;

            return new ArticleSummary(url, title, summary);
        }
    }
}
