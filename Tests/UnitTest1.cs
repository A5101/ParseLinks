using Parse.Service;
using System.Text.RegularExpressions;
namespace Tests
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
        public void GetTextContent_ReturnsNonEmptyString()
        {
            string textContent = RegexMatches.GetTextContent(sampleHtml);
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
    }
}