using FluentAssertions;
using MmsRelay.Client.Application.Models;
using MmsRelay.Client.Application.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MmsRelay.Client.Tests.Application.Validation;

[TestClass]
public class SendMmsCommandValidatorTests
{
    private readonly SendMmsCommandValidator _validator = new();

    [TestClass]
    public class PhoneNumberValidation : SendMmsCommandValidatorTests
    {
        [TestMethod]
        public void Should_Reject_Empty_PhoneNumber()
        {
            // Arrange
            var command = CreateValidCommand() with { To = "" };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(SendMmsCommand.To));
        }

        [TestMethod]
        public void Should_Reject_Invalid_E164_Format()
        {
            // Arrange & Act & Assert - Test various invalid formats
            var invalidNumbers = new[]
            {
                "5551234567",      // Missing country code
                "15551234567",     // Missing + prefix
                "+0551234567",     // Country code starting with 0
                "+1234567890123456", // Too long (>15 digits)
                "+1abc234567",     // Contains letters
                "+1-555-123-4567", // Contains hyphens
                "+1 555 123 4567", // Contains spaces
                "+1(555)1234567",  // Contains parentheses
                "++15551234567",   // Double plus
                "+",               // Just plus sign
                "+1",              // Too short
            };

            foreach (var invalidNumber in invalidNumbers)
            {
                var command = CreateValidCommand() with { To = invalidNumber };
                var result = _validator.Validate(command);

                result.IsValid.Should().BeFalse($"'{invalidNumber}' should be invalid");
                result.Errors.Should().Contain(e => 
                    e.PropertyName == nameof(SendMmsCommand.To) &&
                    e.ErrorMessage.Contains("E.164"));
            }
        }

