# Property Create API Reference

## Endpoint
**POST** `/property`

## Authentication
Requires JWT Bearer token in Authorization header:
```
Authorization: Bearer <token>
```

## Request Body Structure

The API expects a JSON object matching the `CreatePropertyDto` structure. Here are the **exact field names and types**:

### Important Field Name Notes:
⚠️ **CRITICAL**: The API uses `AvailableFrom` and `A vailableUntil` (with typo - missing 'v'). The UI must send these exact names, NOT `AvailableFrom`/`AvailableUntil`.

### Field Reference

```json
{
  "propertyCode": "string (required)",
  "contactId": "Guid (required, cannot be empty)",
  "isActive": "bool",
  
  // Availability Section - NOTE THE TYPO IN FIELD NAMES
  "AvailableFrom": "DateTimeOffset? (nullable)",
  "a vailableUntil": "DateTimeOffset? (nullable)",
  "minStay": "int",
  "maxStay": "int",
  "checkInTime": "string",
  "checkOutTime": "string",
  "MonthlyRate": "decimal",
  "dailyRate": "decimal",
  "propertyStyle": "int",
  "propertyType": "int",
  "propertyStatus": "int",
  "bedrooms": "int",
  "bathrooms": "decimal",
  "accomodates": "int",
  "squareFeet": "int",
  "bedSizes": "string",
  
  // Address Section
  "address1": "string (required)",
  "address2": "string? (nullable)",
  "suite": "string",
  "city": "string (required)",
  "state": "string (required)",
  "zip": "string (required)",
  "phone": "string? (nullable)",
  "neighborhood": "string? (nullable)",
  "crossStreet": "string? (nullable)",
  "view": "string",
  "mailbox": "string",
  
  // Features & Security Section
  "furnished": "bool",
  "heating": "bool",
  "ac": "bool",
  "elevator": "bool",
  "security": "bool",
  "gated": "bool",
  "petsAllowed": "bool",
  "smoking": "bool",
  "assignedParking": "bool",
  "notes": "string",
  "alarm": "bool",
  "alarmCode": "string",
  "remoteAccess": "bool",
  "keyCode": "string",
  
  // Kitchen & Bath
  "kitchen": "bool",
  "oven": "bool",
  "refrigerator": "bool",
  "microwave": "bool",
  "dishwasher": "bool",
  "bathtub": "bool",
  "washerDryer": "string? (nullable) - NOTE: This is a STRING, not boolean!",
  "sofabeds": "bool",
  
  // Electronics Section
  "tv": "bool",
  "cable": "bool",
  "dvd": "bool",
  "fastInternet": "bool",
  
  // Outdoor Spaces Section
  "deck": "bool",
  "patio": "bool",
  "yard": "bool",
  "garden": "bool",
  
  // Pool & Spa Section
  "commonPool": "bool",
  "privatePool": "bool",
  "jacuzzi": "bool",
  "sauna": "bool",
  "gym": "bool",
  
  // Trash Section
  "trashPickupId": "int",
  "trashRemoval": "string",
  
  // Additional Amenities
  "amenities": "string"
}
```

## Critical Issues to Check in UI:

### 1. Field Name Mismatches
- ❌ **WRONG**: `availableFrom`, `availableUntil`
- ✅ **CORRECT**: `AvailableFrom`, `a vailableUntil` (note the typo)

### 2. Type Mismatches
- ❌ **WRONG**: `washerDryer: true` (boolean)
- ✅ **CORRECT**: `washerDryer: "YES"` or `washerDryer: "NO"` (string)

### 3. Required Fields
The API validates these fields are present and not empty:
- `propertyCode` (string, not null/empty)
- `contactId` (Guid, not empty)
- `address1` (string, not null/empty)
- `city` (string, not null/empty)
- `state` (string, not null/empty)
- `zip` (string, not null/empty)

### 4. JSON Property Naming
ASP.NET Core uses camelCase by default for JSON serialization. The API expects:
- `propertyCode` (not `PropertyCode`)
- `contactId` (not `ContactId`)
- `AvailableFrom` (not `AvailableFrom` or `AvailableFrom`)

## Common UI Issues:

1. **Field name typos**: Using `AvailableFrom` instead of `AvailableFrom`
2. **Type mismatches**: Sending `washerDryer` as boolean instead of string
3. **Missing required fields**: Not sending all required fields
4. **JSON casing**: Using PascalCase instead of camelCase
5. **Null vs empty string**: Some fields expect empty string `""` not `null`

## Testing with cURL:

```bash
curl -X POST https://your-api-url/property \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "propertyCode": "TEST001",
    "contactId": "00000000-0000-0000-0000-000000000000",
    "isActive": true,
    "AvailableFrom": null,
    "a vailableUntil": null,
    "address1": "123 Main St",
    "city": "Test City",
    "state": "CA",
    "zip": "12345",
    "washerDryer": "NO"
  }'
```

## Debugging Tips:

1. **Check browser Network tab**: Look at the exact JSON being sent
2. **Verify field names**: Compare UI field names with this reference
3. **Check Content-Type header**: Must be `application/json`
4. **Verify Authorization header**: Must include valid JWT token
5. **Check CORS**: Ensure UI origin is in `AllowedHostNames` in appsettings.json



