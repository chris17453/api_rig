using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Core.Interfaces;
using Core.Models;
using Data.Context;
using Data.Entities;

namespace Data.Repositories;

public class history_repository : i_history_repository
{
    private readonly postman_clone_db_context _context;
    private static readonly JsonSerializerSettings _json_settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None
    };

    public history_repository(postman_clone_db_context context)
    {
        _context = context;
    }

    public async Task append_async(history_entry_model entry, CancellationToken cancellation_token)
    {
        var entity = map_to_entity(entry);
        _context.history_entries.Add(entity);
        await _context.SaveChangesAsync(cancellation_token);
    }

    public async Task<IReadOnlyList<history_entry_model>> get_all_async(CancellationToken cancellation_token)
    {
        var entities = await _context.history_entries
            .OrderByDescending(e => e.executed_at)
            .AsNoTracking()
            .ToListAsync(cancellation_token);

        return entities.Select(map_to_model).ToList();
    }

    public async Task<IReadOnlyList<history_entry_model>> get_recent_async(int count, CancellationToken cancellation_token)
    {
        var entities = await _context.history_entries
            .OrderByDescending(e => e.executed_at)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellation_token);

        return entities.Select(map_to_model).ToList();
    }

    public async Task<history_entry_model?> get_by_id_async(string id, CancellationToken cancellation_token)
    {
        var entity = await _context.history_entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.id == id, cancellation_token);

        return entity is null ? null : map_to_model(entity);
    }

    public async Task delete_async(string id, CancellationToken cancellation_token)
    {
        var entity = await _context.history_entries
            .FirstOrDefaultAsync(e => e.id == id, cancellation_token);

        if (entity is not null)
        {
            _context.history_entries.Remove(entity);
            await _context.SaveChangesAsync(cancellation_token);
        }
    }

    public async Task clear_all_async(CancellationToken cancellation_token)
    {
        var all_entries = await _context.history_entries.ToListAsync(cancellation_token);
        _context.history_entries.RemoveRange(all_entries);
        await _context.SaveChangesAsync(cancellation_token);
    }

    private static history_entry_entity map_to_entity(history_entry_model model)
    {
        return new history_entry_entity
        {
            id = model.id,
            request_name = model.request_name,
            method = model.method,
            url = model.url,
            status_code = model.status_code,
            status_description = model.status_description,
            elapsed_ms = model.elapsed_ms,
            response_size_bytes = model.response_size_bytes,
            environment_id = model.environment_id,
            environment_name = model.environment_name,
            collection_id = model.collection_id,
            collection_item_id = model.collection_item_id,
            collection_name = model.collection_name,
            source_tab_id = model.source_tab_id,
            executed_at = model.executed_at,
            error_message = model.error_message,
            request_snapshot_json = model.request_snapshot is null 
                ? null 
                : JsonConvert.SerializeObject(model.request_snapshot, _json_settings),
            response_snapshot_json = model.response_snapshot is null 
                ? null 
                : JsonConvert.SerializeObject(model.response_snapshot, _json_settings)
        };
    }

    private static history_entry_model map_to_model(history_entry_entity entity)
    {
        return new history_entry_model
        {
            id = entity.id,
            request_name = entity.request_name,
            method = entity.method,
            url = entity.url,
            status_code = entity.status_code,
            status_description = entity.status_description,
            elapsed_ms = entity.elapsed_ms,
            response_size_bytes = entity.response_size_bytes,
            environment_id = entity.environment_id,
            environment_name = entity.environment_name,
            collection_id = entity.collection_id,
            collection_item_id = entity.collection_item_id,
            collection_name = entity.collection_name,
            source_tab_id = entity.source_tab_id,
            executed_at = entity.executed_at,
            error_message = entity.error_message,
            request_snapshot = string.IsNullOrEmpty(entity.request_snapshot_json) 
                ? null 
                : JsonConvert.DeserializeObject<http_request_model>(entity.request_snapshot_json),
            response_snapshot = string.IsNullOrEmpty(entity.response_snapshot_json) 
                ? null 
                : JsonConvert.DeserializeObject<http_response_model>(entity.response_snapshot_json)
        };
    }
}
