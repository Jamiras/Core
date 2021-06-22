using System.IO;
using System.Text;
using Jamiras.Components;
using NUnit.Framework;

namespace Jamiras.Core.Tests.Components
{
    [TestFixture]
    public class PositionalTokenizerTests
    {
        private PositionalTokenizer CreateTokenizer(string input)
        {
            var tokenizer = Tokenizer.CreateTokenizer(input);
            return new PositionalTokenizer(tokenizer);
        }

        [Test]
        public void TestAdvance()
        {
            var tokenizer = CreateTokenizer("sad\nhappy");
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(1));
            Assert.That(tokenizer.NextChar, Is.EqualTo('s'));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('a'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(2));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('d'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(3));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('\n'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(4));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            tokenizer.Advance(4);
            Assert.That(tokenizer.NextChar, Is.EqualTo('y'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(5));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo((char)0));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(6));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo((char)0));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(6));
        }

        [Test]
        public void TestReadIdentifier()
        {
            // validates StartToken and EndToken overrides
            var tokenizer = CreateTokenizer("Red\nBlue");
            var token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo("Red"));

            Assert.That(tokenizer.NextChar, Is.EqualTo('\n'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(4));

            tokenizer.Advance();
            token = tokenizer.ReadIdentifier();
            Assert.That(token.ToString(), Is.EqualTo("Blue"));

            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(5));
        }

        [Test]
        public void TestMatch()
        {
            // validates MatchSubstring override
            var tokenizer = CreateTokenizer("RedFish\nBlueFish");
            Assert.That(tokenizer.Match("Blue"), Is.False);
            Assert.That(tokenizer.NextChar, Is.EqualTo('R'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            Assert.That(tokenizer.Match("Red"), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('F'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(4));

            Assert.That(tokenizer.Match("Fish"), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('\n'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(8));

            tokenizer.Advance();

            Assert.That(tokenizer.Match("Fish"), Is.False);
            Assert.That(tokenizer.NextChar, Is.EqualTo('B'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            Assert.That(tokenizer.Match("BlueFish"), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(9));

            Assert.That(tokenizer.Match("BlueFish"), Is.False);
            Assert.That(tokenizer.Match("h"), Is.False);
            Assert.That(tokenizer.Match(""), Is.True);
        }

        [Test]
        public void TestPushState()
        {
            var input = "This\nis\na test.";
            var tokenizer = CreateTokenizer(input);
            Assert.That(tokenizer.NextChar, Is.EqualTo('T'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            tokenizer.PushState();
            Assert.That(tokenizer.NextChar, Is.EqualTo('T'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            tokenizer.Match("This\n");
            Assert.That(tokenizer.NextChar, Is.EqualTo('i'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            tokenizer.PushState();
            Assert.That(tokenizer.NextChar, Is.EqualTo('i'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            tokenizer.Match("is\n");
            Assert.That(tokenizer.NextChar, Is.EqualTo('a'));
            Assert.That(tokenizer.Line, Is.EqualTo(3));
            Assert.That(tokenizer.Column, Is.EqualTo(1));
            tokenizer.PushState();

            tokenizer.Advance();
            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('t'));
            Assert.That(tokenizer.Line, Is.EqualTo(3));
            Assert.That(tokenizer.Column, Is.EqualTo(3));

            tokenizer.PopState(); // back to "a test."
            Assert.That(tokenizer.NextChar, Is.EqualTo('a'));
            Assert.That(tokenizer.Line, Is.EqualTo(3));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            Assert.That(tokenizer.Match("a test."), Is.True);
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));
            Assert.That(tokenizer.Line, Is.EqualTo(3));
            Assert.That(tokenizer.Column, Is.EqualTo(8));

            tokenizer.PushState();
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));
            Assert.That(tokenizer.Line, Is.EqualTo(3));
            Assert.That(tokenizer.Column, Is.EqualTo(8));

            tokenizer.PopState(); // back to end of string
            Assert.That(tokenizer.NextChar, Is.EqualTo('\0'));
            Assert.That(tokenizer.Line, Is.EqualTo(3));
            Assert.That(tokenizer.Column, Is.EqualTo(8));

            tokenizer.PopState(); // back to "is\na test."
            Assert.That(tokenizer.NextChar, Is.EqualTo('i'));
            Assert.That(tokenizer.Line, Is.EqualTo(2));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            tokenizer.PopState(); // back to "This is a test."
            Assert.That(tokenizer.NextChar, Is.EqualTo('T'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(1));

            tokenizer.Advance();
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));

            tokenizer.PopState(); // nothing to go back to
            Assert.That(tokenizer.NextChar, Is.EqualTo('h'));
            Assert.That(tokenizer.Line, Is.EqualTo(1));
            Assert.That(tokenizer.Column, Is.EqualTo(2));
        }
    }
}
