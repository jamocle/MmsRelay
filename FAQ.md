# MmsRelay Frequently Asked Questions (FAQ)

## Getting Started

### Q: What is MmsRelay and why would I use it?
**A:** MmsRelay is a service that makes it easy for your applications to send MMS messages (text messages with pictures/files) through Twilio. Instead of each of your applications connecting directly to Twilio, they all use this service as a middleman.

**Benefits:**
- **Consistency**: All your apps use the same interface
- **Reliability**: Built-in retry logic and error handling
- **Security**: Centralized credential management
- **Monitoring**: Centralized logging and health checks

### Q: Do I need a Twilio account?
**A:** Yes, you need a Twilio account to actually send messages. MmsRelay is a "relay" service - it forwards your requests to Twilio but doesn't send messages itself.

**What you need from Twilio:**
- Account SID (starts with "AC...")
- Auth Token (secret key)
- A phone number (starts with "+1" for US) OR a Messaging Service SID

### Q: How much does it cost?
**A:** MmsRelay itself is free (open source), but you pay Twilio for each message sent. Current Twilio pricing (as of 2024):
- **SMS**: ~$0.0075 per message in the US
- **MMS**: ~$0.02 per message in the US
- International rates vary by country

## Phone Numbers & Formatting

### Q: What's this E.164 format everyone keeps mentioning?
**A:** E.164 is the international standard for phone numbers. It's simpler than it sounds:

**Format:** `+[country code][phone number]` (no spaces, dashes, or parentheses)

**Examples:**
- US number (555) 123-4567 → `+15551234567`
- UK number 020 1234 5678 → `+442012345678`
- German number 030 12345678 → `+4930123456789`

**Why this format?** It ensures your messages reach the right person worldwide, not just locally.

### Q: Can I send to numbers without the + sign?
**A:** No, the service requires E.164 format starting with `+`. This prevents confusion between countries (is "123456789" a US number or German number?).

### Q: What happens if I use the wrong phone number format?
**A:** The service will reject your request with a validation error before sending anything to Twilio. This saves you money on invalid attempts.

## Technical Questions

### Q: Do I need to know C# to use this?
**A:** Not necessarily! While MmsRelay is written in C#, you can call it from any programming language that can make HTTP requests:

- **C#**: Use the included console client or HttpClient
- **Python**: Use `requests` library
- **JavaScript**: Use `fetch()` or `axios`
- **curl**: Command-line HTTP tool
- **Postman**: Graphical API testing tool

### Q: What's the difference between the service and the client?
**A:** 
- **Service** (`src/MmsRelay`): The HTTP API that receives requests and forwards them to Twilio
- **Client** (`clients/MmsRelay.Client`): A command-line tool that makes it easy to send requests to the service

**Analogy**: The service is like a post office, the client is like a mail truck that brings letters to the post office.

### Q: Can multiple applications use the same MmsRelay service?
**A:** Yes! That's one of the main benefits. You run one MmsRelay service and all your applications send requests to it.

### Q: What happens if the service is down?
**A:** Your applications will get HTTP errors when trying to send messages. Consider implementing retry logic in your applications, or use a message queue for critical messages.

## Development & Testing

### Q: How do I test without sending real messages?
**A:** Several options:

1. **Twilio Test Credentials**: Use Twilio's test Account SID and Auth Token (they won't actually send messages)
2. **Twilio Console**: Use their web interface to verify your credentials work
3. **Integration Tests**: The project includes tests that mock Twilio responses
4. **Health Check**: Use `GET /health/ready` to test connectivity without sending messages

### Q: What's all this "Clean Architecture" and "SOLID" stuff?
**A:** These are software design principles that make code easier to maintain and test. As a beginner:

- **Clean Architecture**: Code is organized in layers (like floors of a building)
- **SOLID**: Five rules that help you write better code
- **Don't worry about understanding everything at once** - focus on getting it running first, learn the patterns over time

**See our [GLOSSARY.md](GLOSSARY.md) for detailed explanations.**

### Q: Do I need Docker to run this?
**A:** No! Docker is optional. You can run MmsRelay directly with:
```bash
cd src/MmsRelay
dotnet run
```

Docker is useful for production deployment but not required for development.

### Q: What's the difference between Development and Production configurations?
**A:** 
- **Development**: Detailed logging, uses User Secrets for credentials, allows HTTP
- **Production**: Less logging, uses environment variables for credentials, requires HTTPS

## Error Messages & Troubleshooting

### Q: I'm getting "Phone number must be in E.164 format"
**A:** Your phone number needs to start with `+` followed by country code. See the phone number format question above.

### Q: I'm getting "Unable to connect to MmsRelay service"
**A:** Check these in order:
1. Is the service running? Try `http://localhost:5000/health/live`
2. Are you using the right URL? Default is `http://localhost:5000`
3. Is there a firewall blocking the connection?
4. Check the service logs for error messages

