using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MmsRelay.Client.Application.Models;
using MmsRelay.Client.Infrastructure;
using RichardSzalay.MockHttp;

namespace MmsRelay.Client.Tests.Infrastructure;

[TestClass]
public class MmsRelayHttpClientTests
{
    private readonly MmsRelayClientOptions _options = new()
    {
        BaseUrl = "https://api.example.com",
        TimeoutSeconds = 30
    };

    private MmsRelayHttpClient CreateClient(MockHttpMessageHandler mockHttp)
    {
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
        return new MmsRelayHttpClient(httpClient, Options.Create(_options), NullLogger<MmsRelayHttpClient>.Instance);
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Return_Success_Result()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message",
            MediaUrls = new[] { new Uri("https://example.com/image.jpg") }
        };

        var expectedResult = new SendMmsResult
        {
            Provider = "twilio",
            ProviderMessageId = "SM123456",
            Status = "queued",
            ProviderMessageUri = new Uri("https://api.twilio.com/2010-04-01/Accounts/AC123/Messages/SM123456.json")
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Respond("application/json", JsonSerializer.Serialize(expectedResult, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

        var client = CreateClient(mockHttp);

        // Act
        var result = await client.SendMmsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be(expectedResult.Provider);
        result.ProviderMessageId.Should().Be(expectedResult.ProviderMessageId);
        result.Status.Should().Be(expectedResult.Status);
        result.ProviderMessageUri.Should().Be(expectedResult.ProviderMessageUri);
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Send_Correct_Request_Body()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message",
            MediaUrls = new[] { new Uri("https://example.com/image.jpg") }
        };

        var expectedResult = new SendMmsResult
        {
            Provider = "twilio",
            ProviderMessageId = "SM123456",
            Status = "queued"
        };

        string? capturedRequestBody = null;
        var mockHttp = new MockHttpMessageHandler();
        
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .With(httpRequestMessage =>
            {
                capturedRequestBody = httpRequestMessage.Content!.ReadAsStringAsync().Result;
                return true;
            })
            .Respond("application/json", JsonSerializer.Serialize(expectedResult, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

        var client = CreateClient(mockHttp);

        // Act
        await client.SendMmsAsync(request);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        
        var sentRequest = JsonSerializer.Deserialize<SendMmsRequest>(capturedRequestBody!, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        sentRequest.Should().NotBeNull();
        sentRequest!.To.Should().Be(request.To);
        sentRequest.Body.Should().Be(request.Body);
        sentRequest.MediaUrls.Should().BeEquivalentTo(request.MediaUrls);
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Throw_Exception_On_BadRequest()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "invalid-phone",
            Body = "Test message"
        };

        var errorResponse = """
        {
          "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
          "title": "One or more validation errors occurred.",
          "status": 400,
          "errors": {
            "To": ["Phone number must be in E.164 format"]
          }
        }
        """;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Respond(HttpStatusCode.BadRequest, "application/json", errorResponse);

        var client = CreateClient(mockHttp);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<MmsRelayClientException>(
            () => client.SendMmsAsync(request));

        exception.StatusCode.Should().Be(400);
        exception.Message.Should().Contain("Invalid request");
        exception.ResponseContent.Should().Be(errorResponse);
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Throw_Exception_On_Unauthorized()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message"
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Respond(HttpStatusCode.Unauthorized);

        var client = CreateClient(mockHttp);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<MmsRelayClientException>(
            () => client.SendMmsAsync(request));

        exception.StatusCode.Should().Be(401);
        exception.Message.Should().Contain("Authentication failed");
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Throw_Exception_On_ServiceUnavailable()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message"
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Respond(HttpStatusCode.ServiceUnavailable);

        var client = CreateClient(mockHttp);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<MmsRelayClientException>(
            () => client.SendMmsAsync(request));

        exception.StatusCode.Should().Be(503);
        exception.Message.Should().Contain("temporarily unavailable");
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Throw_Exception_On_TooManyRequests()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message"
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Respond(HttpStatusCode.TooManyRequests);

        var client = CreateClient(mockHttp);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<MmsRelayClientException>(
            () => client.SendMmsAsync(request));

        exception.StatusCode.Should().Be(429);
        exception.Message.Should().Contain("Rate limit exceeded");
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Throw_Exception_On_Network_Error()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message"
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Throw(new HttpRequestException("Network error"));

        var client = CreateClient(mockHttp);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<MmsRelayClientException>(
            () => client.SendMmsAsync(request));

        exception.Message.Should().Contain("Failed to communicate with MmsRelay service");
        exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Throw_Exception_On_Timeout()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message"
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Throw(new TaskCanceledException("Request timeout", new TimeoutException()));

        var client = CreateClient(mockHttp);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<MmsRelayClientException>(
            () => client.SendMmsAsync(request));

        exception.Message.Should().Contain("Request timed out");
        exception.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Handle_Cancellation()
    {
        // Arrange
        var request = new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Test message"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://api.example.com/mms")
            .Throw(new TaskCanceledException());

        var client = CreateClient(mockHttp);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => client.SendMmsAsync(request, cts.Token));
    }

    [TestMethod]
    public async Task SendMmsAsync_Should_Throw_ArgumentNullException_For_Null_Request()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var client = CreateClient(mockHttp);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => client.SendMmsAsync(null!));
    }

    [TestMethod]
    public async Task CheckHealthAsync_Should_Return_True_When_Healthy()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.example.com/health/live")
            .Respond(HttpStatusCode.OK, "text/plain", "Healthy");

        var client = CreateClient(mockHttp);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task CheckHealthAsync_Should_Return_False_When_Unhealthy()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.example.com/health/live")
            .Respond(HttpStatusCode.ServiceUnavailable);

        var client = CreateClient(mockHttp);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckHealthAsync_Should_Return_False_On_Exception()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.example.com/health/live")
            .Throw(new HttpRequestException("Network error"));

        var client = CreateClient(mockHttp);

        // Act
        var result = await client.CheckHealthAsync();

        // Assert
        result.Should().BeFalse();
    }
}