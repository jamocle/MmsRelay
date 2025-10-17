# MmsRelay Examples - Real-World Usage Scenarios

This document shows complete, real-world examples of how to use MmsRelay in different situations.

## Table of Contents

1. [Basic Examples](#basic-examples)
2. [E-Commerce Scenarios](#e-commerce-scenarios)
3. [System Administration](#system-administration)
4. [Marketing & Customer Engagement](#marketing--customer-engagement)
5. [Integration Examples](#integration-examples)
6. [Troubleshooting Scenarios](#troubleshooting-scenarios)

---

## Basic Examples

### Example 1: Send a Simple Text Message

**Scenario**: Send a basic notification to a user.

**Using the Console Client:**
```bash
cd clients/MmsRelay.Client
dotnet run -- send --to "+15551234567" --body "Your appointment is confirmed for tomorrow at 2 PM"
```

**Using curl (any programming language can do similar):**
```bash
curl -X POST http://localhost:5000/mms \
  -H "Content-Type: application/json" \
  -d '{
    "to": "+15551234567",
    "body": "Your appointment is confirmed for tomorrow at 2 PM"
  }'
```

**Expected Response:**
```json
{
  "provider": "twilio",
  "providerMessageId": "SM1234567890abcdef",
  "status": "queued",
  "providerMessageUri": "https://api.twilio.com/2010-04-01/Accounts/AC.../Messages/SM..."
}
```

### Example 2: Send Image with Text

**Scenario**: Send a promotional message with an attached image.

```bash
dotnet run -- send \
  --to "+15551234567" \
  --body "🎉 Flash Sale! 50% off everything until midnight!" \
  --media "https://yourdomain.com/images/flash-sale-banner.jpg"
```

### Example 3: Send Multiple Media Files

**Scenario**: Send order confirmation with receipt and product images.

```bash
dotnet run -- send \
  --to "+15551234567" \
  --body "Order #12345 confirmed! Here's your receipt and product photos." \
  --media "https://yourdomain.com/receipts/order-12345.pdf,https://yourdomain.com/products/widget-photo.jpg,https://yourdomain.com/products/widget-manual.pdf"
```

---

## E-Commerce Scenarios

### Example 4: Order Confirmation System

**Scenario**: Customer places an order, system sends confirmation with receipt.

**Script Version:**
```bash
#!/bin/bash
# order-confirmation.sh

ORDER_ID=$1
CUSTOMER_PHONE=$2
RECEIPT_URL=$3

echo "Sending order confirmation for order $ORDER_ID..."

if dotnet run -- send \
  --to "$CUSTOMER_PHONE" \
  --body "✅ Order #$ORDER_ID confirmed! Thank you for your purchase. Your receipt is attached." \
  --media "$RECEIPT_URL" \
  --verbose; then
    echo "✅ Order confirmation sent successfully"
    # Log to order system that notification was sent
    curl -X POST "https://your-order-system.com/api/orders/$ORDER_ID/notification-sent"
else
    echo "❌ Failed to send order confirmation"
    # Alert admin about failed notification
    dotnet run -- send \
      --to "+15551234999" \
      --body "🚨 ALERT: Failed to send order confirmation for order #$ORDER_ID to $CUSTOMER_PHONE"
fi
```

**Usage:**
```bash
./order-confirmation.sh "12345" "+15551234567" "https://receipts.mystore.com/order-12345.pdf"
```

### Example 5: Shipping Notifications

**Scenario**: Notify customers when orders ship with tracking information.

```bash
# shipping-notification.sh
#!/bin/bash

TRACKING_NUMBER=$1
CUSTOMER_PHONE=$2
CARRIER=$3

dotnet run -- send \
  --to "$CUSTOMER_PHONE" \
  --body "📦 Your order has shipped! Track your package: $TRACKING_NUMBER via $CARRIER. Expected delivery: 2-3 business days." \
  --media "https://tracking-images.mystore.com/map-$TRACKING_NUMBER.png"
```

### Example 6: Abandoned Cart Recovery

**Scenario**: Send reminder to customers who left items in their cart.

```bash
# abandoned-cart.sh - Run this as a scheduled job
#!/bin/bash

# Get abandoned carts from your database (pseudo-code)
# In real life, this would query your database
ABANDONED_CARTS=$(curl -s "https://your-api.com/abandoned-carts")

echo "$ABANDONED_CARTS" | while read cart_data; do
    PHONE=$(echo "$cart_data" | jq -r '.phone')
    CART_VALUE=$(echo "$cart_data" | jq -r '.total')
    CART_IMAGE=$(echo "$cart_data" | jq -r '.preview_image')
    
    dotnet run -- send \
      --to "$PHONE" \
      --body "🛒 Don't forget your items! Complete your $cart_value order and get free shipping." \
      --media "$CART_IMAGE"
        
    sleep 2  # Rate limiting - don't overwhelm Twilio
done
```

---

## System Administration

### Example 7: Server Monitoring Alerts

**Scenario**: Monitor server health and alert admins when issues occur.

```bash
# server-monitor.sh - Run every 5 minutes via cron
#!/bin/bash

ADMIN_PHONE="+15551234999"
SERVER_NAME="web-server-01"

# Check if service is responding
if ! curl -f -s "http://localhost:8080/health" > /dev/null; then
    # Service is down, send alert
    dotnet run -- send \
      --to "$ADMIN_PHONE" \
      --body "🚨 CRITICAL: $SERVER_NAME is not responding to health checks. Investigate immediately!" \
      --service-url "https://mmsrelay.yourcompany.com"
    
    # Also send to backup admin
    dotnet run -- send \
      --to "+15551234998" \
      --body "🚨 BACKUP ALERT: $SERVER_NAME down, primary admin notified"
fi

# Check disk space
DISK_USAGE=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -gt 85 ]; then
    dotnet run -- send \
      --to "$ADMIN_PHONE" \
      --body "⚠️ WARNING: $SERVER_NAME disk usage is ${DISK_USAGE}%. Consider cleanup or expansion."
fi

# Check memory usage
MEMORY_USAGE=$(free | grep Mem | awk '{printf "%.0f", $3/$2 * 100.0}')
if [ "$MEMORY_USAGE" -gt 90 ]; then
    dotnet run -- send \
      --to "$ADMIN_PHONE" \
      --body "⚠️ WARNING: $SERVER_NAME memory usage is ${MEMORY_USAGE}%. Check for memory leaks."
fi
```

### Example 8: Backup Completion Notifications

**Scenario**: Notify when automated backups complete (or fail).

```bash
# backup-notify.sh
#!/bin/bash

BACKUP_TYPE=$1  # "database" or "files"  
BACKUP_STATUS=$2  # "success" or "failure"
BACKUP_SIZE=$3   # e.g., "2.3GB"
ADMIN_PHONE="+15551234999"

if [ "$BACKUP_STATUS" = "success" ]; then
    dotnet run -- send \
      --to "$ADMIN_PHONE" \
      --body "✅ $BACKUP_TYPE backup completed successfully. Size: $BACKUP_SIZE. $(date)"
else
    dotnet run -- send \
      --to "$ADMIN_PHONE" \
      --body "❌ CRITICAL: $BACKUP_TYPE backup FAILED at $(date). Check logs immediately!"
        
    # Also notify backup admin
    dotnet run -- send \
      --to "+15551234998" \
      --body "❌ BACKUP FAILURE: $BACKUP_TYPE backup failed, primary admin notified"
fi
```

### Example 9: Database Maintenance Windows

**Scenario**: Notify users before and after scheduled maintenance.

```bash
# maintenance-notification.sh
#!/bin/bash

PHASE=$1  # "before", "starting", "completed"
USER_PHONES="+15551234567 +15551234568 +15551234569"  # In reality, query from database

case $PHASE in
    "before")
        MESSAGE="🔧 Scheduled maintenance in 30 minutes (11:30 PM - 12:30 AM EST). Service may be briefly unavailable."
        ;;
    "starting")  
        MESSAGE="🔧 Maintenance starting now. Service temporarily unavailable. Expected completion: 12:30 AM EST."
        ;;
    "completed")
        MESSAGE="✅ Maintenance completed! All services are back online. Thank you for your patience."
        ;;
esac

for PHONE in $USER_PHONES; do
    dotnet run -- send --to "$PHONE" --body "$MESSAGE"
    sleep 1  # Rate limiting
done
```

---

## Marketing & Customer Engagement

### Example 10: Birthday Promotions

**Scenario**: Send personalized birthday offers to customers.

```bash
# birthday-promotions.sh - Run daily via cron
#!/bin/bash

# Get today's birthdays from customer database (pseudo-code)
BIRTHDAYS=$(curl -s "https://your-crm.com/api/birthdays/today")

echo "$BIRTHDAYS" | while read customer; do
    PHONE=$(echo "$customer" | jq -r '.phone')
    FIRST_NAME=$(echo "$customer" | jq -r '.firstName')
    COUPON_CODE=$(echo "$customer" | jq -r '.birthdayCode')
    
    dotnet run -- send \
      --to "$PHONE" \
      --body "🎂 Happy Birthday, $FIRST_NAME! Enjoy 25% off your next purchase with code: $COUPON_CODE (valid for 7 days)" \
      --media "https://images.yourstore.com/birthday-promotion.jpg"
        
    sleep 2  # Rate limiting
done
```

### Example 11: Flash Sale Announcements

**Scenario**: Announce time-sensitive sales to your customer base.

```bash
# flash-sale.sh
#!/bin/bash

SALE_END_TIME="11:59 PM EST"
DISCOUNT_PERCENT="40"
SALE_IMAGE="https://images.yourstore.com/flash-sale-40-percent.jpg"

# Get opt-in customers from database
CUSTOMERS=$(curl -s "https://your-crm.com/api/customers/sms-optin")

echo "Starting flash sale notification to $(echo "$CUSTOMERS" | wc -l) customers..."

echo "$CUSTOMERS" | while read customer; do
    PHONE=$(echo "$customer" | jq -r '.phone')
    
    dotnet run -- send \
      --to "$PHONE" \
      --body "⚡ FLASH SALE: $DISCOUNT_PERCENT% off EVERYTHING! Ends tonight at $SALE_END_TIME. Shop now!" \
      --media "$SALE_IMAGE"
    
    # Track notification sent
    CUSTOMER_ID=$(echo "$customer" | jq -r '.id')
    curl -X POST "https://your-crm.com/api/customers/$CUSTOMER_ID/notifications" \
      -d '{"type": "flash_sale", "sent_at": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}'
    
    sleep 1  # Rate limiting - 1 message per second
done

echo "Flash sale notifications completed!"
```

---

## Integration Examples

### Example 12: Python Integration

**Scenario**: Send MMS from a Python web application.

```python
# mms_client.py
import requests
import logging
from typing import Optional, List

class MmsRelayClient:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url.rstrip('/')
        self.session = requests.Session()
        self.session.headers.update({'Content-Type': 'application/json'})
    
    def send_mms(self, to: str, body: Optional[str] = None, media_urls: Optional[List[str]] = None) -> dict:
        """Send an MMS message via MmsRelay service."""
        if not body and not media_urls:
            raise ValueError("Either body or media_urls must be provided")
            
        payload = {"to": to}
        if body:
            payload["body"] = body
        if media_urls:
            payload["mediaUrls"] = media_urls
            
        try:
            response = self.session.post(f"{self.base_url}/mms", json=payload)
            response.raise_for_status()
            return response.json()
        except requests.exceptions.RequestException as e:
            logging.error(f"Failed to send MMS to {to}: {e}")
            raise
    
    def health_check(self) -> bool:
        """Check if MmsRelay service is healthy."""
        try:
            response = self.session.get(f"{self.base_url}/health/live")
            return response.status_code == 200
        except requests.exceptions.RequestException:
            return False

# Usage example
if __name__ == "__main__":
    client = MmsRelayClient("https://mmsrelay.yourcompany.com")
    
    # Send order confirmation
    try:
        result = client.send_mms(
            to="+15551234567",
            body="Order #12345 confirmed! Your receipt is attached.",
            media_urls=["https://receipts.mystore.com/order-12345.pdf"]
        )
        print(f"Message sent! Twilio ID: {result['providerMessageId']}")
    except Exception as e:
        print(f"Failed to send message: {e}")
        
        # Send admin alert
        client.send_mms(
            to="+15551234999",
            body=f"🚨 Failed to send order confirmation: {e}"
        )
```

### Example 13: C# Web Application Integration

**Scenario**: Integrate MmsRelay into an ASP.NET Core application.

```csharp
// Services/IMmsService.cs
public interface IMmsService
{
    Task<SendMmsResult> SendOrderConfirmationAsync(string phoneNumber, string orderId, string receiptUrl);
    Task<bool> IsHealthyAsync();
}

// Services/MmsService.cs
public class MmsService : IMmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MmsService> _logger;

    public MmsService(HttpClient httpClient, ILogger<MmsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SendMmsResult> SendOrderConfirmationAsync(string phoneNumber, string orderId, string receiptUrl)
    {
        var request = new SendMmsRequest
        {
            To = phoneNumber,
            Body = $"✅ Order #{orderId} confirmed! Thank you for your purchase. Your receipt is attached.",
            MediaUrls = new[] { receiptUrl }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/mms", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SendMmsResult>();
                _logger.LogInformation("Order confirmation sent for {OrderId} to {PhoneNumber}", 
                    orderId, phoneNumber);
                return result;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send MMS for order {OrderId}: {StatusCode} {Error}", 
                    orderId, response.StatusCode, error);
                throw new InvalidOperationException($"MMS sending failed: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending MMS for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health/live");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// Program.cs registration
builder.Services.AddHttpClient<IMmsService, MmsService>(client => 
{
    client.BaseAddress = new Uri("https://mmsrelay.yourcompany.com");
});
```

### Example 14: Node.js Integration

**Scenario**: Send notifications from a Node.js application.

```javascript
// mms-client.js
const axios = require('axios');

class MmsRelayClient {
    constructor(baseUrl = 'http://localhost:5000') {
        this.baseUrl = baseUrl.replace(/\/$/, '');
        this.client = axios.create({
            baseURL: this.baseUrl,
            headers: { 'Content-Type': 'application/json' },
            timeout: 30000
        });
    }

    async sendMms(to, body = null, mediaUrls = null) {
        if (!body && (!mediaUrls || mediaUrls.length === 0)) {
            throw new Error('Either body or mediaUrls must be provided');
        }

        const payload = { to };
        if (body) payload.body = body;
        if (mediaUrls) payload.mediaUrls = mediaUrls;

        try {
            const response = await this.client.post('/mms', payload);
            return response.data;
        } catch (error) {
            console.error(`Failed to send MMS to ${to}:`, error.response?.data || error.message);
            throw error;
        }
    }

    async healthCheck() {
        try {
            const response = await this.client.get('/health/live');
            return response.status === 200;
        } catch {
            return false;
        }
    }
}

// Usage example
async function sendWelcomeMessage(customerPhone, customerName) {
    const mmsClient = new MmsRelayClient('https://mmsrelay.yourcompany.com');
    
    try {
        const result = await mmsClient.sendMms(
            customerPhone,
            `Welcome to our service, ${customerName}! Here's your getting started guide.`,
            ['https://guides.yourcompany.com/welcome-guide.pdf']
        );
        
        console.log(`Welcome message sent! Twilio ID: ${result.providerMessageId}`);
        return result;
    } catch (error) {
        console.error('Failed to send welcome message:', error);
        
        // Alert admin
        await mmsClient.sendMms(
            '+15551234999',
            `🚨 Failed to send welcome message to ${customerPhone}: ${error.message}`
        );
        
        throw error;
    }
}

module.exports = { MmsRelayClient, sendWelcomeMessage };
```

---

## Troubleshooting Scenarios

### Example 15: Connection Testing Script

**Scenario**: Diagnose connection issues between your app and MmsRelay.

```bash
#!/bin/bash
# diagnose-connection.sh

SERVICE_URL=${1:-"http://localhost:5000"}
echo "🔍 Diagnosing connection to MmsRelay at $SERVICE_URL"
echo "================================================"

# Test 1: Basic connectivity
echo "1. Testing basic connectivity..."
if curl -f -s "$SERVICE_URL/health/live" > /dev/null; then
    echo "✅ Service is responding"
else
    echo "❌ Service is not responding"
    echo "   - Check if MmsRelay service is running"
    echo "   - Verify the URL is correct"
    echo "   - Check firewall settings"
    exit 1
fi

# Test 2: Twilio connectivity  
echo "2. Testing Twilio connectivity..."
if curl -f -s "$SERVICE_URL/health/ready" > /dev/null; then
    echo "✅ Twilio connectivity OK"
else
    echo "❌ Cannot connect to Twilio"
    echo "   - Check Twilio credentials"
    echo "   - Verify internet connectivity"
    echo "   - Check Twilio service status"
fi

# Test 3: Send test message (requires phone number)
if [ ! -z "$2" ]; then
    echo "3. Testing message sending to $2..."
    
    RESPONSE=$(curl -s -w "HTTPSTATUS:%{http_code}" \
        -X POST "$SERVICE_URL/mms" \
        -H "Content-Type: application/json" \
        -d "{\"to\": \"$2\", \"body\": \"Test message from MmsRelay diagnostics - $(date)\"}")
    
    HTTP_STATUS=$(echo $RESPONSE | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    BODY=$(echo $RESPONSE | sed -e 's/HTTPSTATUS\:.*//g')
    
    if [ "$HTTP_STATUS" -eq 202 ]; then
        echo "✅ Test message sent successfully"
        echo "   Response: $BODY"
    else
        echo "❌ Test message failed (HTTP $HTTP_STATUS)"
        echo "   Error: $BODY"
    fi
else
    echo "3. Skipping message test (no phone number provided)"
    echo "   To test messaging: $0 $SERVICE_URL +15551234567"
fi

echo "================================================"
echo "Diagnosis complete!"
```

**Usage:**
```bash
./diagnose-connection.sh "https://mmsrelay.yourcompany.com" "+15551234567"
```

### Example 16: Rate Limit Handling

**Scenario**: Send messages to many recipients while respecting rate limits.

```bash
#!/bin/bash
# bulk-send-with-limits.sh

PHONE_LIST_FILE=$1
MESSAGE_BODY=$2
MEDIA_URL=$3
MAX_PER_SECOND=1  # Twilio default for new accounts

if [ ! -f "$PHONE_LIST_FILE" ]; then
    echo "Usage: $0 <phone-list-file> <message> [media-url]"
    echo "Phone list file should contain one phone number per line"
    exit 1
fi

TOTAL_PHONES=$(wc -l < "$PHONE_LIST_FILE")
echo "📱 Sending to $TOTAL_PHONES recipients at $MAX_PER_SECOND messages/second"
echo "⏱️  Estimated completion: $(($TOTAL_PHONES / $MAX_PER_SECOND)) seconds"

SUCCESSFUL=0
FAILED=0

while IFS= read -r phone; do
    # Skip empty lines or comments
    if [[ -z "$phone" || "$phone" =~ ^# ]]; then
        continue
    fi
    
    echo "Sending to $phone..."
    
    # Build command
    CMD="dotnet run -- send --to \"$phone\" --body \"$MESSAGE_BODY\""
    if [ ! -z "$MEDIA_URL" ]; then
        CMD="$CMD --media \"$MEDIA_URL\""
    fi
    
    # Send message
    if eval $CMD > /dev/null 2>&1; then
        echo "✅ Sent to $phone"
        ((SUCCESSFUL++))
    else
        echo "❌ Failed to send to $phone"
        ((FAILED++))
        
        # Log failure for retry later
        echo "$phone" >> failed-sends.txt
    fi
    
    # Rate limiting delay
    sleep $((1 / $MAX_PER_SECOND))
    
done < "$PHONE_LIST_FILE"

echo "================================================"
echo "📊 Bulk send completed!"
echo "✅ Successful: $SUCCESSFUL"
echo "❌ Failed: $FAILED"

if [ $FAILED -gt 0 ]; then
    echo "📝 Failed phone numbers saved to: failed-sends.txt"
    echo "🔄 Retry with: $0 failed-sends.txt \"$MESSAGE_BODY\" \"$MEDIA_URL\""
fi
```

**Phone list file example (customers.txt):**
```
# Customer notification list
+15551234567
+15551234568  
+15551234569
# +15551234570 - opted out
+15551234571
```

**Usage:**
```bash
./bulk-send-with-limits.sh customers.txt "System maintenance tonight 11 PM - 1 AM EST" "https://status.yourcompany.com/maintenance-details.png"
```

---

## Advanced Integration Patterns

### Example 17: Message Queue Integration

**Scenario**: Use a message queue to ensure reliable message delivery even if MmsRelay is temporarily down.

```python
# message_queue_sender.py
import json
import redis
import requests
import time
import logging
from typing import Dict, Any

class QueuedMmsSender:
    def __init__(self, redis_url: str, mmsrelay_url: str):
        self.redis_client = redis.from_url(redis_url)
        self.mmsrelay_url = mmsrelay_url.rstrip('/')
        self.queue_name = "mms_queue"
        self.retry_queue = "mms_retry_queue"
        
    def queue_message(self, to: str, body: str = None, media_urls: list = None, priority: int = 0):
        """Queue a message for sending."""
        message = {
            "to": to,
            "body": body,
            "media_urls": media_urls,
            "attempts": 0,
            "max_attempts": 3,
            "queued_at": time.time()
        }
        
        # Higher priority = lower score (sent first)
        self.redis_client.zadd(self.queue_name, {json.dumps(message): -priority})
        logging.info(f"Queued MMS for {to}")
    
    def process_queue(self):
        """Process messages from the queue."""
        while True:
            try:
                # Get next message (lowest score = highest priority)
                result = self.redis_client.zpopmin(self.queue_name, count=1)
                
                if not result:
                    time.sleep(1)  # No messages, wait
                    continue
                    
                message_json, score = result[0]
                message = json.loads(message_json)
                
                if self._send_message(message):
                    logging.info(f"✅ Sent MMS to {message['to']}")
                else:
                    self._handle_failed_message(message)
                    
                time.sleep(1)  # Rate limiting
                
            except Exception as e:
                logging.error(f"Queue processing error: {e}")
                time.sleep(5)
    
    def _send_message(self, message: Dict[str, Any]) -> bool:
        """Attempt to send a message via MmsRelay."""
        try:
            payload = {"to": message["to"]}
            if message.get("body"):
                payload["body"] = message["body"]
            if message.get("media_urls"):
                payload["mediaUrls"] = message["media_urls"]
                
            response = requests.post(f"{self.mmsrelay_url}/mms", json=payload, timeout=30)
            return response.status_code == 202
            
        except Exception as e:
            logging.error(f"Send error for {message['to']}: {e}")
            return False
    
    def _handle_failed_message(self, message: Dict[str, Any]):
        """Handle a failed message - retry or dead letter."""
        message["attempts"] += 1
        
        if message["attempts"] < message["max_attempts"]:
            # Exponential backoff: retry later
            delay = 2 ** message["attempts"]  # 2, 4, 8 seconds
            retry_time = time.time() + delay
            
            self.redis_client.zadd(self.retry_queue, {json.dumps(message): retry_time})
            logging.warning(f"Retrying {message['to']} in {delay} seconds (attempt {message['attempts']})")
        else:
            # Dead letter - manual intervention needed
            self.redis_client.lpush("mms_dead_letter", json.dumps(message))
            logging.error(f"❌ Dead lettered message for {message['to']} after {message['attempts']} attempts")

# Usage
if __name__ == "__main__":
    sender = QueuedMmsSender("redis://localhost:6379", "https://mmsrelay.yourcompany.com")
    
    # Queue some messages
    sender.queue_message("+15551234567", "High priority alert", priority=10)
    sender.queue_message("+15551234568", "Normal notification", priority=1)
    
    # Process the queue (would run as a background service)
    sender.process_queue()
```

---

These examples show real-world patterns for integrating MmsRelay into your applications. Each example includes error handling, rate limiting, and logging - essential for production use.

For more integration help, see:
- [FAQ.md](FAQ.md) - Common questions and troubleshooting
- [GLOSSARY.md](GLOSSARY.md) - Technical terms explained
- Individual project documentation in each folder