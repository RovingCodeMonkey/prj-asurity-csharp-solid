using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

public class Program
{
	public static void Main()
	{
        
        using IHost host = CreateHostBuilder().Build();
        using var scope = host.Services.CreateScope();

        var services = scope.ServiceProvider;

        try
        {
            services.GetRequiredService<UnitTests>().PerformUnitTest();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }

        // implementatinon of 'CreateHostBuilder' static method and create host object
        static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddScoped<ICustomerRepository, CustomerRepository>();
                    services.AddScoped<DbContext, DbContext>();
                    services.AddSingleton<IHttpClient>(sp => {
                        var mock = new Mock<IHttpClient>();
                        mock.Setup(h => h.Get<List<string>>(It.IsAny<string>())).Returns(new List<string>{"12345"});
                        return mock.Object;
                    });
                    services.AddSingleton<DbContext>(sp => {
                        var dbFaker = new DbContext
                        {
                            Customers = new List<Customer>() { new Customer() { ID = 1, ZipCode = "12345" } }
                        };
                        return dbFaker;
                    });
                    services.AddScoped<HttpContext, DefaultHttpContext>();
                    services.AddSingleton<UnitTests>();
                });
        }
	}
}

// INSTRUCTIONS:
// The CustomerManager class will be used inside a .NET web aplication.  It has 
// an UpdateZip() method that updates a customer's zip code after validating it.  
// That method needs to make an API call to get valid zip codes, retrieve a value
// from the current HttpContext, and access a repository class.  
//
// All of those external calls makes unit testing difficult. (It currently doesn't
// even complile because of those dependencies.)  Refactor CustomerManager 
// so that PerformUnitTest() can execute without directly invoking any of these
// other services.  After running, two PASS lines should appear in the console.  
// (If necessary, update other classes so that tests can execute.)
//
// Leave UpdateZip() in a state where someone can later make it interact with the 
// HttpContext and DbContext when run in the context of the real application.
//

public static class Logger {
    public static void Error(Exception e) {
         Console.WriteLine(e);
    }
}

public class UnitTests
{
    private readonly ICustomerRepository _repository;
    private readonly HttpContext _httpContext;
    private readonly IHttpClient _httpClient;
    public UnitTests(ICustomerRepository repository, IHttpClient httpClient, HttpContext httpContext){
        _repository = repository;
        _httpContext = httpContext;
        _httpClient = httpClient;
    }
	public void PerformUnitTest()
	{
		var sut = new CustomerManager(_repository, _httpClient, _httpContext);
        _httpContext.Items["CustomerId"] = 1;
		// No need to make any changes to this method below this line.
		var expected = true;
		var actual = sut.UpdateZip("12345"); // GOOD zip code
		var result = expected == actual ? "PASS" : "FAIL";
		Console.WriteLine(result);
		expected = false;
		actual = sut.UpdateZip("9999"); // BAD zip code
		result = expected == actual ? "PASS" : "FAIL";
		Console.WriteLine(result);
	}
}

public class CustomerManager
{
    private readonly ICustomerRepository _repository;
    private readonly HttpContext _httpContext;
    private readonly IHttpClient _httpClient;
    public CustomerManager(ICustomerRepository repository, IHttpClient httpClient, HttpContext httpContext) {
        _repository = repository;
        _httpContext = httpContext;
        _httpClient = httpClient;
    }
	public bool UpdateZip(string newZipCode)
	{
		var allZipCodes = GetAllValidZipCodes();
        if (!allZipCodes.Contains(newZipCode)) return false;
		
		int? customerId = (int?)_httpContext?.Items["CustomerId"] ?? 0;
		_repository.UpdateZip(customerId.Value, newZipCode);
		return true;
	}

	private List<string> GetAllValidZipCodes()
	{
		return _httpClient.Get<List<string>>("http://somezipcodeapi.com/GetAll");
	}
}

public class Customer
{
	public int ID { get; set; }

	public string ZipCode { get; set; }
}

public class CustomerRepository : ICustomerRepository
{
    private DbContext db;
    public CustomerRepository(DbContext db) {
        this.db = db;
    }
	public bool UpdateZip(int customerId, string newZipCode)
	{
		var customer = db.Customers.SingleOrDefault(i => i.ID == customerId);
        if (customer == null) {
            return false;
        }
        try {
            customer.ZipCode = newZipCode;
            db.SaveChanges();
            return true;
        } catch(Exception ex) {
            Logger.Error(ex);
            return false;
        }
           
	}
}

public interface ICustomerRepository {
    bool UpdateZip(int customerId,  string newZipCode);
}

public class DbContext
{
	public required List<Customer> Customers { get; set; }
    //Moot in this example, but in realistic scenarios likely necessary to propogate data.
    public void SaveChanges() {}
}

public class HttpClient : IHttpClient
{
	public T? Get<T>(string url)
	{
		// perform HTTP call and return response
		return default;
	}
}

public interface IHttpClient {
    T Get<T>(string url);
}