using Bogus;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

ElasticsearchClientSettings settings = new(new Uri("http://localhost:9200"));
settings.DefaultIndex("products");

ElasticsearchClient client = new(settings);
client.IndexAsync("products").GetAwaiter().GetResult(); //Await kullanmak yerine bunuda yazabiliriz indexi oluþturduk


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.MapPost("/products/create", async (CreateProductDto request, CancellationToken canncellation) =>
{
    Product product = new()
    {
        Name = request.Name,
        Price = request.Price,
        Stock = request.Stock,
        Descripion = request.Description
    };

    CreateRequest<Product> createRequest = new(product.Id.ToString())
    {
        Document = product,
    };
    CreateResponse createResponse = await client.CreateAsync(createRequest, canncellation);
    return Results.Ok(createResponse.Id);

});

app.MapPut("/products/update", async (UpdateProductDto request, CancellationToken canncellation) =>
{


    UpdateRequest<Product, UpdateProductDto> updateProduct = new("products", request.Id.ToString())
    {
        Doc = request
    };

    UpdateResponse<Product> updateResponse = await client.UpdateAsync(updateProduct, canncellation);
    return Results.Ok(updateResponse);

});

app.MapDelete("/products/deletebyId", async (Guid id, CancellationToken canncellation) =>
{


    DeleteRequest<Product> deleteRequest = new("products", id.ToString());
    DeleteResponse deleteResponse = await client.DeleteAsync("products", id, canncellation);

    return Results.Ok(deleteResponse);

});


app.MapGet("/products/getall", async (CancellationToken cancellation) =>
{
    SearchRequest searchRequest = new("products")
    {
        Size = 100,
        Sort = new List<SortOptions>
        {
            SortOptions.Field(new Field("name.keyword"), new FieldSort() { Order = SortOrder.Asc }),
        }
        //Query = new MatchQuery(new Field("name"))
        //{
        //    Query = "domates" //aranacak deðeri yazdýk
        //}

    };
    SearchResponse<Product> response = await client.SearchAsync<Product>("products", cancellation);

    return Results.Ok(response.Documents); //Elimizde oluþan veriyi görmek için yazdýk documents=satýrdýr 
});


app.MapGet("/products/seeddata", async (CancellationToken cancellation) =>
{

    for (int i = 0; i < 100; i++)
    {
        Faker faker = new Faker();
        Product product = new()
        {
            Name = faker.Commerce.ProductName(),
            Price = Convert.ToDecimal(faker.Commerce.Price()),
            Stock = faker.Commerce.Random.Int(1, 20),
            Descripion = faker.Commerce.ProductDescription(),
        };
        CreateRequest<Product> createRequest = new(product.Id.ToString())
        {
            Document = product
        };
        await client.CreateAsync(createRequest, cancellation);

    }
    return Results.Created();

});





app.Run();


class Product
{
    public Product()
    {
        Id = Guid.NewGuid(); //Her defasýnda id deðerini bir arttýrmasý gerektiðini söyledik
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Descripion { get; set; } = default!;
}

record CreateProductDto(
    string Name,
    decimal Price,
    int Stock,
    string Description
    );
//id eklememe sebebimiz ctor her istek attýðýmýzda bizim yerimizi arttýracak id deðerini
record UpdateProductDto(
    Guid Id,
    string Name,
    decimal Price,
    int Stock,
    string Description
    );