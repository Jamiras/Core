using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jamiras.IO.Serialization;
using NUnit.Framework;

namespace Jamiras.Core.Tests.IO.Serialization
{
    [TestFixture]
    public class JsonTests
    {
        [Test]
        public void TestEmptyObject()
        {
            var o = new JsonObject("{}");
            Assert.That(o.IsEmpty, Is.True);
        }

        [Test]
        public void TestStringField()
        {
            var o = new JsonObject("{ \"foo\" : \"bar\" }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.String));
            Assert.That(o.GetField("foo").StringValue, Is.EqualTo("bar"));
        }

        [Test]
        public void TestIntegerField()
        {
            var o = new JsonObject("{ \"foo\" : 1234 }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.Integer));
            Assert.That(o.GetField("foo").IntegerValue, Is.EqualTo(1234));
        }

        [Test]
        public void TestDecimalField()
        {
            var o = new JsonObject("{ \"foo\" : 1234.5678 }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.Double));
            Assert.That(o.GetField("foo").DoubleValue, Is.EqualTo(1234.5678));
        }

        [Test]
        public void TestTrueField()
        {
            var o = new JsonObject("{ \"foo\" : true }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.Boolean));
            Assert.That(o.GetField("foo").BooleanValue, Is.True);
        }

        [Test]
        public void TestFalseField()
        {
            var o = new JsonObject("{ \"foo\" : false }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.Boolean));
            Assert.That(o.GetField("foo").BooleanValue, Is.False);
        }

        [Test]
        public void TestNullField()
        {
            var o = new JsonObject("{ \"foo\" : null }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.Null));
            Assert.That(o.GetField("foo").Value, Is.Null);
            Assert.That(o.GetField("foo").DateTimeValue, Is.Null);
            Assert.That(o.GetField("foo").DoubleValue, Is.Null);
            Assert.That(o.GetField("foo").IntegerArrayValue, Is.Null);
            Assert.That(o.GetField("foo").IntegerValue, Is.Null);
            Assert.That(o.GetField("foo").ObjectArrayValue, Is.Null);
            Assert.That(o.GetField("foo").ObjectValue, Is.Null);
            Assert.That(o.GetField("foo").StringValue, Is.Null);
        }

        [Test]
        public void TestMultipleFields()
        {
            var o = new JsonObject("{ \"a\" : \"string\", \"b\" : 99, \"c\": true, \"d\": null }");
            Assert.That(o.GetField("a").Type, Is.EqualTo(JsonFieldType.String));
            Assert.That(o.GetField("a").StringValue, Is.EqualTo("string"));
            Assert.That(o.GetField("b").Type, Is.EqualTo(JsonFieldType.Integer));
            Assert.That(o.GetField("b").IntegerValue, Is.EqualTo(99));
            Assert.That(o.GetField("c").Type, Is.EqualTo(JsonFieldType.Boolean));
            Assert.That(o.GetField("c").BooleanValue, Is.True);
            Assert.That(o.GetField("d").Type, Is.EqualTo(JsonFieldType.Null));
            Assert.That(o.GetField("d").Value, Is.Null);
        }

        [Test]
        public void TestNestedObject()
        {
            var o = new JsonObject("{ \"foo\" : { \"bar\" : 66 } }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.Object));
            Assert.That(o.GetField("foo").ObjectValue, Is.Not.Null);
            Assert.That(o.GetField("foo").ObjectValue.GetField("bar").Type, Is.EqualTo(JsonFieldType.Integer));
            Assert.That(o.GetField("foo").ObjectValue.GetField("bar").IntegerValue, Is.EqualTo(66));
        }

        [Test]
        public void TestNestedArray()
        {
            var o = new JsonObject("{ \"foo\" : [ { \"bar\" : 66 } { \"bar\" : 67 } ] }");
            Assert.That(o.GetField("foo").Type, Is.EqualTo(JsonFieldType.ObjectArray));
            Assert.That(o.GetField("foo").ObjectArrayValue, Is.Not.Null);
            Assert.That(o.GetField("foo").ObjectArrayValue.First().GetField("bar").IntegerValue, Is.EqualTo(66));
            Assert.That(o.GetField("foo").ObjectArrayValue.ElementAt(1).GetField("bar").IntegerValue, Is.EqualTo(67));
        }

        [Test]
        public void TestFormatEmpty()
        {
            var str = new JsonObject().ToString();
            Assert.That(str, Is.EqualTo("{ }"));
        }

        [Test]
        public void TestFormatStringField()
        {
            var o = new JsonObject();
            o.AddField("foo", "bar");
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": \"bar\" }"));
        }

        [Test]
        public void TestFormatStringFieldNewLine()
        {
            var o = new JsonObject();
            o.AddField("foo", "1\r\n2");
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\n2\" }"));
        }

        [Test]
        public void TestFormatStringFieldTab()
        {
            var o = new JsonObject();
            o.AddField("foo", "1\t2");
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\t2\" }"));
        }

        [Test]
        public void TestFormatStringFieldBackslash()
        {
            var o = new JsonObject();
            o.AddField("foo", "1\\2");
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\\\2\" }"));
        }

