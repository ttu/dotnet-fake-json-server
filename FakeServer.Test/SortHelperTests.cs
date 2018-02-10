using FakeServer.Common;
using System.Dynamic;
using System.Linq;
using Xunit;

namespace FakeServer.Test
{
    public class SortHelperTests
    {
        [Fact]
        public void SortFields()
        {
            dynamic exp = new ExpandoObject();
            exp.Name = "Jim";
            exp.Age = 20;
            exp.Occupation = "Gardener";

            dynamic exp2 = new ExpandoObject();
            exp2.Name = "Danny";
            exp2.Age = 60;
            exp2.Occupation = "Engineer";

            dynamic exp3 = new ExpandoObject();
            exp3.Name = "John";
            exp3.Age = 40;
            exp3.Occupation = "Tech";

            var result = SortHelper.SortFields(new[] { exp, exp2, exp3 }, new[] { "+Age" });
            Assert.Equal(3, result.Count());
            Assert.Equal(20, ObjectHelper.GetNestedProperty(result.ToList()[0], "Age"));
            Assert.Equal(60, ObjectHelper.GetNestedProperty(result.ToList()[2], "Age"));

            result = SortHelper.SortFields(new[] { exp, exp2, exp3 }, new[] { "-Age" });
            Assert.Equal(3, result.Count());
            Assert.Equal(60, ObjectHelper.GetNestedProperty(result.ToList()[0], "Age"));
            Assert.Equal(20, ObjectHelper.GetNestedProperty(result.ToList()[2], "Age"));

            result = SortHelper.SortFields(new[] { exp, exp2, exp3 }, new[] { "Age" });
            Assert.Equal(3, result.Count());
            Assert.Equal(60, ObjectHelper.GetNestedProperty(result.ToList()[0], "Age"));
            Assert.Equal(20, ObjectHelper.GetNestedProperty(result.ToList()[2], "Age"));
        }
        
        [Fact]
        public void SortFields_EmptySortArray()
        {
            var result = SortHelper.SortFields(new dynamic[] { }, new string[] { });
            Assert.NotNull(result);

            result = SortHelper.SortFields(new dynamic[] { }, new[] { "" });
            Assert.NotNull(result);
        }
    }
}
