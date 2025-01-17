using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Npgsql.EntityFrameworkCore.PostgreSQL.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query
{
    public class JsonPocoQueryTest  : IClassFixture<JsonPocoQueryTest.JsonPocoQueryFixture>
    {
        JsonPocoQueryFixture Fixture { get; }

        public JsonPocoQueryTest(JsonPocoQueryFixture fixture, ITestOutputHelper testOutputHelper)
        {
            Fixture = fixture;
            Fixture.TestSqlLoggerFactory.Clear();
            //Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
        }

        [Fact]
        public void Roundtrip()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Id == 1);
                var customer = x.Customer;
                Assert.Equal("Joe", customer.Name);
                Assert.Equal(25, customer.Age);
                var orders = customer.Orders;
                Assert.Equal(99.5m, orders[0].Price);
                Assert.Equal("Some address 1", orders[0].ShippingAddress);
                Assert.Equal(new DateTime(2019, 10, 1), orders[0].ShippingDate);
                Assert.Equal(23, orders[1].Price);
                Assert.Equal("Some address 2", orders[1].ShippingAddress);
                Assert.Equal(new DateTime(2019, 10, 10), orders[1].ShippingDate);
            }
        }

        [Fact]
        public void Roundtrip_json()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonEntities.Single(e => e.Id == 1);
                var customer = x.Customer;
                Assert.Equal("Joe", customer.Name);
                Assert.Equal(25, customer.Age);
                var orders = customer.Orders;
                Assert.Equal(99.5m, orders[0].Price);
                Assert.Equal("Some address 1", orders[0].ShippingAddress);
                Assert.Equal(new DateTime(2019, 10, 1), orders[0].ShippingDate);
                Assert.Equal(23, orders[1].Price);
                Assert.Equal("Some address 2", orders[1].ShippingAddress);
                Assert.Equal(new DateTime(2019, 10, 10), orders[1].ShippingDate);
            }
        }

        [Fact]
        public void Literal()
        {
            using (var ctx = Fixture.CreateContext())
            {
                Assert.Empty(ctx.JsonbEntities.Where(e => e.Customer == new Customer { Name = "Test customer", Age = 80 }));

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE (j.""Customer"" = '{""Name"":""Test customer"",""Age"":80,""IsVip"":false,""Statistics"":null,""Orders"":null}') AND (j.""Customer"" IS NOT NULL)");
            }
        }

        [Fact]
        public void Parameter()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var expected = ctx.JsonbEntities.Find(1).Customer;
                var actual = ctx.JsonbEntities.Single(e => e.Customer == expected).Customer;
                Assert.Equal(actual.Name, expected.Name);

                AssertSql(
                    @"@__p_0='1'

SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE (j.""Id"" = @__p_0) AND (@__p_0 IS NOT NULL)
LIMIT 1",
                    //
                    @"@__expected_0='Npgsql.EntityFrameworkCore.PostgreSQL.Query.JsonPocoQueryTest+Customer' (DbType = Object)

SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE ((j.""Customer"" = @__expected_0) AND ((j.""Customer"" IS NOT NULL) AND (@__expected_0 IS NOT NULL))) OR ((j.""Customer"" IS NULL) AND (@__expected_0 IS NULL))
LIMIT 2");
            }
        }

        [Fact]
        public void Text_output()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Name == "Joe");
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE j.""Customer""->>'Name' = 'Joe'
LIMIT 2");
            }
        }

        [Fact]
        public void Text_output_json()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonEntities.Single(e => e.Customer.Name == "Joe");
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonEntities"" AS j
WHERE j.""Customer""->>'Name' = 'Joe'
LIMIT 2");
            }
        }

        [Fact]
        public void Integer_output()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Age < 30);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE CAST(j.""Customer""->>'Age' AS integer) < 30
LIMIT 2");
            }
        }

        [Fact]
        public void Bool_output()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.IsVip);
                Assert.Equal("Moe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE CAST(j.""Customer""->>'IsVip' AS boolean)
LIMIT 2");
            }
        }

        [Fact]
        public void Nested()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Statistics.Visits == 4);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE CAST(j.""Customer""#>>'{Statistics,Visits}' AS bigint) = 4
LIMIT 2");
            }
        }

        [Fact]
        public void Nested_twice()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Statistics.Nested.SomeProperty == 10);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE CAST(j.""Customer""#>>'{Statistics,Nested,SomeProperty}' AS integer) = 10
LIMIT 2");
            }
        }

        [Fact]
        public void Array_of_objects()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Orders[0].Price == 99.5m);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE CAST(j.""Customer""#>>'{Orders,0,Price}' AS numeric) = 99.5
LIMIT 2");
            }
        }

        [Fact]
        public void Array_toplevel()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.ToplevelArray[1] == "two");
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE j.""ToplevelArray""->>1 = 'two'
LIMIT 2");
            }
        }

        [Fact]
        public void Array_nested()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Statistics.Nested.IntArray[1] == 4);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE CAST(j.""Customer""#>>'{Statistics,Nested,IntArray,1}' AS integer) = 4
LIMIT 2");
            }
        }

        [Fact]
        public void Array_parameter_index()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var i = 1;
                var x = ctx.JsonbEntities.Single(e => e.Customer.Statistics.Nested.IntArray[i] == 4);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"@__i_0='1'

SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE (CAST(j.""Customer""#>>ARRAY['Statistics','Nested','IntArray',@__i_0]::TEXT[] AS integer) = 4) AND (CAST(j.""Customer""#>>ARRAY['Statistics','Nested','IntArray',@__i_0]::TEXT[] AS integer) IS NOT NULL)
LIMIT 2");
            }
        }

        [Fact]
        public void Array_Length()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Orders.Length == 2);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE jsonb_array_length(j.""Customer""->'Orders') = 2
LIMIT 2");
            }
        }

        [Fact]
        public void Array_Length_json()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonEntities.Single(e => e.Customer.Orders.Length == 2);
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonEntities"" AS j
WHERE json_array_length(j.""Customer""->'Orders') = 2
LIMIT 2");
            }
        }

        [Fact(Skip = "https://github.com/aspnet/EntityFrameworkCore/issues/17374")]
        public void Array_Any_toplevel()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.ToplevelArray.Any());
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE jsonb_array_length(j.""ToplevelArray"") > 0
LIMIT 2");
            }
        }

        [Fact]
        public void Like()
        {
            using (var ctx = Fixture.CreateContext())
            {
                var x = ctx.JsonbEntities.Single(e => e.Customer.Name.StartsWith("J"));
                Assert.Equal("Joe", x.Customer.Name);

                AssertSql(
                    @"SELECT j.""Id"", j.""Customer"", j.""ToplevelArray""
FROM ""JsonbEntities"" AS j
WHERE j.""Customer""->>'Name' LIKE 'J%'
LIMIT 2");
            }
        }

        #region Support

        void AssertSql(params string[] expected)
            => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

        public class JsonQueryContext : PoolableDbContext
        {
            public DbSet<JsonbEntity> JsonbEntities { get; set; }
            public DbSet<JsonEntity> JsonEntities { get; set; }

            public JsonQueryContext(DbContextOptions options) : base(options) {}

            public static void Seed(JsonQueryContext context)
            {
                context.JsonbEntities.AddRange(
                    new JsonbEntity { Id = 1, Customer = CreateCustomer1(), ToplevelArray = new[] { "one", "two", "three" } },
                    new JsonbEntity { Id = 2, Customer = CreateCustomer2() });
                context.JsonEntities.AddRange(
                    new JsonEntity { Id = 1, Customer = CreateCustomer1(), ToplevelArray = new[] { "one", "two", "three" } },
                    new JsonEntity { Id = 2, Customer = CreateCustomer2() });
                context.SaveChanges();

                static Customer CreateCustomer1() => new Customer
                {
                    Name = "Joe",
                    Age = 25,
                    IsVip = false,
                    Statistics = new Statistics
                    {
                        Visits = 4,
                        Purchases = 3,
                        Nested = new NestedStatistics
                        {
                            SomeProperty = 10,
                            IntArray = new[] { 3, 4 }
                        }
                    },
                    Orders = new[]
                    {
                        new Order
                        {
                            Price = 99.5m,
                            ShippingAddress = "Some address 1",
                            ShippingDate = new DateTime(2019, 10, 1)
                        },
                        new Order
                        {
                            Price = 23,
                            ShippingAddress = "Some address 2",
                            ShippingDate = new DateTime(2019, 10, 10)
                        }
                    }
                };

                static Customer CreateCustomer2() => new Customer
                {
                    Name = "Moe",
                    Age = 35,
                    IsVip = true,
                    Statistics = new Statistics
                    {
                        Visits = 20,
                        Purchases = 25,
                        Nested = new NestedStatistics
                        {
                            SomeProperty = 20,
                            IntArray = new[] { 5, 6 }
                        }
                    },
                    Orders = new[]
                    {
                        new Order
                        {
                            Price = 5,
                            ShippingAddress = "Moe's address",
                            ShippingDate = new DateTime(2019, 11, 3)
                        }
                    }
                };
            }
        }

        public class JsonbEntity
        {
            public int Id { get; set; }

            [Column(TypeName = "jsonb")]
            public Customer Customer { get; set; }

            [Column(TypeName = "jsonb")]
            public string[] ToplevelArray { get; set; }
        }

        public class JsonEntity
        {
            public int Id { get; set; }

            [Column(TypeName = "json")]
            public Customer Customer { get; set; }

            [Column(TypeName = "json")]
            public string[] ToplevelArray { get; set; }
        }

        public class JsonPocoQueryFixture : SharedStoreFixtureBase<JsonQueryContext>
        {
            protected override string StoreName => "JsonPocoQueryTest";
            protected override ITestStoreFactory TestStoreFactory => NpgsqlTestStoreFactory.Instance;
            public TestSqlLoggerFactory TestSqlLoggerFactory => (TestSqlLoggerFactory)ListLoggerFactory;
            protected override void Seed(JsonQueryContext context) => JsonQueryContext.Seed(context);
        }

        public class Customer
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public bool IsVip { get; set; }
            public Statistics Statistics { get; set; }
            public Order[] Orders { get; set; }
        }

        public class Statistics
        {
            public long Visits { get; set; }
            public int Purchases { get; set; }
            public NestedStatistics Nested { get; set; }
        }

        public class NestedStatistics
        {
            public int SomeProperty { get; set; }
            public int[] IntArray { get; set; }
        }

        public class Order
        {
            public decimal Price { get; set; }
            public string ShippingAddress { get; set; }
            public DateTime ShippingDate { get; set; }
        }

        #endregion
    }
}
