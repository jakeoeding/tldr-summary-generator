using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tldr.Objects;

namespace Tldr.Tests
{
    [TestFixture]
    public class ArticleSummarizerTests
    {
        [TestCase("http://www.abc.net.au/news/2018-07-06/rescue-worker-helping-thai-boys-in-cave-dies/9949622", true)]
        [TestCase("http://www.abc.net.au/", true)]
        [TestCase("www.google.com", false)]
        [TestCase("test.com", false)]
        [TestCase("a", false)]
        public void ValidateUrl_Url_ReturnsBool(string url, bool expectedResult)
        {
            var result = ArticleSummarizer.ValidateUrl(url);

            Assert.That(result, Is.EqualTo(expectedResult));
        }


        [TestCase("TEST", "test")]
        [TestCase("test", "test")]
        [TestCase("Test", "test")]
        public void CleanText_TextWithUpperCase_ReturnsLowerCaseText(string input, string expectedResult)
        {
            var result = ArticleSummarizer.CleanText(input);

            Assert.That(result, Is.EqualTo(expectedResult));
        }


        [TestCase("te-st", "test")]
        [TestCase("te—st", "test")]
        [TestCase("te.st", "test")]
        [TestCase("te...st", "test")]
        [TestCase("test's", "tests")]
        [TestCase("test,", "test")]
        [TestCase("\"test\"", "test")]
        public void CleanText_TextWithPuncuation_ReturnsTextWithoutPunctuation(string input, string expectedResult)
        {
            var result = ArticleSummarizer.CleanText(input);

            Assert.That(result, Is.EqualTo(expectedResult));
        }


        [Test]
        public void CountWords_ListOfNonStopWords_ReturnsProperCount()
        {
            ArticleSummarizer.LoadStopWords();

            var words = new List<string>(new string[] { "test", "test", "trial" });
            var expectedWordCounts = new Dictionary<string, int>
            {
                { "test", 2 },
                { "trial", 1 }
            };

            var wordCounts = ArticleSummarizer.CountWords(words);

            Assert.That(wordCounts, Is.EqualTo(expectedWordCounts));

        }


        [Test]
        public void CountWords_ListContainsStopWords_ReturnsProperCountExcludingStopWords()
        {
            ArticleSummarizer.LoadStopWords();

            var words = new List<string>(new string[] { "test", "test", "trial", "a", "an", "the" });
            var expectedWordCounts = new Dictionary<string, int>
            {
                { "test", 2 },
                { "trial", 1 }
            };

            var result = ArticleSummarizer.CountWords(words);

            Assert.That(result, Is.EqualTo(expectedWordCounts));
        }


        [Test]
        public void SortWords_DictionaryOfWordCounts_ReturnsProperCount()
        {
            var wordCounts = new Dictionary<string, int>
            {
                { "test", 2 },
                { "challenge", 5},
                { "trial", 1 }
            };

            string[] expectedOrder = { "challenge", "test" };

            var result = ArticleSummarizer.SortWords(wordCounts);

            Assert.That(result, Is.EqualTo(expectedOrder));
        }


        [TestCase("a", 5)]
        [TestCase("b", 4)]
        [TestCase("e", 3)]
        [TestCase("i", 2)]
        [TestCase("m", 1)]
        [TestCase("q", 0)]
        [TestCase("r", 0)]
        [TestCase("s", 0)]
        [TestCase("t", 0)]
        public void ScoreWords_ArrayOfWords_WordsHaveProperScores(string input, int expectedResult)
        {
            string[] sortedWords = { "a", "b", "c", "d", "e",
                                     "f", "g", "h", "i", "j",
                                     "k", "l", "m", "n", "o",
                                     "p", "q", "r", "s", "t" };

            var wordScores = ArticleSummarizer.ScoreWords(sortedWords);

            int result;
            if (wordScores.ContainsKey(input))
                result = wordScores[input];
            else
                result = 0;

            Assert.That(result, Is.EqualTo(expectedResult));
        }


        [TestCase("This is a test.", 5)]
        [TestCase("The scores must match.", 7)]
        [TestCase("Is a this that the.", 0)]
        public void ScoreSentences_WordScores_SentenceScoreMatchesSumOfWordScores(string input, int expectedResult)
        {
            string[] sentences = { "This is a test.", "The scores must match.", "Is a this that the." };

            var wordScores = new Dictionary<string, int>
            {
                {"test", 5 },
                {"scores", 4 },
                {"match", 3 }
            };

            var sentenceScores = ArticleSummarizer.ScoreSentences(sentences, wordScores);

            Assert.That(sentenceScores[input], Is.EqualTo(expectedResult));
        }


        [TestCase(0.2, new string[] {"correct"})]
        [TestCase(0.4, new string[] { "correct", "results" })]
        public void SortSentences_ScoredSentences_ResultsAreSortedAndContainProperAmountOfSentences(double input, string[] expectedResult)
        {
            var sentenceScores = new Dictionary<string, int>
            {
                {"test", 1 },
                {"scores", 2 },
                {"match", 3 },
                {"results", 4 },
                {"correct", 5 }
            };

            var result = ArticleSummarizer.SortSentences(sentenceScores, input);

            Assert.That(result, Is.EqualTo(expectedResult));
        }


        [Test]
        public void BuildSummary_SentenceList_SummaryContainsOnlyFinalSentences()
        {
            string[] rawSentences = { "test", "trial" };
            string[] finalSentences = { "test" };

            var result = ArticleSummarizer.BuildSummary(rawSentences, finalSentences);

            Assert.That(result, Is.EqualTo(finalSentences[0]+". \n\n"));
        }
    }
}