### Q: I'm getting "Authentication failed" (HTTP 401)
**A:** This usually means your Twilio credentials are wrong:
1. Double-check your Account SID and Auth Token in Twilio Console
2. Make sure you're using the right environment (test vs live credentials)
3. Verify your credentials are set correctly (User Secrets for dev, environment variables for production)

### Q: I'm getting "Rate limit exceeded" (HTTP 429)  
**A:** Twilio has limits on how many messages you can send:
1. Check your Twilio account limits
2. Implement delays between requests
3. Consider upgrading your Twilio account if needed

### Q: Messages aren't being delivered
**A:** This is usually a Twilio issue, not MmsRelay:
1. Check Twilio Console for delivery status
2. Verify the recipient phone number is correct and can receive messages
3. Some carriers block messages from new numbers - check with recipient
4. International messages may need account approval

### Q: The service starts but crashes immediately
**A:** Common causes:
1. **Missing configuration**: Set Twilio credentials
2. **Port already in use**: Another service is using port 5000/5001
3. **Permission issues**: Run as administrator or check file permissions
4. **Missing dependencies**: Run `dotnet restore`

Check the console output or logs for specific error messages.

## Configuration & Deployment

### Q: How do I set up Twilio credentials securely?
**A:** It depends on your environment:

**Development (your laptop):**
```bash
cd src/MmsRelay
dotnet user-secrets set "twilio:accountSid" "ACxxxxx"
dotnet user-secrets set "twilio:authToken" "your_token"
dotnet user-secrets set "twilio:fromPhoneNumber" "+15551234567"
```

**Production (server):**
```bash
export TWILIO__ACCOUNTSID=ACxxxxx
export TWILIO__AUTHTOKEN=your_token  
export TWILIO__FROMPHONENUMBER=+15551234567
```

**Never put credentials in code or commit them to Git!**

### Q: Can I run this on Linux/Mac?
**A:** Yes! .NET 8 runs on Windows, Mac, and Linux. The instructions are the same, just use the appropriate terminal for your OS.

### Q: How do I know if it's working in production?
**A:** Use the health check endpoints:
- `GET /health/live` - Is the service running?
- `GET /health/ready` - Can it connect to Twilio?

Monitor these endpoints with your monitoring system (Pingdom, New Relic, etc.).

### Q: How many messages per second can it handle?
**A:** This depends more on Twilio's limits than MmsRelay's performance. Twilio typically limits new accounts to 1 message per second, with higher rates available on request.

MmsRelay itself can handle much higher rates - the bottleneck will be Twilio's API.

## Console Client Questions

### Q: What's the difference between `dotnet run` and just running the executable?
**A:** 
- `dotnet run`: Compiles and runs from source code (development)
- `./MmsRelay.Client.exe`: Runs pre-compiled executable (production)

For development, use `dotnet run`. For production deployment, publish an executable.

### Q: Can I use the client in scripts?
**A:** Yes! The client returns appropriate exit codes:
- **0**: Success
- **1**: Error (validation, network, etc.)

```bash
#!/bin/bash
if ./MmsRelay.Client send --to "+15551234567" --body "Server backup completed"; then
    echo "Notification sent successfully"
else
    echo "Failed to send notification"
fi
```

### Q: How do I send to multiple people?
**A:** The client sends to one person at a time. For multiple recipients, use a loop:

```bash
# Bash example
for phone in "+15551234567" "+15551234568" "+15551234569"; do
    dotnet run -- send --to "$phone" --body "System maintenance tonight"
done
```

## Performance & Reliability

### Q: What happens if Twilio is down?
**A:** MmsRelay has built-in resilience patterns:
1. **Retry**: Automatically retries failed requests up to 3 times
2. **Circuit Breaker**: Stops trying if Twilio is consistently down
3. **Timeout**: Gives up on requests that take too long

Your application will receive an error response that you should handle appropriately.

### Q: Should I retry failed requests from my application?
**A:** MmsRelay already does retries, so you usually don't need to. However, you might want to retry:
- Network errors connecting to MmsRelay
- Validation errors after fixing the data
- Rate limit errors after waiting

**Don't retry server errors (5xx) immediately** - MmsRelay already tried multiple times.

### Q: How do I monitor message delivery?
**A:** Use Twilio Console to see delivery status. MmsRelay logs the Twilio message ID that you can use to look up status.

For automated monitoring, you could:
1. Store the returned message ID
2. Use Twilio's webhooks to get delivery notifications
3. Use Twilio's API to query message status

---

## Still Need Help?

If your question isn't answered here:

1. **Check the logs** - Most issues show helpful error messages
2. **Review the [GLOSSARY.md](GLOSSARY.md)** - Understand the technical terms  
3. **Check Twilio Console** - Many issues are on Twilio's side
4. **Open a GitHub Issue** - Include error messages and steps to reproduce
5. **Read the detailed documentation** in each project folder

**When asking for help, always include:**
- What you were trying to do
- What command you ran
- What error message you got
- Your operating system
- Whether you're using development or production configuration