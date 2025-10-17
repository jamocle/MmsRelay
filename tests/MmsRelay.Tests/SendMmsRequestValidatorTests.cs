using System;
using FluentAssertions;
using MmsRelay.Application.Models;
using MmsRelay.Application.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MmsRelay.Tests;

[TestClass]
public class SendMmsRequestValidatorTests
{
    [TestMethod]
    public void Rejects_Invalid_E164()
    {
        var v = new SendMmsRequestValidator();
        var r = v.Validate(new SendMmsRequest { To = "5551234567", Body = "x" });
        r.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Requires_Body_Or_Media()
    {
        var v = new SendMmsRequestValidator();
        var r = v.Validate(new SendMmsRequest { To = "+15551234567" });
        r.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Accepts_Valid_Request()
    {
        var v = new SendMmsRequestValidator();
        var r = v.Validate(new SendMmsRequest
        {
            To = "+15551234567",
            Body = "Hello",
            MediaUrls = new[] { new Uri("https://example.com/img.jpg") }
        });
        r.IsValid.Should().BeTrue();
    }
}
