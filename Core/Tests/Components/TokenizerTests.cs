using System.IO;
using System.Text;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class TokenizerTests
    {
        private Tokenizer CreateTokenizer(string input, bool useStream = false)
        {
            if (useStream)
                return Tokenizer.CreateTokenizer(new MemoryStream(Encoding.UTF8.GetBytes(input)));

            return Tokenizer.CreateTokenizer(input);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestEmptyString(bool useStream)
        {
            var tokenizer = CreateTokenizer("", useStream);
            Assert.That(tokenizer.NextChar, Is.EqualTo((char)0));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestAdvance(bool useStream)
        {
            var tokenizer = CreateTokenizer("happy", useStream);
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('a'));
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('p'));
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('p'));
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('y'));
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo((char)0));
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo((char)0));
        }

        [Test]
        public void TestSkipWhitespaceEmptyString()
        {
            var tokenizer = CreateTokenizer("");
            tokenizer.SkipWhitespace();
            Assert.That(tokenizer.NextChar, Is.EqualTo((char)0));            
        }

        [Test]
        public void TestSkipWhitespaceNoWhitespace()
        {
            var tokenizer = CreateTokenizer("happy");
            tokenizer.SkipWhitespace();
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
        }

        [Test]
        public void TestSkipWhitespaceSpaces()
        {
            var tokenizer = CreateTokenizer("   happy");
            tokenizer.SkipWhitespace();
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
        }

        [Test]
        public void TestSkipWhitespaceMixed()
        {
            var tokenizer = CreateTokenizer(" \t   \r\n   happy");
            tokenizer.SkipWhitespace();
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestMatch(bool useStream)
        {
            var tokenizer = CreateTokenizer("RedFishBlueFish", useStream);
            Assert.That(tokenizer.Match("Blue"), Is.False);
            Assert.That(tokenizer.NextChar, Is.EqualTo('R'));

            Assert.That(tokenizer.Match("Red"), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('F'));

            Assert.That(tokenizer.Match("Fish"), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('B'));

            Assert.That(tokenizer.Match("Fish"), Is.False);
            Assert.That(tokenizer.NextChar, Is.EqualTo('B'));

            Assert.That(tokenizer.Match("BlueFish"), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));

            Assert.That(tokenizer.Match("BlueFish"), Is.False);
            Assert.That(tokenizer.Match("h"), Is.False);
            Assert.That(tokenizer.Match(""), Is.True);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestReadIdentifierPascalCase(bool useStream)
        {
            var tokenizer = CreateTokenizer("RedSquare2();", useStream);
            var token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo("RedSquare2"));
            Assert.That(tokenizer.NextChar, Is.EqualTo('('));
        }

        [Test]
        public void TestReadIdentifierCamelCase()
        {
            var tokenizer = CreateTokenizer("redSquare2();");
            var token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo("redSquare2"));
            Assert.That(tokenizer.NextChar, Is.EqualTo('('));
        }

        [Test]
        public void TestReadIdentifierAllCaps()
        {
            var tokenizer = CreateTokenizer("RED_SQUARE2();");
            var token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo("RED_SQUARE2"));
            Assert.That(tokenizer.NextChar, Is.EqualTo('('));
        }

        [Test]
        public void TestReadIdentifierLeadingUnderscore()
        {
            var tokenizer = CreateTokenizer("_field = 6;");
            var token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo("_field"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(' '));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestReadIdentifierNumber(bool useStream)
        {
            var tokenizer = CreateTokenizer("6", useStream);
            var token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo(""));
            Assert.That(tokenizer.NextChar, Is.EqualTo('6'));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestReadNumberInteger(bool useStream)
        {
            var tokenizer = CreateTokenizer("16;", useStream);
            var token = tokenizer.ReadNumber();
            Assert.That(token.ToString(), Is.EqualTo("16"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadNumberWord()
        {
            var tokenizer = CreateTokenizer("happy");
            var token = tokenizer.ReadNumber();
            Assert.That(token.ToString(), Is.EqualTo(""));
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
        }

        [Test]
        public void TestReadNumberFloat()
        {
            var tokenizer = CreateTokenizer("16.773;");
            var token = tokenizer.ReadNumber();
            Assert.That(token.ToString(), Is.EqualTo("16.773"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadNumberPhone()
        {
            var tokenizer = CreateTokenizer("406.555.1234");
            var token = tokenizer.ReadNumber();
            Assert.That(token.ToString(), Is.EqualTo("406.555"));
            Assert.That(tokenizer.NextChar, Is.EqualTo('.'));
        }

        [Test]
        public void TestReadNumberDate()
        {
            var tokenizer = CreateTokenizer("12/25/2000");
            var token = tokenizer.ReadNumber();
            Assert.That(token.ToString(), Is.EqualTo("12"));
            Assert.That(tokenizer.NextChar, Is.EqualTo('/'));
        }

        [Test]
        public void TestReadNumberLeadingPeriod()
        {
            var tokenizer = CreateTokenizer(".6");
            var token = tokenizer.ReadNumber();
            Assert.That(token.ToString(), Is.EqualTo(""));
            Assert.That(tokenizer.NextChar, Is.EqualTo('.'));
        }

        [Test]
        public void TestReadQuotedStringNotQuoted()
        {
            var tokenizer = CreateTokenizer("happy");
            Assert.That(() => tokenizer.ReadQuotedString(), Throws.InvalidOperationException);
        }

        [Test]
        public void TestReadQuotedStringUnclosed()
        {
            var tokenizer = CreateTokenizer("\"happy");
            Assert.That(() => tokenizer.ReadQuotedString(), Throws.InvalidOperationException);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestReadQuotedStringSimple(bool useStream)
        {
            var tokenizer = CreateTokenizer("\"happy\";", useStream);
            var token = tokenizer.ReadQuotedString();
            Assert.That(token.ToString(), Is.EqualTo("happy"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadQuotedStringSentence()
        {
            var tokenizer = CreateTokenizer("\"I am happy.\";");
            var token = tokenizer.ReadQuotedString();
            Assert.That(token.ToString(), Is.EqualTo("I am happy."));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadQuotedStringEscapedQuotes()
        {
            var tokenizer = CreateTokenizer("\"I am \\\"happy\\\".\";");
            var token = tokenizer.ReadQuotedString();
            Assert.That(token.ToString(), Is.EqualTo("I am \"happy\"."));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadQuotedStringEscapedTab()
        {
            var tokenizer = CreateTokenizer("\"I am \\thappy.\";");
            var token = tokenizer.ReadQuotedString();
            Assert.That(token.ToString(), Is.EqualTo("I am \thappy."));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadQuotedStringEscapedNewLine()
        {
            var tokenizer = CreateTokenizer("\"I am \\nhappy.\";");
            var token = tokenizer.ReadQuotedString();
            Assert.That(token.ToString(), Is.EqualTo("I am \r\nhappy."));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadQuotedStringEmpty()
        {
            var tokenizer = CreateTokenizer("\"\";");
            var token = tokenizer.ReadQuotedString();
            Assert.That(token.ToString(), Is.EqualTo(""));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadValueNumber()
        {
            var tokenizer = CreateTokenizer("6.8;");
            var token = tokenizer.ReadValue();
            Assert.That(token.ToString(), Is.EqualTo("6.8"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadValueQuotedString()
        {
            var tokenizer = CreateTokenizer("\"happy\";");
            var token = tokenizer.ReadValue();
            Assert.That(token.ToString(), Is.EqualTo("happy"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadValueVariable()
        {
            var tokenizer = CreateTokenizer("happy;");
            var token = tokenizer.ReadValue();
            Assert.That(token.ToString(), Is.EqualTo("happy"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestReadValueField()
        {
            var tokenizer = CreateTokenizer("_happy;");
            var token = tokenizer.ReadValue();
            Assert.That(token.ToString(), Is.EqualTo("_happy"));
            Assert.That(tokenizer.NextChar, Is.EqualTo(';'));
        }

        [Test]
        public void TestSplit()
        {
            var input = "This is a test.";
            var tokens = Tokenizer.Split(input, ' ');
            Assert.That(tokens.Length, Is.EqualTo(4));
            Assert.That(tokens[0].ToString(), Is.EqualTo("This"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("is"));
            Assert.That(tokens[2].ToString(), Is.EqualTo("a"));
            Assert.That(tokens[3].ToString(), Is.EqualTo("test."));
        }

        [Test]
        public void TestGetLongestWordsIgnoresPunctuation()
        {
            var input = "This [(is)] a test.";
            var tokens = Tokenizer.GetLongestWords(input, 2);
            Assert.That(tokens.Length, Is.EqualTo(2));
            Assert.That(tokens[0].ToString(), Is.EqualTo("This"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("test"));
        }

        [Test]
        public void TestGetLongestWordsIgnoresCommonWords()
        {
            var input = "The Barber of Seville";
            var tokens = Tokenizer.GetLongestWords(input, 4);
            Assert.That(tokens.Length, Is.EqualTo(2));
            // NOTE: order is maintained, even though Seville is longer than Barber
            Assert.That(tokens[0].ToString(), Is.EqualTo("Barber"));
            Assert.That(tokens[1].ToString(), Is.EqualTo("Seville"));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestPushState(bool useStream)
        {
            var input = "This is a test.";
            var tokenizer = CreateTokenizer(input, useStream);
            Assert.That(tokenizer.NextChar, Is.EqualTo('T'));

            tokenizer.PushState();
            Assert.That(tokenizer.NextChar, Is.EqualTo('T'));

            tokenizer.Match("This ");
            Assert.That(tokenizer.NextChar, Is.EqualTo('i'));

            tokenizer.PushState();
            Assert.That(tokenizer.NextChar, Is.EqualTo('i'));

            tokenizer.Match("is ");
            Assert.That(tokenizer.NextChar, Is.EqualTo('a'));

            tokenizer.PushState();
            Assert.That(tokenizer.NextChar, Is.EqualTo('a'));

            tokenizer.Advance();
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('t'));

            tokenizer.PopState(); // back to "a test."
            Assert.That(tokenizer.NextChar, Is.EqualTo('a'));

            Assert.That(tokenizer.Match("a test."), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));

            tokenizer.PushState();
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));

            tokenizer.PopState(); // back to end of string
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));

            tokenizer.PopState(); // back to "is a test."
            Assert.That(tokenizer.NextChar, Is.EqualTo('i'));

            tokenizer.PopState(); // back to "This is a test."
            Assert.That(tokenizer.NextChar, Is.EqualTo('T'));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));

            tokenizer.PopState(); // nothing to go back to
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
        }
    }
}
