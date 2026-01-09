using PostmanClone.Core.Models;

namespace PostmanClone.Core.Interfaces;

public interface i_app_registration_store
{
    Task<app_registration_model?> get_registration_async(CancellationToken cancellation_token = default);
    Task save_registration_async(app_registration_model registration, CancellationToken cancellation_token = default);
    Task update_registration_async(app_registration_model registration, CancellationToken cancellation_token = default);
    Task<bool> is_registered_async(CancellationToken cancellation_token = default);
}
