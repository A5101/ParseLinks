using Parse.Domain.Entities;
using Parse.Service;
using System.Text.RegularExpressions;
namespace ParseTests
{
    [TestFixture]
    public class RegexMatchesTests
    {
        private string sampleHtml = @"
        <html>
            <head>
                <title>Sample Title</title>
            </head>
            <body>
                <div class="" article content"">
                    <a href=""http://example.com/page1"">Link 1</a>
                    <a href=""http://example.com/page2"">Link 2</a>
                    <a href=""http://example.com/page3"">Link 3</a>
                </div>
                <div class=""article"">
                    <p>This is a sample article.</p>
                </div>
            </body>
        </html>";

        private string sampleXml = @"
        <root>
            <item>
                <name>Item 1</name>
                <description>Description for Item 1</description>
            </item>
            <item>
                <name>Item 2</name>
                <description>Description for Item 2</description>
            </item>
        </root>";

        [Test]
        public void GetHrefs_ReturnsCorrectMatchCount()
        {
            MatchCollection matches = RegexMatches.GetHrefs(sampleHtml);
            Assert.AreEqual(3, matches.Count);
        }

        [Test]
        public void GetTitle_ReturnsCorrectTitle()
        {
            string title = RegexMatches.GetTitle(sampleHtml);
            Assert.AreEqual("Sample Title", title);
        }

        [Test]
        public async Task GetTextContent_ReturnsNonEmptyString()
        {
            string textContent = await RegexMatches.GetTextContent(sampleHtml);
            Assert.IsFalse(string.IsNullOrEmpty(textContent));
        }

        [Test]
        public void GetItemXmlNodes_ReturnsCorrectMatchCount()
        {
            MatchCollection matches = RegexMatches.GetItemXmlNodes(sampleXml);
            Assert.AreEqual(2, matches.Count); // 2 <item> nodes
        }

        [Test]
        public void GetConcreteXmlNodes_ReturnsCorrectNode()
        {
            Match match = RegexMatches.GetConcreteXmlNodes(sampleXml, "name");
            Assert.IsTrue(match.Success);
            Assert.AreEqual("<name>Item 1</name>", match.Value.Trim());
        }

        [Test]
        public void TestGetHrefs()
        {
            string html = "<a href=\"https://www.example.com\">Example</a>";
            MatchCollection matches = RegexMatches.GetHrefs(html);
            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual("https://www.example.com", matches[0].Groups[1].Value);
        }

        [Test]
        public void TestGetMeta()
        {
            string html = "<head><meta name=\"description\" content=\"Example description\"></head>";
            string meta = RegexMatches.GetMeta(html);
            Assert.AreEqual("<meta name=\"description\" content=\"Example description\">", meta);
        }

        [Test]
        public void TestGetDatePublished()
        {
            string html = "<meta name=\"published_time\" content=\"2022-01-01T00:00:00Z\">";
            string date = RegexMatches.GetDatePublished(html);
            Assert.AreEqual("2022-01-01T00:00:00Z", date);
        }

        [Test]
        public void TestGetImages()
        {
            string html = "<img src=\"https://www.example.com/image1.jpg\" alt=\"Image 1\">" +
                          "<img src=\"https://www.example.com/image2.jpg\" alt=\"Image 2\">";
            string url = "https://www.example.com";
            List<Image> images = RegexMatches.GetImages(html, url);
            Assert.AreEqual(2, images.Count);
            Assert.AreEqual("https://www.example.com/image1.jpg", images[0].Url);
            Assert.AreEqual("Image 1", images[0].Alt);
            Assert.AreEqual("https://www.example.com/image2.jpg", images[1].Url);
            Assert.AreEqual("Image 2", images[1].Alt);
        }

        [Test]
        public void TestGetPageLang()
        {
            string html = "<html lang=\"en-US\">";
            string lang = RegexMatches.GetPageLang(html);
            Assert.AreEqual("en-US", lang);
        }

        [Test]
        public void TestGetTitle()
        {
            string html = "<title>Example Title</title>";
            string title = RegexMatches.GetTitle(html);
            Assert.AreEqual("Example Title", title);
        }

        [Test]
        public void TestRemovePunctuation()
        {
            string input = "Example, Title!";
            string output = RegexMatches.RemovePunctuation(input);
            Assert.AreEqual("Example  Title ", output);
        }

        [Test]
        public void TestGetRssHref()
        {
            string html = "<link rel=\"alternate\" type=\"application/rss+xml\" href=\"https://www.example.com/rss\">";
            string rssHref = RegexMatches.GetRssHref(html);
            Assert.AreEqual("https://www.example.com/rss", rssHref);
        }

        [Test]
        public void TestGetDescription()
        {
            string html = "<meta name=\"description\" content=\"Example description\">";
            string description = RegexMatches.GetDescription(html);
            Assert.AreEqual("Example description", description);
        }
    }
}