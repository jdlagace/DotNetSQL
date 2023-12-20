using Microsoft.Data.SqlClient;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using Azure;
using Azure.Core.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

/* Setup a listener to monitor logged events.
// https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/README.md#logging
//using AzureEventSourceListener listener = AzureEventSourceListener.CreateConsoleLogger();
DefaultAzureCredentialOptions options = new DefaultAzureCredentialOptions
{
    Diagnostics =
    {
        LoggedHeaderNames = { "x-ms-request-id" },
        LoggedQueryParameters = { "api-version" },
        IsLoggingContentEnabled = true
    }
};

var credential = new DefaultAzureCredential();
var credential = new EnvironmentCredential();
var credential = new ChainedTokenCredential(new DefaultAzureCredential(), new EnvironmentCredential());
var client = new SecretClient(new Uri("https://kvdan888.vault.azure.net"), credential);
KeyVaultSecret secret = await client.GetSecretAsync("sqlConnectionString");
Console.WriteLine(secret.Name);
string connectionString = secret.Value;

 */
// For production scenarios, consider keeping Swagger configurations behind the environment check
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

string connectionString = app.Configuration.GetConnectionString("SQL_ConnectionString")!;
//    string connectionString = app.Configuration.GetConnectionString("SQLStorage");

try
{

    // Table would be created ahead of time in production
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand(
        "CREATE TABLE Persons (ID int NOT NULL PRIMARY KEY IDENTITY, FirstName varchar(255), LastName varchar(255));",
        conn);
    using SqlDataReader reader = command.ExecuteReader();
}
catch (Exception e)
{
    // Table may already exist
    Console.WriteLine(e.Message);
}

app.MapGet("/Person", () => {
    var rows = new List<string>();

    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand("SELECT * FROM Persons", conn);
    using SqlDataReader reader = command.ExecuteReader();

    if (reader.HasRows)
    {
        while (reader.Read())
        {
            rows.Add($"{reader.GetInt32(0)}, {reader.GetString(1)}, {reader.GetString(2)}");
        }
    }

    return rows;
})
.WithName("GetPersons")
.WithOpenApi();

app.MapPost("/Person", (Person person) => {
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand(
        "INSERT INTO Persons (firstName, lastName) VALUES (@firstName, @lastName)",
        conn);

    command.Parameters.Clear();
    command.Parameters.AddWithValue("@firstName", person.FirstName);
    command.Parameters.AddWithValue("@lastName", person.LastName);

    using SqlDataReader reader = command.ExecuteReader();
})
.WithName("CreatePerson")
.WithOpenApi();

app.Run();

public class Person
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}