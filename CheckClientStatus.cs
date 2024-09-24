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
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, client.name),
                new Claim("CPF", client.cpf),
                new Claim(ClaimTypes.Email, client.mail)
            }),
            Expires = DateTime.UtcNow.AddHours(1), // Token expiration
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

}

[BsonIgnoreExtraElements]
public class Client
{
    public string name { get; set; }
    public string cpf { get; set; }
    public string mail { get; set; }
}