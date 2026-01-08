using FluentAssertions;
using PostmanClone.Core.Models;

namespace PostmanClone.Core.Tests.Models;

public class http_response_model_tests
{
    [Theory]
    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(204, true)]
    [InlineData(299, true)]
    [InlineData(199, false)]
    [InlineData(300, false)]
    [InlineData(400, false)]
    [InlineData(500, false)]
    public void is_success_returns_expected_when_status_code_varies(int status_code, bool expected)
    {
        var response = new http_response_model
        {
            status_code = status_code,
            status_description = "Test"
        };

        response.is_success.Should().Be(expected);
    }

    [Fact]
    public void response_stores_body_bytes_when_provided()
    {
        var body_bytes = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };

        var response = new http_response_model
        {
            status_code = 200,
            status_description = "OK",
            body_bytes = body_bytes,
            body_string = "Hello",
            size_bytes = body_bytes.Length
        };

        response.body_bytes.Should().BeEquivalentTo(body_bytes);
        response.body_string.Should().Be("Hello");
        response.size_bytes.Should().Be(5);
    }
}
