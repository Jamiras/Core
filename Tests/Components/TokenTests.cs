using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Jamiras.Components;
using System.Reflection;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class TokenTests
    {
        private string book = "'Twas the night before Christmas\n" +
                              "And all through the house\n" +
                              "Not a creature was stirring,\n" +
                              "Not even a mouse." +
                              "\n" +
                              "The stockings were hung\n" +
                              "By the chimney with care\n" +
                              "In hopes that St. Nicholas\n" +
                              "Soon would be there.\n";

        private FieldInfo _sourceField = typeof(Token).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);

        private void AssertTokenPointer(Token token, string text)
        {
            var source = _sourceField.GetValue(token);
            Assert.That(source, Is.SameAs(text));
        }

        [Test]
        public void TestEmptyToken()
        {
            Token token = new Token();
            Assert.That(token.Length, Is.EqualTo(0));
            Assert.That(token.IsEmpty, Is.True);
            Assert.That(token.IsEmptyOrWhitespace, Is.True);
            Assert.That(token.DebugString, Is.EqualTo(String.Empty));
            Assert.That(token.ToString(), Is.EqualTo(String.Empty));
        }

        [Test]
        public void TestWhitespaceToken()
        {
            Token token = new Token(" \n\t", 0, 3);
            Assert.That(token.Length, Is.EqualTo(3));
            Assert.That(token.IsEmpty, Is.False);
            Assert.That(token.IsEmptyOrWhitespace, Is.True);
            Assert.That(token.DebugString, Is.EqualTo(" \n\t"));
            Assert.That(token.ToString(), Is.EqualTo(" \n\t"));
        }

        [Test]
        public void TestWholeToken()
        {
            string test = "Test";
            Token token = new Token(test, 0, 4);
            Assert.That(token.Length, Is.EqualTo(4));
            Assert.That(token.IsEmpty, Is.False);
            Assert.That(token.IsEmptyOrWhitespace, Is.False);
            Assert.That(token.DebugString, Is.EqualTo(test));
            Assert.That(token.ToString(), Is.EqualTo(test));
            AssertTokenPointer(token, test);
        }

        [Test]
        public void TestStartToken()
        {
            string input = "Test1234";
            Token token = new Token(input, 0, 4);
            Assert.That(token.Length, Is.EqualTo(4));
            Assert.That(token.DebugString, Is.EqualTo("Test"));
            AssertTokenPointer(token, input);
            Assert.That(token.ToString(), Is.EqualTo("Test"));
        }

        [Test]
        public void TestEndToken()
        {
            string input = "Test1234";
            Token token = new Token(input, 4, 4);
            Assert.That(token.Length, Is.EqualTo(4));
            Assert.That(token.DebugString, Is.EqualTo("1234"));
            AssertTokenPointer(token, input);
            Assert.That(token.ToString(), Is.EqualTo("1234"));
        }

        [Test]
        public void TestMidToken()
        {
            string input = "Test1234";
            Token token = new Token(input, 2, 4);
            Assert.That(token.Length, Is.EqualTo(4));
            Assert.That(token.DebugString, Is.EqualTo("st12"));
            AssertTokenPointer(token, input);
            Assert.That(token.ToString(), Is.EqualTo("st12"));
        }

        [Test]
        public void TestNullSource()
        {
            Assert.That(() => new Token(null, 0, 0), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void TestInvalidStart()
        {
            Assert.That(() => new Token("input", -1, 3), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new Token("input", 6, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestInvalidLength()
        {
            Assert.That(() => new Token("input", 1, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new Token("input", 4, 3), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestCompareToString()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.CompareTo("st12"), Is.EqualTo(0));
            Assert.That(token.CompareTo("ST12"), Is.LessThan(0));
            Assert.That(token.CompareTo("st13"), Is.LessThan(0));
            Assert.That(token.CompareTo("st11"), Is.GreaterThan(0));
            Assert.That(token.CompareTo("st1"), Is.GreaterThan(0));
            Assert.That(token.CompareTo("st123"), Is.LessThan(0));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestCompareToStringCaseInsensitive()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.CompareTo("ST12", StringComparison.OrdinalIgnoreCase), Is.EqualTo(0));
            Assert.That(token.CompareTo("ST13", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            Assert.That(token.CompareTo("ST11", StringComparison.OrdinalIgnoreCase), Is.GreaterThan(0));
            Assert.That(token.CompareTo("ST1", StringComparison.OrdinalIgnoreCase), Is.GreaterThan(0));
            Assert.That(token.CompareTo("ST123", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestCompareToToken()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.CompareTo(new Token("test1234", 2, 4)), Is.EqualTo(0));
            Assert.That(token.CompareTo(new Token("TEST1234", 2, 4)), Is.LessThan(0));
            Assert.That(token.CompareTo(new Token("test1334", 2, 4)), Is.LessThan(0));
            Assert.That(token.CompareTo(new Token("test1134", 2, 4)), Is.GreaterThan(0));
            Assert.That(token.CompareTo(new Token("test1234", 2, 3)), Is.GreaterThan(0));
            Assert.That(token.CompareTo(new Token("test1234", 2, 5)), Is.LessThan(0));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestCompareToTokenCaseInsensitive()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.CompareTo(new Token("TEST1234", 2, 4), StringComparison.OrdinalIgnoreCase), Is.EqualTo(0));
            Assert.That(token.CompareTo(new Token("TEST1334", 2, 4), StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            Assert.That(token.CompareTo(new Token("TEST1134", 2, 4), StringComparison.OrdinalIgnoreCase), Is.GreaterThan(0));
            Assert.That(token.CompareTo(new Token("TEST1234", 2, 3), StringComparison.OrdinalIgnoreCase), Is.GreaterThan(0));
            Assert.That(token.CompareTo(new Token("TEST1234", 2, 5), StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestOperatorEqualsString()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token == "st12", Is.True);
            Assert.That(token == "ST12", Is.False);
            Assert.That(token == "st1", Is.False);
            Assert.That(token == "st123", Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestOperatorNotEqualsString()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token != "st12", Is.False);
            Assert.That(token != "ST12", Is.True);
            Assert.That(token != "st1", Is.True);
            Assert.That(token != "st123", Is.True);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestOperatorEqualsToken()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token == new Token("Test1234", 2, 4), Is.True);
            Assert.That(token == new Token("TEST1234", 2, 4), Is.False);
            Assert.That(token == new Token("Test1234", 2, 3), Is.False);
            Assert.That(token == new Token("Test1234", 2, 5), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestOperatorNotEqualsToken()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token != new Token("Test1234", 2, 4), Is.False);
            Assert.That(token != new Token("TEST1234", 2, 4), Is.True);
            Assert.That(token != new Token("Test1234", 2, 3), Is.True);
            Assert.That(token != new Token("TEst1234", 2, 5), Is.True);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestEqualsNull()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.Equals(null), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestEqualsString()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.Equals("st12"), Is.True);
            Assert.That(token.Equals("ST12"), Is.False);
            Assert.That(token.Equals("st1"), Is.False);
            Assert.That(token.Equals("st123"), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestEqualsToken()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.Equals(new Token("Test1234", 2, 4)), Is.True);
            Assert.That(token.Equals(new Token("TEST1234", 2, 4)), Is.False);
            Assert.That(token.Equals(new Token("Test1234", 2, 3)), Is.False);
            Assert.That(token.Equals(new Token("Test1234", 2, 5)), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestGetHashCode()
        {
            var token1 = new Token("Test1234", 2, 4);
            var token2 = new Token("st12", 0, 4);
            var string1 = "st12";
            Assert.That(token1.GetHashCode(), Is.EqualTo(token2.GetHashCode()));
            Assert.That(token1.GetHashCode(), Is.EqualTo(string1.GetHashCode()));
            Assert.That(token2.GetHashCode(), Is.EqualTo(string1.GetHashCode()));

            Assert.That(token1.GetHashCode(), Is.Not.EqualTo("foo".GetHashCode()));
            Assert.That(token1.GetHashCode(), Is.Not.EqualTo("ST12".GetHashCode()));
        }

        [Test]
        public void TestIndexer()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token[0], Is.EqualTo('s'));
            Assert.That(token[1], Is.EqualTo('t'));
            Assert.That(token[2], Is.EqualTo('1'));
            Assert.That(token[3], Is.EqualTo('2'));
            Assert.That(() => token[4], Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token[-1], Throws.InstanceOf<ArgumentOutOfRangeException>());
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestStartsWith()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.StartsWith("s"), Is.True);
            Assert.That(token.StartsWith("st"), Is.True);
            Assert.That(token.StartsWith("st1"), Is.True);
            Assert.That(token.StartsWith("st12"), Is.True);
            Assert.That(token.StartsWith("st123"), Is.False);
            Assert.That(token.StartsWith("es"), Is.False);
            Assert.That(token.StartsWith("Tes"), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestEndsWith()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.EndsWith("2"), Is.True);
            Assert.That(token.EndsWith("12"), Is.True);
            Assert.That(token.EndsWith("t12"), Is.True);
            Assert.That(token.EndsWith("st12"), Is.True);
            Assert.That(token.EndsWith("est12"), Is.False);
            Assert.That(token.EndsWith("23"), Is.False);
            Assert.That(token.EndsWith("234"), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestIndexOfChar()
        {
            var input = "test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.IndexOf('s'), Is.EqualTo(0));
            Assert.That(token.IndexOf('t'), Is.EqualTo(1));
            Assert.That(token.IndexOf('1'), Is.EqualTo(2));
            Assert.That(token.IndexOf('2'), Is.EqualTo(3));
            Assert.That(token.IndexOf('3'), Is.EqualTo(-1));
            Assert.That(token.IndexOf('e'), Is.EqualTo(-1));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestIndexOfCharStart()
        {
            var input = "test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.IndexOf('s', 1), Is.EqualTo(-1));
            Assert.That(token.IndexOf('t', 1), Is.EqualTo(1));
            Assert.That(token.IndexOf('1', 1), Is.EqualTo(2));
            Assert.That(token.IndexOf('2', 1), Is.EqualTo(3));
            Assert.That(token.IndexOf('3', 1), Is.EqualTo(-1));
            Assert.That(token.IndexOf('e', 1), Is.EqualTo(-1));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestIndexOfString()
        {
            var input = "test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.IndexOf("s"), Is.EqualTo(0));
            Assert.That(token.IndexOf("t"), Is.EqualTo(1));
            Assert.That(token.IndexOf("1"), Is.EqualTo(2));
            Assert.That(token.IndexOf("2"), Is.EqualTo(3));
            Assert.That(token.IndexOf("3"), Is.EqualTo(-1));
            Assert.That(token.IndexOf("e"), Is.EqualTo(-1));
            Assert.That(token.IndexOf("12"), Is.EqualTo(2));
            Assert.That(token.IndexOf("23"), Is.EqualTo(-1));
            Assert.That(token.IndexOf("S"), Is.EqualTo(-1));
            Assert.That(token.IndexOf("T"), Is.EqualTo(-1));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestIndexOfStringStart()
        {
            var input = "test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.IndexOf("s", 1), Is.EqualTo(-1));
            Assert.That(token.IndexOf("t", 1), Is.EqualTo(1));
            Assert.That(token.IndexOf("1", 1), Is.EqualTo(2));
            Assert.That(token.IndexOf("2", 1), Is.EqualTo(3));
            Assert.That(token.IndexOf("3", 1), Is.EqualTo(-1));
            Assert.That(token.IndexOf("e", 1), Is.EqualTo(-1));
            Assert.That(token.IndexOf("12", 1), Is.EqualTo(2));
            Assert.That(token.IndexOf("S", 1), Is.EqualTo(-1));
            Assert.That(token.IndexOf("T", 1), Is.EqualTo(-1));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestIndexOfStringCaseInsensitive()
        {
            var input = "test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.IndexOf("S", StringComparison.OrdinalIgnoreCase), Is.EqualTo(0));
            Assert.That(token.IndexOf("T", StringComparison.OrdinalIgnoreCase), Is.EqualTo(1));
            Assert.That(token.IndexOf("1", StringComparison.OrdinalIgnoreCase), Is.EqualTo(2));
            Assert.That(token.IndexOf("2", StringComparison.OrdinalIgnoreCase), Is.EqualTo(3));
            Assert.That(token.IndexOf("3", StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1));
            Assert.That(token.IndexOf("E", StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1));
            Assert.That(token.IndexOf("12", StringComparison.OrdinalIgnoreCase), Is.EqualTo(2));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestIndexOfStringCaseInsensitiveStart()
        {
            var input = "test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.IndexOf("S", 1, StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1));
            Assert.That(token.IndexOf("T", 1, StringComparison.OrdinalIgnoreCase), Is.EqualTo(1));
            Assert.That(token.IndexOf("1", 1, StringComparison.OrdinalIgnoreCase), Is.EqualTo(2));
            Assert.That(token.IndexOf("2", 1, StringComparison.OrdinalIgnoreCase), Is.EqualTo(3));
            Assert.That(token.IndexOf("3", 1, StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1));
            Assert.That(token.IndexOf("E", 1, StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1));
            Assert.That(token.IndexOf("12", 1, StringComparison.OrdinalIgnoreCase), Is.EqualTo(2));
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestContainsChar()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.Contains('T'), Is.False);
            Assert.That(token.Contains('e'), Is.False);
            Assert.That(token.Contains('s'), Is.True);
            Assert.That(token.Contains('t'), Is.True);
            Assert.That(token.Contains('1'), Is.True);
            Assert.That(token.Contains('2'), Is.True);
            Assert.That(token.Contains('3'), Is.False);
            Assert.That(token.Contains('4'), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestContainsString()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.Contains("T"), Is.False);
            Assert.That(token.Contains("e"), Is.False);
            Assert.That(token.Contains("s"), Is.True);
            Assert.That(token.Contains("t"), Is.True);
            Assert.That(token.Contains("1"), Is.True);
            Assert.That(token.Contains("2"), Is.True);
            Assert.That(token.Contains("3"), Is.False);
            Assert.That(token.Contains("4"), Is.False);
            Assert.That(token.Contains("es"), Is.False);
            Assert.That(token.Contains("12"), Is.True);
            Assert.That(token.Contains("23"), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestContainsStringCaseInsensitive()
        {
            var input = "Test1234";
            var token = new Token(input, 2, 4);
            Assert.That(token.Contains("T", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(token.Contains("e", StringComparison.OrdinalIgnoreCase), Is.False);
            Assert.That(token.Contains("s", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(token.Contains("t", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(token.Contains("1", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(token.Contains("2", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(token.Contains("3", StringComparison.OrdinalIgnoreCase), Is.False);
            Assert.That(token.Contains("4", StringComparison.OrdinalIgnoreCase), Is.False);
            Assert.That(token.Contains("es", StringComparison.OrdinalIgnoreCase), Is.False);
            Assert.That(token.Contains("ST", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(token.Contains("12", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(token.Contains("23", StringComparison.OrdinalIgnoreCase), Is.False);
            AssertTokenPointer(token, input);
        }

        [Test]
        public void TestSubToken()
        {
            var input = "Test1234";
            var token = new Token(input, 0, 8);
            var token2 = token.SubToken(1, 2);
            var token3 = token.SubToken(2, 4);
            var token4 = token.SubToken(3, 5);
            Assert.That(token2 == "es", Is.True);
            Assert.That(token3 == "st12", Is.True);
            Assert.That(token4 == "t1234", Is.True);
            AssertTokenPointer(token, input);
            AssertTokenPointer(token2, input);
            AssertTokenPointer(token3, input);
            AssertTokenPointer(token4, input);

            Assert.That(() => token.SubToken(-1, 2), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.SubToken(8, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.SubToken(2, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.SubToken(6, 3), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestSubTokenNoLength()
        {
            var input = "Test1234";
            var token = new Token(input, 0, 8);
            var token2 = token.SubToken(1);
            var token3 = token.SubToken(2);
            var token4 = token.SubToken(3);
            Assert.That(token2 == "est1234", Is.True);
            Assert.That(token3 == "st1234", Is.True);
            Assert.That(token4 == "t1234", Is.True);
            AssertTokenPointer(token, input);
            AssertTokenPointer(token2, input);
            AssertTokenPointer(token3, input);
            AssertTokenPointer(token4, input);

            Assert.That(() => token.SubToken(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.SubToken(8), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestSubstring()
        {
            var input = "Test1234";
            var token = new Token(input, 0, 8);
            var str1 = token.Substring(1, 2);
            var str2 = token.Substring(2, 4);
            var str3 = token.Substring(3, 5);
            Assert.That(str1, Is.EqualTo("es"));
            Assert.That(str2, Is.EqualTo("st12"));
            Assert.That(str3, Is.EqualTo("t1234"));
            AssertTokenPointer(token, input);

            Assert.That(() => token.Substring(-1, 2), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.Substring(8, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.Substring(2, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.Substring(6, 3), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestSubstringNoLength()
        {
            var input = "Test1234";
            var token = new Token(input, 0, 8);
            var str1 = token.Substring(1);
            var str2 = token.Substring(2);
            var str3 = token.Substring(3);
            Assert.That(str1, Is.EqualTo("est1234"));
            Assert.That(str2, Is.EqualTo("st1234"));
            Assert.That(str3, Is.EqualTo("t1234"));
            AssertTokenPointer(token, input);

            Assert.That(() => token.Substring(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => token.Substring(8), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestTrimLeft()
        {
            var token = new Token(book, 5, 5);
            Assert.That(token == " the ", Is.True);
            var token2 = token.TrimLeft();
            Assert.That(token2 == "the ", Is.True);
            var token3 = token.TrimLeft();
            Assert.That(token3 == "the ", Is.True);
            AssertTokenPointer(token, book);
            AssertTokenPointer(token2, book);
            AssertTokenPointer(token3, book);
        }

        [Test]
        public void TestTrimRight()
        {
            var token = new Token(book, 5, 5);
            Assert.That(token == " the ", Is.True);
            var token2 = token.TrimRight();
            Assert.That(token2 == " the", Is.True);
            var token3 = token.TrimRight();
            Assert.That(token3 == " the", Is.True);
            AssertTokenPointer(token, book);
            AssertTokenPointer(token2, book);
            AssertTokenPointer(token3, book);
        }

        [Test]
        public void TestTrim()
        {
            var token = new Token(book, 5, 5);
            Assert.That(token == " the ", Is.True);
            var token2 = token.Trim();
            Assert.That(token2 == "the", Is.True);
            var token3 = token.Trim();
            Assert.That(token3 == "the", Is.True);
            AssertTokenPointer(token, book);
            AssertTokenPointer(token2, book);
            AssertTokenPointer(token3, book);
        }

        [Test]
        public void TestSplit()
        {
            var input = "This is a  test.";
            var token = new Token(input, 0, input.Length);
            var tokens = token.Split(' ');
            Assert.That(tokens.Length, Is.EqualTo(5));
            Assert.That(tokens[0] == "This", Is.True);
            Assert.That(tokens[1] == "is", Is.True);
            Assert.That(tokens[2] == "a", Is.True);
            Assert.That(tokens[3] == "", Is.True);
            Assert.That(tokens[4] == "test.", Is.True);
            AssertTokenPointer(tokens[0], input);
            AssertTokenPointer(tokens[1], input);
            AssertTokenPointer(tokens[2], input);
            AssertTokenPointer(tokens[3], input);
            AssertTokenPointer(tokens[4], input);
        }

        [Test]
        public void TestSplitMultiple()
        {
            var input = "This is a  test.";
            var token = new Token(input, 0, input.Length);
            var tokens = token.Split(' ', 's');
            Assert.That(tokens.Length, Is.EqualTo(8));
            Assert.That(tokens[0] == "Thi", Is.True);
            Assert.That(tokens[1] == "", Is.True);
            Assert.That(tokens[2] == "i", Is.True);
            Assert.That(tokens[3] == "", Is.True);
            Assert.That(tokens[4] == "a", Is.True);
            Assert.That(tokens[5] == "", Is.True);
            Assert.That(tokens[6] == "te", Is.True);
            Assert.That(tokens[7] == "t.", Is.True);
        }

        [Test]
        public void TestSplitMany()
        {
            var input = "This is a  test.";
            var token = new Token(input, 0, input.Length);
            var tokens = token.Split(' ', 'i', 's');
            Assert.That(tokens.Length, Is.EqualTo(10));
            Assert.That(tokens[0] == "Th", Is.True);
            Assert.That(tokens[1] == "", Is.True);
            Assert.That(tokens[2] == "", Is.True);
            Assert.That(tokens[3] == "", Is.True);
            Assert.That(tokens[4] == "", Is.True);
            Assert.That(tokens[5] == "", Is.True);
            Assert.That(tokens[6] == "a", Is.True);
            Assert.That(tokens[7] == "", Is.True);
            Assert.That(tokens[8] == "te", Is.True);
            Assert.That(tokens[9] == "t.", Is.True);
        }

        [Test]
        public void TestSplitRemoveEmptyEntries()
        {
            var input = "This is a  test.";
            var token = new Token(input, 0, input.Length);
            var tokens = token.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(tokens.Length, Is.EqualTo(4));
            Assert.That(tokens[0] == "This", Is.True);
            Assert.That(tokens[1] == "is", Is.True);
            Assert.That(tokens[2] == "a", Is.True);
            Assert.That(tokens[3] == "test.", Is.True);
            AssertTokenPointer(tokens[0], input);
            AssertTokenPointer(tokens[1], input);
            AssertTokenPointer(tokens[2], input);
            AssertTokenPointer(tokens[3], input);
        }

        [Test]
        public void TestSplitMultipleRemoveEmptyEntries()
        {
            var input = "This is a  test.";
            var token = new Token(input, 0, input.Length);
            var tokens = token.Split(new[] { ' ', 's' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(tokens.Length, Is.EqualTo(5));
            Assert.That(tokens[0] == "Thi", Is.True);
            Assert.That(tokens[1] == "i", Is.True);
            Assert.That(tokens[2] == "a", Is.True);
            Assert.That(tokens[3] == "te", Is.True);
            Assert.That(tokens[4] == "t.", Is.True);
        }

        [Test]
        public void TestSplitManyRemoveEmptyEntries()
        {
            var input = "This is a  test.";
            var token = new Token(input, 0, input.Length);
            var tokens = token.Split(new[] { ' ', 'i', 's' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(tokens.Length, Is.EqualTo(4));
            Assert.That(tokens[0] == "Th", Is.True);
            Assert.That(tokens[1] == "a", Is.True);
            Assert.That(tokens[2] == "te", Is.True);
            Assert.That(tokens[3] == "t.", Is.True);
        }

        [Test]
        public void TestSplitRemoveEmptyEntriesEdges()
        {
            var input = "~test~one~";
            var token = new Token(input, 0, input.Length);
            var tokens = token.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(tokens.Length, Is.EqualTo(2));
            Assert.That(tokens[0] == "test", Is.True);
            Assert.That(tokens[1] == "one", Is.True);
            AssertTokenPointer(tokens[0], input);
            AssertTokenPointer(tokens[1], input);
        }
    }
}
