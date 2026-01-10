using Core.Models;

namespace Core.Interfaces;

public interface i_collection_repository
{
    Task<postman_collection_model> import_from_file_async(string file_path, CancellationToken cancellation_token);
    Task<postman_collection_model> import_from_json_async(string json_content, CancellationToken cancellation_token);
    Task<IReadOnlyList<postman_collection_model>> list_all_async(CancellationToken cancellation_token);
    Task<postman_collection_model?> get_by_id_async(string id, CancellationToken cancellation_token);
    Task save_async(postman_collection_model collection, CancellationToken cancellation_token);
    Task delete_async(string id, CancellationToken cancellation_token);
    Task<string> export_to_json_async(string id, CancellationToken cancellation_token);
    Task export_to_file_async(string id, string file_path, CancellationToken cancellation_token);
}
