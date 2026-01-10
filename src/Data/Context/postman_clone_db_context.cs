using Microsoft.EntityFrameworkCore;
using Data.Entities;

namespace Data.Context;

public class postman_clone_db_context : DbContext
{
    public postman_clone_db_context(DbContextOptions<postman_clone_db_context> options)
        : base(options)
    {
    }

    public DbSet<history_entry_entity> history_entries => Set<history_entry_entity>();
    public DbSet<collection_entity> collections => Set<collection_entity>();
    public DbSet<collection_item_entity> collection_items => Set<collection_item_entity>();
    public DbSet<environment_entity> environments => Set<environment_entity>();
    public DbSet<environment_variable_entity> environment_variables => Set<environment_variable_entity>();
    public DbSet<vault_secret_entity> vault_secrets => Set<vault_secret_entity>();
    public DbSet<workspace_entity> workspaces => Set<workspace_entity>();

    protected override void OnModelCreating(ModelBuilder model_builder)
    {
        base.OnModelCreating(model_builder);

        configure_history_entry(model_builder);
        configure_collection(model_builder);
        configure_collection_item(model_builder);
        configure_environment(model_builder);
        configure_environment_variable(model_builder);
        configure_vault_secret(model_builder);
        configure_workspace(model_builder);
    }

    private static void configure_history_entry(ModelBuilder model_builder)
    {
        model_builder.Entity<history_entry_entity>(entity =>
        {
            entity.ToTable("history_entries");
            entity.HasKey(e => e.id);

            entity.Property(e => e.id)
                .HasMaxLength(50);

            entity.Property(e => e.request_name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.method)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.url)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(e => e.status_description)
                .HasMaxLength(100);

            entity.Property(e => e.environment_id)
                .HasMaxLength(50);

            entity.Property(e => e.environment_name)
                .HasMaxLength(200);

            entity.Property(e => e.collection_id)
                .HasMaxLength(50);

            entity.Property(e => e.collection_name)
                .HasMaxLength(500);

            entity.Property(e => e.error_message)
                .HasColumnType("TEXT");

            entity.Property(e => e.request_snapshot_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.response_snapshot_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.executed_at)
                .IsRequired();

            entity.HasIndex(e => e.executed_at)
                .IsDescending();

            entity.HasIndex(e => e.collection_id);
        });
    }

    private static void configure_collection(ModelBuilder model_builder)
    {
        model_builder.Entity<collection_entity>(entity =>
        {
            entity.ToTable("collections");
            entity.HasKey(e => e.id);

            entity.Property(e => e.id)
                .HasMaxLength(50);

            entity.Property(e => e.name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.description)
                .HasColumnType("TEXT");

            entity.Property(e => e.version)
                .HasMaxLength(20);

            entity.Property(e => e.auth_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.variables_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.created_at)
                .IsRequired();

            entity.HasMany(e => e.items)
                .WithOne(i => i.collection)
                .HasForeignKey(i => i.collection_id)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void configure_collection_item(ModelBuilder model_builder)
    {
        model_builder.Entity<collection_item_entity>(entity =>
        {
            entity.ToTable("collection_items");
            entity.HasKey(e => e.id);

            entity.Property(e => e.id)
                .HasMaxLength(50);

            entity.Property(e => e.name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.description)
                .HasColumnType("TEXT");

            entity.Property(e => e.folder_path)
                .HasMaxLength(2000);

            entity.Property(e => e.request_method)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.Property(e => e.request_url)
                .HasMaxLength(2048);

            entity.Property(e => e.request_headers_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.request_query_params_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.request_body_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.request_auth_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.pre_request_script)
                .HasColumnType("TEXT");

            entity.Property(e => e.post_response_script)
                .HasColumnType("TEXT");

            entity.Property(e => e.collection_id)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.parent_item_id)
                .HasMaxLength(50);

            entity.HasOne(e => e.parent_item)
                .WithMany(e => e.children)
                .HasForeignKey(e => e.parent_item_id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.collection_id);
            entity.HasIndex(e => e.parent_item_id);
            entity.HasIndex(e => new { e.collection_id, e.sort_order });
        });
    }

    private static void configure_environment(ModelBuilder model_builder)
    {
        model_builder.Entity<environment_entity>(entity =>
        {
            entity.ToTable("environments");
            entity.HasKey(e => e.id);

            entity.Property(e => e.id)
                .HasMaxLength(50);

            entity.Property(e => e.name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.created_at)
                .IsRequired();

            entity.HasMany(e => e.variables)
                .WithOne(v => v.environment)
                .HasForeignKey(v => v.environment_id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.is_active);
        });
    }

    private static void configure_environment_variable(ModelBuilder model_builder)
    {
        model_builder.Entity<environment_variable_entity>(entity =>
        {
            entity.ToTable("environment_variables");
            entity.HasKey(e => e.id);

            entity.Property(e => e.key)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.value)
                .HasColumnType("TEXT");

            entity.Property(e => e.environment_id)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(e => new { e.environment_id, e.key })
                .IsUnique();
        });
    }

    private static void configure_vault_secret(ModelBuilder model_builder)
    {
        model_builder.Entity<vault_secret_entity>(entity =>
        {
            entity.ToTable("vault_secrets");
            entity.HasKey(e => e.id);

            entity.Property(e => e.id)
                .HasMaxLength(50);

            entity.Property(e => e.name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.description)
                .HasColumnType("TEXT");

            entity.Property(e => e.secret_type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.encrypted_value)
                .HasColumnType("TEXT");

            entity.Property(e => e.metadata_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.tags_json)
                .HasColumnType("TEXT");

            entity.Property(e => e.created_at)
                .IsRequired();

            entity.HasIndex(e => e.name);
            entity.HasIndex(e => e.secret_type);
        });
    }

    private static void configure_workspace(ModelBuilder model_builder)
    {
        model_builder.Entity<workspace_entity>(entity =>
        {
            entity.ToTable("workspaces");
            entity.HasKey(e => e.id);

            entity.Property(e => e.id)
                .HasMaxLength(50);

            entity.Property(e => e.name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.description)
                .HasColumnType("TEXT");

            entity.Property(e => e.icon)
                .HasMaxLength(50);

            entity.Property(e => e.color)
                .HasMaxLength(20);

            entity.Property(e => e.created_at)
                .IsRequired();

            entity.HasIndex(e => e.is_active);
            entity.HasIndex(e => e.name);
        });
    }
}
