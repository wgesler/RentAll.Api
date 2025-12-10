# UI to API Field Mapping Reference

## How ASP.NET Core JSON Serialization Works

By default, ASP.NET Core converts C# PascalCase property names to **camelCase** in JSON.

**C# Property Name** → **JSON Field Name**
- `PropertyCode` → `propertyCode`
- `ContactId` → `contactId`
- `AvailableFrom` → `availableFrom`
- `WasherDryer` → `washerDryer`

## Complete Field Mapping

Here's what the UI should send (camelCase JSON) vs what the API expects:

| UI JSON Field Name | C# Property Name | Type | Notes |
|-------------------|-----------------|------|-------|
| `propertyCode` | `PropertyCode` | `string` | **Required**, cannot be null/empty |
| `contactId` | `ContactId` | `Guid` | **Required**, cannot be empty Guid |
| `isActive` | `IsActive` | `bool` | |
| `availableFrom` | `AvailableFrom` | `DateTimeOffset?` | Nullable |
| `availableUntil` | `AvailableUntil` | `DateTimeOffset?` | Nullable |
| `minStay` | `MinStay` | `int` | |
| `maxStay` | `MaxStay` | `int` | |
| `checkInTime` | `CheckInTime` | `int` | Maps to enum value |
| `checkOutTime` | `CheckOutTime` | `int` | Maps to enum value |
| `monthlyRate` | `MonthlyRate` | `decimal` | ⚠️ Note: "Monthly" not "Montly" |
| `dailyRate` | `DailyRate` | `decimal` | |
| `propertyStyle` | `PropertyStyle` | `int` | |
| `propertyType` | `PropertyType` | `int` | |
| `propertyStatus` | `PropertyStatus` | `int` | |
| `bedrooms` | `Bedrooms` | `int` | |
| `bathrooms` | `Bathrooms` | `decimal` | |
| `accomodates` | `Accomodates` | `int` | |
| `squareFeet` | `SquareFeet` | `int` | |
| `bedSizes` | `BedSizes` | `string` | Can be empty string |
| `address1` | `Address1` | `string` | **Required**, cannot be null/empty |
| `address2` | `Address2` | `string?` | Nullable |
| `suite` | `Suite` | `string` | Can be empty string |
| `city` | `City` | `string` | **Required**, cannot be null/empty |
| `state` | `State` | `string` | **Required**, cannot be null/empty |
| `zip` | `Zip` | `string` | **Required**, cannot be null/empty |
| `phone` | `Phone` | `string?` | Nullable |
| `neighborhood` | `Neighborhood` | `string?` | Nullable |
| `crossStreet` | `CrossStreet` | `string?` | Nullable |
| `view` | `View` | `string` | Can be empty string |
| `mailbox` | `Mailbox` | `string` | Can be empty string |
| `furnished` | `Furnished` | `bool` | |
| `heating` | `Heating` | `bool` | |
| `ac` | `AC` | `bool` | ⚠️ Note: "ac" not "aC" |
| `elevator` | `Elevator` | `bool` | |
| `security` | `Security` | `bool` | |
| `gated` | `Gated` | `bool` | |
| `petsAllowed` | `PetsAllowed` | `bool` | |
| `smoking` | `Smoking` | `bool` | |
| `assignedParking` | `AssignedParking` | `bool` | |
| `notes` | `Notes` | `string` | Can be empty string |
| `alarm` | `Alarm` | `bool` | |
| `alarmCode` | `AlarmCode` | `string` | Can be empty string |
| `remoteAccess` | `RemoteAccess` | `bool` | |
| `keyCode` | `KeyCode` | `string` | Can be empty string |
| `kitchen` | `Kitchen` | `bool` | |
| `oven` | `Oven` | `bool` | |
| `refrigerator` | `Refrigerator` | `bool` | |
| `microwave` | `Microwave` | `bool` | |
| `dishwasher` | `Dishwasher` | `bool` | |
| `bathtub` | `Bathtub` | `bool` | |
| `washerDryer` | `WasherDryer` | `bool` | ⚠️ **This is a BOOLEAN, not a string!** |
| `sofabeds` | `Sofabeds` | `bool` | |
| `tv` | `TV` | `bool` | ⚠️ Note: "tv" not "tV" |
| `cable` | `Cable` | `bool` | |
| `dvd` | `Dvd` | `bool` | ⚠️ Note: "dvd" not "dVD" |
| `fastInternet` | `FastInternet` | `bool` | |
| `deck` | `Deck` | `bool` | |
| `patio` | `Patio` | `bool` | |
| `yard` | `Yard` | `bool` | |
| `garden` | `Garden` | `bool` | |
| `commonPool` | `CommonPool` | `bool` | |
| `privatePool` | `PrivatePool` | `bool` | |
| `jacuzzi` | `Jacuzzi` | `bool` | |
| `sauna` | `Sauna` | `bool` | |
| `gym` | `Gym` | `bool` | |
| `trashPickupId` | `TrashPickupId` | `int` | |
| `trashRemoval` | `TrashRemoval` | `string` | Can be empty string |
| `amenities` | `Amenities` | `string` | Can be empty string |

## Common Mistakes to Avoid

### ❌ WRONG Field Names:
- `PropertyCode` (PascalCase) → Should be `propertyCode`
- `AvailableFrom` (PascalCase) → Should be `availableFrom`
- `MontlyRate` (typo) → Should be `monthlyRate`
- `AbailableFrom` (typo) → Should be `availableFrom`

### ❌ WRONG Types:
- `washerDryer: "YES"` (string) → Should be `washerDryer: true` (boolean)
- `washerDryer: "NO"` (string) → Should be `washerDryer: false` (boolean)

### ✅ CORRECT Examples:

```json
{
  "propertyCode": "TEST001",
  "contactId": "123e4567-e89b-12d3-a456-426614174000",
  "isActive": true,
  "availableFrom": null,
  "availableUntil": null,
  "monthlyRate": 1500.00,
  "washerDryer": true,
  "address1": "123 Main St",
  "city": "Test City",
  "state": "CA",
  "zip": "12345"
}
```

## Special Cases

1. **Acronyms**: `AC`, `TV`, `Dvd` become `ac`, `tv`, `dvd` in JSON (all lowercase)
2. **Nullable strings**: Can send `null` or empty string `""`
3. **Boolean fields**: Must be `true` or `false`, not strings
4. **Required fields**: Must be present and non-empty

## Debugging Checklist

When the request fails, check:

1. ✅ All field names are **camelCase** (not PascalCase)
2. ✅ `washerDryer` is a **boolean** (not a string)
3. ✅ Required fields are present and non-empty
4. ✅ `monthlyRate` is spelled correctly (not `montlyRate`)
5. ✅ `availableFrom`/`availableUntil` are spelled correctly (not `abailableFrom`)
6. ✅ Content-Type header is `application/json`
7. ✅ Authorization header includes valid JWT token



