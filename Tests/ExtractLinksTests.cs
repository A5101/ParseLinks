using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parse.Service;

namespace ParseTests
{
    [TestFixture]
    public class ExtractLinksTests
    {
        Parser parser = new Parser();

        [Test]
        public void ExtractLinks_ShouldExtractAbsoluteUrls()
        {
            // Arrange
            string html = "<a href=\"http://example.com/page1\">Link 1</a><a href=\"https://example.com/page2\">Link 2</a>";
            string host = "example.com";

            // Act
            var result = parser.ExtractLinks(html, host);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("http://example.com/page1", result[0]);
            Assert.AreEqual("https://example.com/page2", result[1]);
        }

        [Test]
        public void ExtractLinks_ShouldConvertRelativeUrls()
        {
            // Arrange
            string html = "<a href=\"/page1\">Link 1</a><a href=\"/page2\">Link 2</a>";
            string host = "example.com";

            // Act
            var result = parser.ExtractLinks(html, host);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("https://example.com/page1", result[0]);
            Assert.AreEqual("https://example.com/page2", result[1]);
        }

        [Test]
        public void ExtractLinks_ShouldHandleProtocolRelativeUrls()
        {
            // Arrange
            string html = "<a href=\"//example.com/page1\">Link 1</a><a href=\"//example.com/page2\">Link 2</a>";
            string host = "example.com";

            // Act
            var result = parser.ExtractLinks(html, host);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("https://example.com/page1", result[0]);
            Assert.AreEqual("https://example.com/page2", result[1]);
        }

        [Test]
        public void ExtractLinks_ShouldIgnoreInvalidUrls()
        {
            // Arrange
            string html = "<a href=\"http://example.com/page1\">Link 1</a><a href=\"#tg\">Invalid Link</a>";
            string host = "example.com";

            // Act
            var result = parser.ExtractLinks(html, host);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("http://example.com/page1", result[0]);
        }

        [Test]
        public void ExtractLinks_ShouldHandleEmptyInput()
        {
            // Arrange
            string html = "";
            string host = "example.com";

            // Act
            var result = parser.ExtractLinks(html, host);

            // Assert
            Assert.AreEqual(0, result.Count);
        }
    }
}
