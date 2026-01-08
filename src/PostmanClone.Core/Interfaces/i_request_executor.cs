using PostmanClone.Core.Models;

namespace PostmanClone.Core.Interfaces;

public interface i_request_executor
{
    Task<http_response_model> execute_async(http_request_model request, CancellationToken cancellation_token);
}