        [TestMethod]
        public void Should_Accept_Valid_E164_Numbers()
        {
            // Arrange & Act & Assert - Test various valid formats
            var validNumbers = new[]
            {
                "+15551234567",    // US number
                "+442071234567",   // UK number
                "+33123456789",    // French number
                "+8613812345678",  // Chinese number
                "+12345",          // Minimum length (5 digits)
                "+123456789012345" // Maximum length (15 digits)
            };

            foreach (var validNumber in validNumbers)
            {
                var command = CreateValidCommand() with { To = validNumber };
                var result = _validator.Validate(command);

                var phoneErrors = result.Errors.Where(e => e.PropertyName == nameof(SendMmsCommand.To)).ToList();
                phoneErrors.Should().BeEmpty($"'{validNumber}' should be valid, but got errors: {string.Join(", ", phoneErrors.Select(e => e.ErrorMessage))}");
            }
        }
    }

    [TestClass]
    public class MessageBodyValidation : SendMmsCommandValidatorTests
    {
        [TestMethod]
        public void Should_Accept_Null_Body_When_MediaUrls_Provided()
        {
            // Arrange
            var command = CreateValidCommand() with 
            { 
                Body = null, 
                MediaUrls = "https://example.com/image.jpg" 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Should_Accept_Empty_Body_When_MediaUrls_Provided()
        {
            // Arrange
            var command = CreateValidCommand() with 
            { 
                Body = "", 
                MediaUrls = "https://example.com/image.jpg" 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Should_Reject_Body_Exceeding_1600_Characters()
        {
            // Arrange
            var longBody = new string('A', 1601);
            var command = CreateValidCommand() with { Body = longBody };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.PropertyName == nameof(SendMmsCommand.Body) &&
                e.ErrorMessage.Contains("1600"));
        }

        [TestMethod]
        public void Should_Accept_Body_At_1600_Characters()
        {
            // Arrange
            var maxBody = new string('A', 1600);
            var command = CreateValidCommand() with { Body = maxBody };

            // Act
            var result = _validator.Validate(command);

            // Assert
            var bodyErrors = result.Errors.Where(e => e.PropertyName == nameof(SendMmsCommand.Body)).ToList();
            bodyErrors.Should().BeEmpty();
        }
    }

    [TestClass]
    public class ServiceUrlValidation : SendMmsCommandValidatorTests
    {
        [TestMethod]
        public void Should_Accept_Valid_Http_Url()
        {
            // Arrange
            var command = CreateValidCommand() with { ServiceUrl = "http://localhost:8080" };

            // Act
            var result = _validator.Validate(command);

            // Assert
            var urlErrors = result.Errors.Where(e => e.PropertyName == nameof(SendMmsCommand.ServiceUrl)).ToList();
            urlErrors.Should().BeEmpty();
        }

        [TestMethod]
        public void Should_Accept_Valid_Https_Url()
        {
            // Arrange
            var command = CreateValidCommand() with { ServiceUrl = "https://api.example.com" };

            // Act
            var result = _validator.Validate(command);

            // Assert
            var urlErrors = result.Errors.Where(e => e.PropertyName == nameof(SendMmsCommand.ServiceUrl)).ToList();
            urlErrors.Should().BeEmpty();
        }

        [TestMethod]
        public void Should_Reject_Invalid_Urls()
        {
            // Arrange & Act & Assert
            var invalidUrls = new[]
            {
                "not-a-url",
                "ftp://example.com",
                "mailto:test@example.com",
                "file:///path/to/file",
                "",
                "http://",
                "https://",
                "://missing-scheme"
            };

            foreach (var invalidUrl in invalidUrls)
            {
                var command = CreateValidCommand() with { ServiceUrl = invalidUrl };
                var result = _validator.Validate(command);

                result.IsValid.Should().BeFalse($"'{invalidUrl}' should be invalid");
                result.Errors.Should().Contain(e => 
                    e.PropertyName == nameof(SendMmsCommand.ServiceUrl) &&
                    e.ErrorMessage.Contains("valid HTTP or HTTPS"));
            }
        }
    }

    [TestClass]
    public class MediaUrlsValidation : SendMmsCommandValidatorTests
    {
        [TestMethod]
        public void Should_Accept_Valid_Single_Media_Url()
        {
            // Arrange
            var command = CreateValidCommand() with 
            { 
                Body = null, 
                MediaUrls = "https://example.com/image.jpg" 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Should_Accept_Valid_Multiple_Media_Urls()
        {
            // Arrange
            var command = CreateValidCommand() with 
            { 
                Body = null, 
                MediaUrls = "https://example.com/image1.jpg,https://example.com/image2.png,http://test.com/video.mp4" 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Should_Accept_Media_Urls_With_Spaces()
        {
            // Arrange
            var command = CreateValidCommand() with 
            { 
                Body = null, 
                MediaUrls = " https://example.com/image1.jpg , https://example.com/image2.png " 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Should_Reject_Invalid_Media_Urls()
        {
            // Arrange & Act & Assert
            var invalidMediaUrls = new[]
            {
                "not-a-url",
                "ftp://example.com/file.jpg",
                "https://example.com/image.jpg,invalid-url",
                "https://example.com/image.jpg,ftp://bad.com/file.jpg",
                "file:///local/path/image.jpg"
            };

            foreach (var invalidMediaUrl in invalidMediaUrls)
            {
                var command = CreateValidCommand() with 
                { 
                    Body = null, 
                    MediaUrls = invalidMediaUrl 
                };
                var result = _validator.Validate(command);

                result.IsValid.Should().BeFalse($"'{invalidMediaUrl}' should be invalid");
                result.Errors.Should().Contain(e => 
                    e.PropertyName == nameof(SendMmsCommand.MediaUrls) &&
                    e.ErrorMessage.Contains("valid HTTP/HTTPS"));
            }
        }

        [TestMethod]
        public void Should_Accept_Null_Media_Urls_When_Body_Provided()
        {
            // Arrange
            var command = CreateValidCommand() with { MediaUrls = null };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }

    [TestClass]
    public class BusinessRulesValidation : SendMmsCommandValidatorTests
    {
        [TestMethod]
        public void Should_Require_Either_Body_Or_MediaUrls()
        {
            // Arrange - No body and no media URLs
            var command = CreateValidCommand() with 
            { 
                Body = null, 
                MediaUrls = null 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.ErrorMessage.Contains("Either message body or media URLs must be provided"));
        }

        [TestMethod]
        public void Should_Require_Either_Body_Or_MediaUrls_When_Both_Empty()
        {
            // Arrange - Empty body and empty media URLs
            var command = CreateValidCommand() with 
            { 
                Body = "", 
                MediaUrls = "" 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => 
                e.ErrorMessage.Contains("Either message body or media URLs must be provided"));
        }

        [TestMethod]
        public void Should_Accept_Both_Body_And_MediaUrls()
        {
            // Arrange
            var command = CreateValidCommand() with 
            { 
                Body = "Test message", 
                MediaUrls = "https://example.com/image.jpg" 
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }

    private static SendMmsCommand CreateValidCommand()
    {
        return new SendMmsCommand
        {
            To = "+15551234567",
            Body = "Test message",
            ServiceUrl = "http://localhost:8080"
        };
    }
}