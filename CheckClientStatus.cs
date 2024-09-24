using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using MongoDB.Driver;
using System;
using MongoDB.Bson.Serialization.Attributes;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;


public static class CheckClientStatus
{
    [FunctionName("CheckClientStatus")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        // Ler variáveis de ambiente
        var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
        var databaseId = Environment.GetEnvironmentVariable("MONGO_DATABASE_ID");
        var collectionId = Environment.GetEnvironmentVariable("MONGO_COLLECTION_ID");
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

        if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(databaseId) || string.IsNullOrEmpty(collectionId))
        {
            log.LogError("Environment variables not set.");
            return new StatusCodeResult(500); // Internal Server Error
        }

        string cpf = req.Query["cpf"];
        if (string.IsNullOrEmpty(cpf))
        {
            return new BadRequestObjectResult("Please provide a valid CPF.");
        }

        // Conectar ao MongoDB
        var client = new MongoClient(mongoConnectionString);
        var database = client.GetDatabase(databaseId);
        var collection = database.GetCollection<Client>(collectionId);

        // Consultar cliente pelo CPF
        var filter = Builders<Client>.Filter.Eq("cpf", cpf);
        var clientResult = await collection.Find(filter).FirstOrDefaultAsync();

        

        // Verificar se o cliente existe
        if (clientResult != null)
        {
            var token = GenerateJwtToken(clientResult, jwtSecret);
            req.HttpContext.Response.Headers.Add("X-Active-User", "true");
            return new OkObjectResult(new { token });
        }
        else
        {
            req.HttpContext.Response.Headers.Add("X-Active-User", "false");
            return new NotFoundObjectResult("Cliente não encontrado");
        }
    }

    private static string GenerateJwtToken(Client client, string secret)
    {
        var header = new
        {
            alg = "HS256",
            typ = "JWT"
        };

        var payload = new
        {
            name = client.name,
            cpf = client.cpf,
            mail = client.mail,
            exp = new DateTimeOffset(DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds() // Expiração do token
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var signature = CreateSignature(headerBase64, payloadBase64, secret);
        return $"{headerBase64}.{payloadBase64}.{signature}";
    }

    // Método auxiliar para criar a assinatura
    private static string CreateSignature(string header, string payload, string secret)
    {
        var key = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var message = Encoding.UTF8.GetBytes($"{header}.{payload}");
        var hash = key.ComputeHash(message);
        return Convert.ToBase64String(hash)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

}

[BsonIgnoreExtraElements]
public class Client
{
    public string name { get; set; }
    public string cpf { get; set; }
    public string mail { get; set; }
}