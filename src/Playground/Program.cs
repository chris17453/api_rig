using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Core.Interfaces;
using Core.Models;
using Data.Context;
using Data.Services;
using Data.Stores;
using Http.Services;
using Scripting;

Console.WriteLine("🚀 Starting Playground...");

// 1. Setup DI Container
var services = new ServiceCollection();

// SQLite database for quick testing
services.AddDbContext<postman_clone_db_context>(options =>
    options.UseSqlite("Data Source=playground.db"));

// Register Core services
services.AddHttpClient();
services.AddSingleton<i_request_executor, http_request_executor>();
services.AddSingleton<i_script_runner>(sp => new script_runner(timeout_ms: 5000)); // 5s timeout
services.AddScoped<i_environment_store, environment_store>();
services.AddSingleton<i_variable_resolver, variable_resolver>();
services.AddScoped<request_orchestrator>();

var provider = services.BuildServiceProvider();

// 2. Initialize DB
var db = provider.GetRequiredService<postman_clone_db_context>();
await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();

Console.WriteLine("✅ Database initialized.");

// 3. Create test environment
var env_store = provider.GetRequiredService<i_environment_store>();
var env = new environment_model
{
    name = "Dev Environment",
    variables = new Dictionary<string, string>
    {
        { "base_url", "https://jsonplaceholder.typicode.com" }, // Real public API for testing
        { "user_id", "1" }
    }
};
await env_store.save_async(env, CancellationToken.None);
await env_store.set_active_async(env.id, CancellationToken.None);

Console.WriteLine($"✅ Environment '{env.name}' created and activated.");

// 4. Define Request with Scripts
var request = new http_request_model
{
    name = "Get User Todo",
    method = http_method.get,
    url = "{{base_url}}/todos/{{user_id}}", // Variable usage
    pre_request_script = @"
        console.log('🔄 [Pre-Request] Starting script...');
        var currentId = pm.environment.get('user_id');
        console.log('   Current ID:', currentId);
        
        // Dynamically modify variable
        // pm.environment.set('timestamp', new Date().toISOString());
        console.log('✅ [Pre-Request] Completed.');
    ",
    post_response_script = @"
        console.log('🔄 [Post-Response] Starting validations...');
        
        pm.test('Status code is 200', function() {
            pm.response.to.have.status(200);
        });

        pm.test('Response time is acceptable', function() {
            pm.expect(pm.response.responseTime).to.be.below(2000);
        });

        pm.test('Title exists and is a string', function() {
            var json = pm.response.json();
            pm.expect(json.title).to.be.a('string');
            console.log('   Title received:', json.title);
        });
        
        console.log('✅ [Post-Response] Completed.');
    "
};

Console.WriteLine("\n📡 Executing Request Orchestrator...");
Console.WriteLine($"   Original URL: {request.url}");

var orchestrator = provider.GetRequiredService<request_orchestrator>();
var result = await orchestrator.execute_request_async(request, CancellationToken.None);

// 5. Display Results
Console.WriteLine("\n📊 EXECUTION RESULTS");
Console.WriteLine("====================");

Console.WriteLine($"\n🔹 Final Request:");
if (result.response != null)
{
    Console.WriteLine($"   Status: {result.response.status_code} {result.response.status_description}");
    Console.WriteLine($"   Time: {result.response.elapsed_ms}ms");
    Console.WriteLine($"   Body Size: {result.response.size_bytes} bytes");
}
else
{
    Console.WriteLine("❌ No HTTP response received.");
}

Console.WriteLine("\n📝 Console Logs:");
if (result.all_logs.Any())
{
    foreach (var log in result.all_logs)
    {
        Console.WriteLine($"   {log}");
    }
}
else
{
    Console.WriteLine("   (No logs)");
}

Console.WriteLine("\n🧪 Test Results:");
if (result.all_test_results.Any())
{
    foreach (var test in result.all_test_results)
    {
        var icon = test.passed ? "✅" : "❌";
        Console.WriteLine($"   {icon} {test.name}");
        if (!test.passed)
        {
            Console.WriteLine($"      Error: {test.error_message}");
        }
    }
}
else
{
    Console.WriteLine("   (No tests executed)");
}

if (result.has_script_errors)
{
    Console.WriteLine("\n⚠️ SCRIPT ERRORS:");
    foreach (var err in result.all_errors)
    {
        Console.WriteLine($"   🔴 {err}");
    }
}

Console.WriteLine("\n🏁 Test completed.");
