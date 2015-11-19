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
            var o = Json.Parse("{}");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(0));
        }

        [Test]
        public void TestStringField()
        {
            var o = Json.Parse("{ \"foo\" : \"bar\" }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void TestIntegerField()
        {
            var o = Json.Parse("{ \"foo\" : 1234 }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.EqualTo(1234));
        }

        [Test]
        public void TestDecimalField()
        {
            var o = Json.Parse("{ \"foo\" : 1234.5678 }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.EqualTo(1234.5678));
        }

        [Test]
        public void TestTrueField()
        {
            var o = Json.Parse("{ \"foo\" : true }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.True);
        }

        [Test]
        public void TestFalseField()
        {
            var o = Json.Parse("{ \"foo\" : false }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.False);
        }

        [Test]
        public void TestNullField()
        {
            var o = Json.Parse("{ \"foo\" : null }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.Null);
        }

        [Test]
        public void TestMultipleFields()
        {
            var o = Json.Parse("{ \"a\" : \"string\", \"b\" : 99, \"c\": true, \"d\": null }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(4));
            Assert.That(o["a"], Is.EqualTo("string"));
            Assert.That(o["b"], Is.EqualTo(99));
            Assert.That(o["c"], Is.True);
            Assert.That(o["d"], Is.Null);
        }

        [Test]
        public void TestNestedObject()
        {
            var o = Json.Parse("{ \"foo\" : { \"bar\" : 66 } }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.InstanceOf<IDictionary<string, object>>());
            var foo = (IDictionary<string, object>)o["foo"];
            Assert.That(foo.Count, Is.EqualTo(1));
            Assert.That(foo["bar"], Is.EqualTo(66));
        }

        [Test]
        public void TestNestedArray()
        {
            var o = Json.Parse("{ \"foo\" : [ { \"bar\" : 66 } { \"bar\" : 67 } ] }");
            Assert.That(o, Is.Not.Null);
            Assert.That(o.Count, Is.EqualTo(1));
            Assert.That(o["foo"], Is.InstanceOf<IEnumerable<IDictionary<string, object>>>());
            var foo = (IEnumerable<IDictionary<string, object>>)o["foo"];
            var foo1 = foo.First();
            Assert.That(foo1.Count, Is.EqualTo(1));
            Assert.That(foo1["bar"], Is.EqualTo(66));
            var foo2 = foo.ElementAt(1);
            Assert.That(foo2.Count, Is.EqualTo(1));
            Assert.That(foo2["bar"], Is.EqualTo(67));
        }

        [Test]
        public void TestFormatEmpty()
        {
            var str = Json.Format(new Dictionary<string, object>());
            Assert.That(str, Is.EqualTo("{ }"));
        }

        [Test]
        public void TestFormatStringField()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = "bar";
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": \"bar\" }"));
        }

        [Test]
        public void TestFormatStringFieldNewLine()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = "1\r\n2";
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\n2\" }"));
        }

        [Test]
        public void TestFormatStringFieldTab()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = "1\t2";
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\t2\" }"));
        }

        [Test]
        public void TestFormatStringFieldBackslash()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = "1\\2";
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\\\2\" }"));
        }

        [Test]
        public void TestFormatStringFieldQuote()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = "1\"2\"3";
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": \"1\\\"2\\\"3\" }"));
        }

        [Test]
        public void TestFormatIntegerField()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = 93;
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": 93 }"));
        }

        [Test]
        public void TestFormatDecimalField()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = 3.14159;
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": 3.14159 }"));
        }

        [Test]
        public void TestFormatTrueField()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = true;
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": true }"));
        }

        [Test]
        public void TestFormatFalseField()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = false;
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": false }"));
        }

        [Test]
        public void TestFormatNullField()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = null;
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": null }"));
        }

        [Test]
        public void TestFormatMultipleFields()
        {
            var dict = new Dictionary<string, object>();
            dict["foo"] = "happy";
            dict["bar"] = 73;
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": \"happy\", \"bar\": 73 }"));
        }

        [Test]
        public void TestFormatNestedObject()
        {
            var nested = new Dictionary<string, object>();
            nested["bar"] = 1234;
            var dict = new Dictionary<string, object>();
            dict["foo"] = nested;
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": { \"bar\": 1234 } }"));
        }

        [Test]
        public void TestFormatNestedArray()
        {
            var nested1 = new Dictionary<string, object>();
            nested1["bar"] = 1234;
            var nested2 = new Dictionary<string, object>();
            nested2["bar"] = 4321;
            var dict = new Dictionary<string, object>();
            dict["foo"] = new[] { nested1, nested2 };
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": [ { \"bar\": 1234 }, { \"bar\": 4321 } ] }"));
        }

        [Test]
        public void TestFormatComplex()
        {
            var nested1 = new Dictionary<string, object>();
            nested1["bar"] = 1234;
            nested1["bool"] = true;
            var nested2 = new Dictionary<string, object>();
            nested2["bar"] = 4321;
            nested2["bool"] = false;
            var dict = new Dictionary<string, object>();
            dict["foo"] = new[] { nested1, nested2 };
            dict["label"] = "happy";
            var str = Json.Format(dict);
            Assert.That(str, Is.EqualTo("{ \"foo\": [ { \"bar\": 1234, \"bool\": true }, { \"bar\": 4321, \"bool\": false } ], \"label\": \"happy\" }"));
        }

        [Test]
        public void TestFormatComplexWithIndent()
        {
            var nested1 = new Dictionary<string, object>();
            nested1["bar"] = 1234;
            nested1["bool"] = true;
            var nested2 = new Dictionary<string, object>();
            nested2["bar"] = 4321;
            nested2["bool"] = false;
            var dict = new Dictionary<string, object>();
            dict["foo"] = new[] { nested1, nested2 };
            dict["label"] = "happy";
            var str = Json.Format(dict, 2);
            Assert.That(str, Is.EqualTo(
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
                "}"));
        }
    }
}
