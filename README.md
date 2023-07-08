C# fiddle challenge. Presumably to test SOLID principles. Sadly I never heard back so have no idea if this is what they were looking for.

My updated and now passing version is in Program.cs

```
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
	public static void Main()
	{
		new UnitTests().PerformUnitTest();
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

public class UnitTests
{
	public void PerformUnitTest()
	{
		var sut = new CustomerManager();
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
	public bool UpdateZip(string newZipCode)
	{
		var allZipCodes = GetAllValidZipCodes();
		if (allZipCodes.Contains(newZipCode))
		{
			var repository = new CustomerRepository();
			
			// The real application will need access to the current HttpContext
			Microsoft.AspNetCore.Http.HttpContext httpContext = null;
			int? customerId = (int?)httpContext?.Items["CustomerId"] ?? 0;
			repository.UpdateZip(customerId.Value, newZipCode);
			return true;
		}
		else
		{
			return false;
		}
	}

	private List<string> GetAllValidZipCodes()
	{
		var httpClient = new HttpClient();
		return httpClient.Get<List<string>>("http://somezipcodeapi.com/GetAll");
	}
}

public class Customer
{
	public int ID { get; set; }

	public string ZipCode { get; set; }
}

public class CustomerRepository
{
	public void UpdateZip(int customerId, string newZipCode)
	{
		var db = new DbContext();
		var customer = db.Customers.SingleOrDefault(i => i.ID == customerId);
		customer.ZipCode = newZipCode;
	}
}

public class DbContext
{
	public List<Customer> Customers { get; set; }
}

public class HttpClient
{
	public T Get<T>(string url)
	{
		// perform HTTP call and return response
		return default(T);
	}
}
```