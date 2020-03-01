# SqlValidator
A roslyn analyzer to validate your SQL queries

Note: This project is just a proof of concept. It needs a lot more work in order to work properly.

Demo: https://youtu.be/yrTwGXqbsTs and https://youtu.be/zLTDqnNY2K4

## How to use
1. Set `SqlAnalyzer_ConnectionString` to a proper connection string.
2. Write some code:
```csharp
var connectionString = "...";
using (var connection = new SqlConnection(connectionString))
{
    await connection.OpenAsync();

    using (var command = new SqlCommand(@"SELECT * FROM PEOPLE WHERE Name LIKE @prsonName", connection))
    {
        command.Parameters.AddWithValue("personName", "%Muhammad%");

        // rest of the code
    }
}
```
3. Notice how it tells you that you have to declare `@prsonName` before using it.
4. Change `@prsonName` to `@personName` and now the warning goes away!
