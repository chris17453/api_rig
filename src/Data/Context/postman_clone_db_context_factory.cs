using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Data.Context;

public class postman_clone_db_context_factory : IDesignTimeDbContextFactory<postman_clone_db_context>
{
    public postman_clone_db_context create_db_context(string[] args)
    {
        var options = new DbContextOptionsBuilder<postman_clone_db_context>()
            .UseSqlite("Data Source=api_rig.db")
            .Options;

        return new postman_clone_db_context(options);
    }

    // Required by EF Core tools
    postman_clone_db_context IDesignTimeDbContextFactory<postman_clone_db_context>.CreateDbContext(string[] args)
    {
        return create_db_context(args);
    }
}
