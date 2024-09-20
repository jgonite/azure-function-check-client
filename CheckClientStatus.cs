using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System.Linq;

public static class CheckClientStatus
{
    private static readonly string _cosmosEndpointUri = "https://localhost:8081/";
    private static readonly string _cosmosPrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private static readonly string _databaseId = "cosmicworks";
    private static readonly string _containerId = "clients";

    [FunctionName("CheckClientStatus")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        string cpf = req.Query["cpf"];
        if (string.IsNullOrEmpty(cpf))
        {
            return new BadRequestObjectResult("Please provide a valid CPF.");
        }

        var cosmosClient = new CosmosClient(
            accountEndpoint: _cosmosEndpointUri,
            authKeyOrResourceToken: _cosmosPrimaryKey
            );

        // Habilitar para ambiente produtivo:
        /* 
        var container = cosmosClient.GetContainer(_databaseId, _containerId);
        */
        
        // Habilitar para ambiente de testes:
        ///*
        Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            id: "cosmicworks",
            throughput: 400
        );
        Container container = await database.CreateContainerIfNotExistsAsync(
            id: "clients",
            partitionKeyPath: "/cpf"
        );
        //*/
        
        ///*
        var aliosha = new{id = "1", cpf = "00413032965"};
        await container.UpsertItemAsync(aliosha);
        //*/

        // Query to find client by CPF
        var query = new QueryDefinition("SELECT * FROM clients WHERE clients.cpf = @cpf")
            .WithParameter("@cpf", cpf);
        var iterator = container.GetItemQueryIterator<Client>(query);
        var clients = await iterator.ReadNextAsync();

        // Check if client exists
        if (clients.Count > 0)
        {
            req.HttpContext.Response.Headers.Add("X-Active-User", "true");
            return new OkObjectResult("Client is active.");
        }
        else
        {
            req.HttpContext.Response.Headers.Add("X-Active-User", "false");
            return new NotFoundObjectResult("Client not found.");
        }
    }
}

public class Client
{
    public string name { get; set; }
    public string cpf { get; set; }
    public string mail { get; set; }
}