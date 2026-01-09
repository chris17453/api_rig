using Microsoft.EntityFrameworkCore;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Context;
using PostmanClone.Data.Entities;

namespace PostmanClone.Data.Stores;

public class app_registration_store : i_app_registration_store
{
    private readonly postman_clone_db_context _context;

    public app_registration_store(postman_clone_db_context context)
    {
        _context = context;
    }

    public async Task<app_registration_model?> get_registration_async(CancellationToken cancellation_token = default)
    {
        var entity = await _context.app_registrations
            .FirstOrDefaultAsync(cancellation_token);

        if (entity == null)
            return null;

        return map_to_model(entity);
    }

    public async Task save_registration_async(app_registration_model registration, CancellationToken cancellation_token = default)
    {
        var entity = new app_registration_entity
        {
            id = registration.id,
            user_email = registration.user_email,
            user_name = registration.user_name,
            organization = registration.organization,
            opted_in = registration.opted_in,
            registered_at = registration.registered_at,
            last_updated_at = registration.last_updated_at
        };

        await _context.app_registrations.AddAsync(entity, cancellation_token);
        await _context.SaveChangesAsync(cancellation_token);
    }

    public async Task update_registration_async(app_registration_model registration, CancellationToken cancellation_token = default)
    {
        var entity = await _context.app_registrations
            .FirstOrDefaultAsync(r => r.id == registration.id, cancellation_token);

        if (entity == null)
            throw new InvalidOperationException($"Registration with ID {registration.id} not found");

        entity.user_email = registration.user_email;
        entity.user_name = registration.user_name;
        entity.organization = registration.organization;
        entity.opted_in = registration.opted_in;
        entity.last_updated_at = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellation_token);
    }

    public async Task<bool> is_registered_async(CancellationToken cancellation_token = default)
    {
        return await _context.app_registrations.AnyAsync(cancellation_token);
    }

    private static app_registration_model map_to_model(app_registration_entity entity)
    {
        return new app_registration_model
        {
            id = entity.id,
            user_email = entity.user_email,
            user_name = entity.user_name,
            organization = entity.organization,
            opted_in = entity.opted_in,
            registered_at = entity.registered_at,
            last_updated_at = entity.last_updated_at
        };
    }
}
