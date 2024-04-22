using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using System.Linq.Expressions;
using System.Text.Json;
using Xunit.Abstractions;

namespace Lantean.QBTBlazor.Test
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test()
        {
            Test2(a => a.Name);
        }

        private void Test2(Expression<Func<TestClass, object>> expr)
        {
            var body = expr.Body;
        }

        [Fact]
        public void Create()
        {
            var propInfo = typeof(TestClass).GetProperty("Name")!;

            ParameterExpression expression = Expression.Parameter(typeof(TestClass), "a");
            var propertyExpression = Expression.Property(expression, "Value");

            var convertExpression = Expression.Convert(propertyExpression, typeof(object));

            var l = Expression.Lambda<Func<TestClass, object>>(convertExpression, expression);

            Expression<Func<TestClass, object>> expr2 = a => a.Name;

            var x = l.Compile();
            var res = (long)x(new TestClass { Name = "Name", Value = 12 });
            Assert.Equal(12, res);
            expr2.Compile();
        }

        [Fact]
        public void ScanDir()
        {
            //Dictionary<string, string>
            var json = "{\r\n\t\"/this/is/path\": 1,\r\n\t\"/this/other\": 0,\r\n\t\"/home\": \"/path\"\r\n}";

            var obj = JsonSerializer.Deserialize<Dictionary<string, SaveLocation>>(json, SerializerOptions.Options);
        }
    }

    public class TestClass
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public long Value { get; set; }
    }
}