        [Test]
        public void TestFormatStringFieldQuote()
        {
            var o = new JsonObject();
            o.AddField("foo", "1\"2\"3");
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\\"2\\\"3\" }"));
        }

        [Test]
        public void TestFormatIntegerField()
        {
            var o = new JsonObject();
            o.AddField("foo", 93);
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": 93 }"));
        }

        [Test]
        public void TestFormatDecimalField()
        {
            var o = new JsonObject();
            o.AddField("foo", 3.14159);
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": 3.14159 }"));
        }

        [Test]
        public void TestFormatTrueField()
        {
            var o = new JsonObject();
            o.AddField("foo", true);
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": true }"));
        }

        [Test]
        public void TestFormatFalseField()
        {
            var o = new JsonObject();
            o.AddField("foo", false);
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": false }"));
        }

        [Test]
        public void TestFormatNullField()
        {
            var o = new JsonObject();
            o.AddField("foo", (string)null);
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": null }"));
        }

        [Test]
        public void TestFormatMultipleFields()
        {
            var o = new JsonObject();
            o.AddField("foo", "happy");
            o.AddField("bar", 73);
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": \"happy\", \"bar\": 73 }"));
        }

        [Test]
        public void TestFormatNestedObject()
        {
            var o = new JsonObject();
            var f = new JsonObject();
            f.AddField("bar", 1234);
            o.AddField("foo", f);
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": { \"bar\": 1234 } }"));
        }

        [Test]
        public void TestFormatNestedArray()
        {
            var o = new JsonObject();
            var f1 = new JsonObject();
            var f2 = new JsonObject();
            f1.AddField("bar", 1234);
            f2.AddField("bar", 4321);
            o.AddField("foo", new[] { f1, f2 });
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": [ { \"bar\": 1234 }, { \"bar\": 4321 } ] }"));
        }

        [Test]
        public void TestFormatComplex()
        {
            var f1 = new JsonObject();
            f1.AddField("bar", 1234);
            f1.AddField("bool", true);
            var f2 = new JsonObject();
            f2.AddField("bar", 4321);
            f2.AddField("bool", false);
            var o = new JsonObject();
            o.AddField("foo", new[] { f1, f2 });
            o.AddField("label", "happy");
            var str = o.ToString();
            Assert.That(str, Is.EqualTo("{ \"foo\": [ { \"bar\": 1234, \"bool\": true }, { \"bar\": 4321, \"bool\": false } ], \"label\": \"happy\" }"));
        }

        [Test]
        public void TestFormatComplexWithIndent()
        {
            var f1 = new JsonObject();
            f1.AddField("bar", 1234);
            f1.AddField("bool", true);
            var f2 = new JsonObject();
            f2.AddField("bar", 4321);
            f2.AddField("bool", false);
            var o = new JsonObject();
            o.AddField("foo", new[] { f1, f2 });
            o.AddField("label", "happy");
            var str = o.ToString(2).Substring(24);
            Assert.That(str, Is.EqualTo((
                "{\r\n" +
                "  \"foo\": [\r\n" +
                "    {\r\n" +
                "      \"bar\": 1234,\r\n" +
                "      \"bool\": true\r\n" +
                "    },\r\n" +
                "    {\r\n" +
                "      \"bar\": 4321,\r\n" +
                "      \"bool\": false\r\n" +
                "    }\r\n" +
                "  ],\r\n" +
                "  \"label\": \"happy\"\r\n" +
                "}").Substring(24)));
        }
    }
}
