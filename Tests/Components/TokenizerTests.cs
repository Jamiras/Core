﻿using System.IO;
using System.Text;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class TokenizerTests
    {
        private Tokenizer CreateTokenizer(string input)
        {
            return new Tokenizer.StreamTokenizer(new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        [Test]
        public void TestEmptyString()
        {
            var tokenizer = CreateTokenizer("");
            Assert.That(tokenizer.NextChar, Is.EqualTo((char)0));
        }

        [Test]
        public void TestAdvance()
        {
            var tokenizer = CreateTokenizer("happy");
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
        public void TestReadIdentifierPascalCase()
        {
            var tokenizer = CreateTokenizer("RedSquare2();");
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
        public void TestReadIdentifierNumber()
        {
            var tokenizer = CreateTokenizer("6");
            var token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo(""));
            Assert.That(tokenizer.NextChar, Is.EqualTo('6'));
        }

        [Test]
        public void TestReadNumberInteger()
        {
            var tokenizer = CreateTokenizer("16;");
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
        public void TestReadQuotedStringSimple()
        {
            var tokenizer = CreateTokenizer("\"happy\";");
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
    }
}
