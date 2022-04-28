using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        services.AddDaprClient();

        var builder = services.BuildServiceProvider();
        var queryTest = builder.GetRequiredService<QueryTest>();

        await queryTest.Execute();

    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<QueryTest>()
            .AddDaprClient();
    }

    class QueryTest
    {
        private const string StoreName = "store";
        private readonly DaprClient _daprClient;


        public QueryTest(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task Execute()
        {
            await CreateItem("entity1");

            PrintItems(await GetItems("name", "entity1"), "Item 1");
        }

        private async Task CreateItem(string name)
        {
            Console.WriteLine($"item: {name} created");
            var entity = new TestItem(name);
            var id = Guid.NewGuid().ToString();
            await _daprClient.SaveStateAsync(StoreName, id, entity);
        }

        private async Task<IEnumerable<TestItem>> GetItems(string key, string value)
        {
            string jsonQuery = $@"{{
                                'filter': {{
                                  'EQ':{{'{key}':'{value}'}}
                                }}
                              }}";
            var query = jsonQuery.Replace("'", "\"");
            var result = await _daprClient.QueryStateAsync<TestItem>(StoreName, query);
            return result.Results.Select(x => x.Data);
        }

        private void PrintItems(IEnumerable<TestItem> items, string header)
        {
            Console.WriteLine("----------------START-------------------");
            Console.WriteLine(header);
            foreach (var item in items)
            {
                Console.WriteLine($"item: {item.Name}");
            }
            Console.WriteLine("----------------END-------------------");
        }
    }

    class TestItem
    {
        public TestItem(string name)
        {
            Name = name;
        }
        public string Name { get; set; }
    }
}