using System.Linq;
using Jamiras.IO.Serialization;
using NUnit.Framework;

namespace Jamiras.Core.Tests.IO.Serialization
{
    [TestFixture]
    public class GraphQueryTests
    {
        [Test]
        public void TestSimple()
        {
            var query = GraphQuery.Parse("test");
            Assert.That(query, Is.Not.Null);
            Assert.That(query.ObjectType, Is.EqualTo("test"));
            Assert.That(query.Filters, Is.Not.Null);
            Assert.That(query.Filters.Count, Is.EqualTo(0));
            Assert.That(query.Fields, Is.Not.Null);
            Assert.That(query.Fields.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestOneField()
        {
            var query = GraphQuery.Parse("test { foo }");
            Assert.That(query, Is.Not.Null);
            Assert.That(query.ObjectType, Is.EqualTo("test"));
            Assert.That(query.Filters, Is.Not.Null);
            Assert.That(query.Filters.Count, Is.EqualTo(0));
            Assert.That(query.Fields, Is.Not.Null);
            Assert.That(query.Fields.Count(), Is.EqualTo(1));
            var field = query.Fields.First();
            Assert.That(field.FieldName, Is.EqualTo("foo"));
            Assert.That(field.NestedFields, Is.Not.Null);
            Assert.That(field.NestedFields.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestTwoFields()
        {
            var query = GraphQuery.Parse("test { foo, bar }");
            Assert.That(query, Is.Not.Null);
            Assert.That(query.ObjectType, Is.EqualTo("test"));
            Assert.That(query.Filters, Is.Not.Null);
            Assert.That(query.Filters.Count, Is.EqualTo(0));
            Assert.That(query.Fields, Is.Not.Null);
            Assert.That(query.Fields.Count(), Is.EqualTo(2));
            var field = query.Fields.First();
            Assert.That(field.FieldName, Is.EqualTo("foo"));
            Assert.That(field.NestedFields, Is.Not.Null);
            Assert.That(field.NestedFields.Count(), Is.EqualTo(0));
            field = query.Fields.ElementAt(1);
            Assert.That(field.FieldName, Is.EqualTo("bar"));
            Assert.That(field.NestedFields, Is.Not.Null);
            Assert.That(field.NestedFields.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestOneFilter()
        {
            var query = GraphQuery.Parse("test (id:1) { foo }");
            Assert.That(query, Is.Not.Null);
            Assert.That(query.ObjectType, Is.EqualTo("test"));
            Assert.That(query.Filters, Is.Not.Null);
            Assert.That(query.Filters.Count, Is.EqualTo(1));
            Assert.That(query.Filters["id"], Is.EqualTo("1"));
            Assert.That(query.Fields, Is.Not.Null);
            Assert.That(query.Fields.Count(), Is.EqualTo(1));
            var field = query.Fields.First();
            Assert.That(field.FieldName, Is.EqualTo("foo"));
            Assert.That(field.NestedFields, Is.Not.Null);
            Assert.That(field.NestedFields.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestTwoFilters()
        {
            var query = GraphQuery.Parse("test (id:1, type:3) { foo }");
            Assert.That(query, Is.Not.Null);
            Assert.That(query.ObjectType, Is.EqualTo("test"));
            Assert.That(query.Filters, Is.Not.Null);
            Assert.That(query.Filters.Count, Is.EqualTo(2));
            Assert.That(query.Filters["id"], Is.EqualTo("1"));
            Assert.That(query.Filters["type"], Is.EqualTo("3"));
            Assert.That(query.Fields, Is.Not.Null);
            Assert.That(query.Fields.Count(), Is.EqualTo(1));
            var field = query.Fields.First();
            Assert.That(field.FieldName, Is.EqualTo("foo"));
            Assert.That(field.NestedFields, Is.Not.Null);
            Assert.That(field.NestedFields.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestNestedField()
        {
            var query = GraphQuery.Parse("test { foo { bar } }");
            Assert.That(query, Is.Not.Null);
            Assert.That(query.ObjectType, Is.EqualTo("test"));
            Assert.That(query.Filters, Is.Not.Null);
            Assert.That(query.Filters.Count, Is.EqualTo(0));
            Assert.That(query.Fields, Is.Not.Null);
            Assert.That(query.Fields.Count(), Is.EqualTo(1));
            var field = query.Fields.First();
            Assert.That(field.FieldName, Is.EqualTo("foo"));
            Assert.That(field.NestedFields, Is.Not.Null);
            Assert.That(field.NestedFields.Count(), Is.EqualTo(1));
            field = field.NestedFields.First();
            Assert.That(field.FieldName, Is.EqualTo("bar"));
            Assert.That(field.NestedFields, Is.Not.Null);
            Assert.That(field.NestedFields.Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestToString()
        {
            var query = new GraphQuery("user");
            query.Filters["id"] = "6";
            query.Fields = new[]
            {
                new GraphQueryField("first_name"),
                new GraphQueryField("last_name"),
                new GraphQueryField("address")
                {
                    NestedFields = new []
                    {
                        new GraphQueryField("street"),
                        new GraphQueryField("city"),
                        new GraphQueryField("state"),
                        new GraphQueryField("zip"),                        
                    }
                },
            };

            var formatted = query.ToString();
            Assert.That(formatted, Is.EqualTo("user (id: 6) { first_name, last_name, address { street, city, state, zip } }"));
        }
    }
}
