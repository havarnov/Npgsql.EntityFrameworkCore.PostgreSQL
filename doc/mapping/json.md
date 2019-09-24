# JSON Mapping

PostgreSQL has rich, built-in support for storing JSON columns and efficiently performing complex queries operations on them. Newcomers can read more about the PostgreSQL support on [the JSON types page](https://www.postgresql.org/docs/current/datatype-json.html), and on the [functions and operators page](https://www.postgresql.org/docs/current/functions-json.html). Note that the below mapping mechanisms support both the `jsonb` and `json` types, although the former is almost always preferred for efficiency reasons.

The Npgsql EF Core provider allows you to map PostgreSQL JSON columns in three different ways:

1. As simple strings
2. As strongly-typed user-defined types (POCOs)
3. As System.Text.Json DOM types (JsonDocument or JsonElement)

> [!NOTE]
> Mapping to POCO or to System.Text.Json types was introduced in version 3.0.0


## String mapping

The simplest form of mapping to JSON is via a regular string property, just like an ordinary text column:

# [Data Annotations](#tab/data-annotations)

```c#
public class SomeEntity
{
    public int Id { get; set; }
    [Column(TypeName = "jsonb")]
    public string Customer { get; set; }
}
```

# [Fluent API](#tab/fluent-api)

```c#
class MyContext : DbContext
{
    public DbSet<SomeEntity> SomeEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SomeEntity>()
            .Property(b => b.Customer)
            .HasColumnType("jsonb");
    }
}

public class SomeEntity
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
}
```

***

With string mapping, the EF Core provider will save and load properties to database JSON columns, but will not do any further serialization or parsing - it's the developer's responsibility to handle the JSON contents, possibly using System.Text.Json to parse them. This mapping approach is more limited compared to the others.

## POCO mapping

If your column's JSON documents have a stable schema, you can map them to your own .NET types (or POCOs). The provider will use [the new System.Text.Json APIs](https://www.postgresql.org/docs/current/datatype-json.html) under the hood to serialize instances to JSON documents before sending them to the database, and to deserialize documents coming from the database back. Just like EF Core can map a .NET type to rows in the table, this capability allows you to map a .NET type to a single JSON column.

Mapping POCOs is extremely easy: simply add a property with your custom POCO type and instruct the provider to map it to JSON:

# [Data Annotations](#tab/data-annotations)

```c#
public class SomeEntity
{
    public int Id { get; set; }
    [Column(TypeName = "jsonb")]
    public Customer Customer { get; set; }
}

public class Customer    // Mapped to a JSON column in the table
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Order[] Orders { get; set; }
}

public class Order       // Part of the JSON column
{
    public decimal Price { get; set; }
    public string ShippingAddress { get; set; }
}
```

# [Fluent API](#tab/fluent-api)

```c#
class MyContext : DbContext
{
    public DbSet<SomeEntity> SomeEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SomeEntity>()
            .Property(b => b.Customer)
            .HasColumnType("jsonb");
    }
}

public class SomeEntity  // Mapped to a database table
{
    public int Id { get; set; }
    public Customer Customer { get; set; }
}

public class Customer    // Mapped to a JSON column in the table
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Order[] Orders { get; set; }
}

public class Order       // Part of the JSON column
{
    public decimal Price { get; set; }
    public string ShippingAddress { get; set; }
}
```

***

You can now assign a regular `Customer` instance to the property, and once you call `SaveChanges()` it will be serialized to database, producing a  document such as the following:

```json
{
    "Age": 25,
    "Name": "Joe",
    "Orders": [
        {"Price": 9, "ShippingAddress": "Some address 1"},
        {"Price": 23, "ShippingAddress": "Some address 2"}
    ]
}
```

Reading is just as simple:

```c#
var someEntity = context.Entities.First();
Console.WriteLine(someEntity.Customer.Orders[0].Price)
```

This provides a seamless mapping approach, and supports embedding nested types and arrays, resulting in complex JSON document schemas as shown above. This approach also allows you to traverse loaded JSON documents in a type-safe way, using regular C# syntax, and to use LINQ to query inside database JSON documents (see [Querying JSON columns](#querying-json-columns) below).

## JsonDocument DOM mapping

If your column JSON schema isn't stable, a strongly-typed POCO mapping may not be appropriate. The Npgsql provider also allows you to map the DOM document type provided by [System.Text.Json APIs](https://www.postgresql.org/docs/current/datatype-json.html):

```c#
public class SomeEntity
{
    public int Id { get; set; }
    public JsonDocument Customer { get; set; }
}
```

Note that neither a data annotation nor the fluent API are required, as `JsonDocument` is automatically recognized and mapped to `jsonb`.

Once a document is loaded from the database, you can traverse it:

```c#
var someEntity = context.Entities.First();
Console.WriteLine(someEntity.Customer.GetProperty("Orders")[0].GetProperty("Price").GetInt32());
```

## Querying JSON columns

Saving and loading documentsthese documents wouldn't be much use without the ability to query them. You can express your queries via the same LINQ constructs you are already using in EF Core:

# [POCO Mapping](#tab/poco)

```c#
var joes = context.CustomerEntries
    .Where(e => e.Customer.Name == "Joe")
    .ToList();
```

# [JsonDocument Mapping](#tab/jsondocument)

```c#
var joes = context.CustomerEntries
    .Where(e => e.Customer.GetProperty("Name").GetString() == "Joe")
    .ToList();
```

***

The provider will recognize the traversal of a JSON document, and translate it to the correspond PostgreSQL JSON traversal operator, producing the following PostgreSQL-specific SQL:

```sql
SELECT c.""Id"", c.""Customer""
FROM ""CustomerEntries"" AS c
WHERE c.""Customer""->>'Name' = 'Joe'
```

[If indexes are set up properly](#indexing-json-columns), this can result in very efficient, server evaluation of searches with database JSON documents.

The following expression types and functions are translated:

# [POCO Mapping](#tab/poco)

| C# expression                                                                             | SQL generated by Npgsql      |
|-------------------------------------------------------------------------------------------|------------------------------|
| `.Where(e => e.Customer.Name == "Joe")`                                                   | [`WHERE "Customer"->>'Name' = 'Joe'`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSON-OP-TABLE)
| `.Where(e => e.Customer.Orders[1].Price = 8)`                                             | [`WHERE "Customer"#>>'{Orders,0,Price}'[1] = 8`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSON-OP-TABLE)
| `.Where(e => e.Customer.Orders.Length == 2)`                                              | [`WHERE jsonb_array_length("Customer"->'Orders' = 2`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSON-PROCESSING-TABLE)
| `.Where(e => EF.Functions.JsonContains(e.Customer, @"{""Name"": ""Joe"", ""Age"": 25}")`  | [`WHERE "Customer" @> '{"Name": "Joe", "Age": 25}'`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonContained(@"{""Name"": ""Joe"", ""Age"": 25}", e.Customer)` | [`WHERE '{"Name": "Joe", "Age": 25}' <@ "Customer"`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonExists(e.Customer, "Age")`                                  | [`WHERE "Customer" ? 'Age'`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonExistsAny(e.Customer, "Age", "Address")`                    | [`WHERE "Customer" ?\| ARRAY['Age','Address']`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonExistsAll(e.Customer, "Age", "Address")`                    | [`WHERE "Customer" ?& ARRAY['Age','Address']`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)

# [JsonDocument Mapping](#tab/jsondocument)

| C# expression                                                                             | SQL generated by Npgsql      |
|-------------------------------------------------------------------------------------------|------------------------------|
| `.Where(e => e.Customer.GetProperty("Name").GetString() == "Joe")`                        | [`WHERE "Customer"->>'Name' = 'Joe'`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSON-OP-TABLE)
| `.Where(e => e.Customer.GetProperty("Orders")[1].GetProperty("Price").GetInt32() = 8)`    | [`WHERE "Customer"#>>'{Orders,0,Price}'[1] = 8`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSON-OP-TABLE)
| `.Where(e => e.Customer.GetProperty("Orders").GetArrayLength() == 2)`                     | [`WHERE jsonb_array_length("Customer"->'Orders' = 2`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSON-PROCESSING-TABLE)
| `.Where(e => EF.Functions.JsonContains(e.Customer, @"{""Name"": ""Joe"", ""Age"": 25}")`  | [`WHERE "Customer" @> '{"Name": "Joe", "Age": 25}'`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonContained(@"{""Name"": ""Joe"", ""Age"": 25}", e.Customer)` | [`WHERE '{"Name": "Joe", "Age": 25}' <@ "Customer"`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonExists(e.Customer, "Age")`                                  | [`WHERE "Customer" ? 'Age'`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonExistsAny(e.Customer, "Age", "Address")`                    | [`WHERE "Customer" ?\| ARRAY['Age','Address']`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)
| `.Where(e => EF.Functions.JsonExistsAll(e.Customer, "Age", "Address")`                    | [`WHERE "Customer" ?& ARRAY['Age','Address']`](https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSONB-OP-TABLE)

***

## Indexing JSON columns

> [!NOTE]
> A section on indices will be added. In the meantime consult the PostgreSQL documentation and other guides on the Internet.

These are early days for EF Core JSON support, and you'll likely run into some limitations. Please let us know how the current features are working for you and what you'd like to see.

