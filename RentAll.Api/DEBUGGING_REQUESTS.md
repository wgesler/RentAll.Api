# Debugging Inbound Requests - Guide

## What Was Added

### 1. Request Logging Middleware
**File**: `Middleware/RequestLoggingMiddleware.cs`

This middleware captures **ALL** incoming requests to `/property` endpoints (POST/PUT/PATCH) and logs:
- HTTP Method
- Request Path
- Content-Type header
- **Raw request body** (exactly as received)
- **Formatted JSON** (if valid JSON)

### 2. Enhanced Controller Logging
**File**: `Controllers/PropertyController.Post.cs`

The controller now logs:
- Whether DTO is null
- **The bound DTO object** (what ASP.NET Core successfully bound)
- **Detailed model binding errors** including:
  - Field name
  - Attempted value (what was sent)
  - Error messages
  - Exception details

## How to Use

### Step 1: Check Application Logs

When you make a request, check your application logs (console output or log file). You'll see:

```
=== INCOMING REQUEST ===
Method: POST
Path: /property
Content-Type: application/json
Raw Body: {"propertyCode":"BAR904","contactId":"e284b433-eeac-4eaa-ae19-20f52560afc4",...}
Formatted JSON:
{
  "propertyCode": "BAR904",
  "contactId": "e284b433-eeac-4eaa-ae19-20f52560afc4",
  ...
}
```

### Step 2: Check Model Binding Errors

If model binding fails, you'll see:

```
=== MODEL BINDING ERRORS ===
[
  {
    "field": "minStay",
    "attemptedValue": "30",
    "errors": [
      {
        "errorMessage": "The value '30' is not valid for MinStay.",
        "exception": "Input string was not in a correct format."
      }
    ]
  }
]
```

### Step 3: Check the HTTP Response

The API now returns detailed error information in the response body:

```json
{
  "message": "Invalid request data - model binding failed",
  "errors": [
    {
      "field": "minStay",
      "attemptedValue": "30",
      "errors": [
        {
          "errorMessage": "The value '30' is not valid for MinStay.",
          "exception": "Input string was not in a correct format."
        }
      ]
    }
  ],
  "modelState": [
    {
      "key": "minStay",
      "attemptedValue": "30",
      "rawValue": ["30"],
      "errorCount": 1
    }
  ]
}
```

## Common Issues You'll Find

### 1. Type Mismatches
**Log shows**: `attemptedValue: "30"` for an `int` field
**Problem**: UI sent string `"30"` instead of number `30`
**Fix**: Convert to number in UI before sending

### 2. Missing Required Fields
**Log shows**: Field not in request body
**Problem**: UI didn't send required field
**Fix**: Add field to request payload

### 3. Invalid JSON
**Log shows**: "Failed to parse JSON"
**Problem**: Malformed JSON in request
**Fix**: Check JSON syntax in UI

### 4. Null Values for Non-Nullable Fields
**Log shows**: `attemptedValue: null` for non-nullable field
**Problem**: UI sent null for required field
**Fix**: Send empty string or valid value

## Quick Debugging Checklist

1. ✅ **Check Raw Body** - See exactly what UI sent
2. ✅ **Check Formatted JSON** - Verify JSON structure
3. ✅ **Check Model Binding Errors** - See which fields failed
4. ✅ **Check Attempted Values** - See what value was sent for each field
5. ✅ **Check Bound DTO** - See what ASP.NET Core successfully bound

## Example: Finding a Type Mismatch

**In Logs:**
```
=== MODEL BINDING ERRORS ===
{
  "field": "accomodates",
  "attemptedValue": "2",  ← This is a STRING
  "errors": [
    {
      "errorMessage": "The value '2' is not valid for Accomodates.",
      "exception": "Input string was not in a correct format."
    }
  ]
}
```

**Problem**: UI sent `"accomodates": "2"` (string) but API expects `int`

**Fix in UI**: Change to `accomodates: 2` (number)

## Disabling Logging (Production)

To disable detailed logging in production, you can:

1. **Remove middleware** (comment out in Program.cs)
2. **Use log levels** - Set logging level to Warning or Error
3. **Conditional logging** - Only log in Development environment

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<RequestLoggingMiddleware>();
}
```

## Tips

- **Always check logs first** - They show the raw request
- **Compare Raw Body with Bound DTO** - See what changed during binding
- **Look for attemptedValue** - Shows exactly what was sent
- **Check exception messages** - Often tells you the exact problem

