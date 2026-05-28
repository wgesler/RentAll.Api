# RentAll External API

## Overview

- **Host:** `https://api.rentallexchange.com`
- **OrganizationId (AvenueWest):** `280CD8DA-F1BE-41F2-AE6E-B45008CF3896`
- **Method (all endpoints):** `POST`
- **Header (all endpoints):**
  - `Content-Type: application/json`
  - `X-Api-Key: <endpoint key>`

## Authentication

Use the endpoint-specific API key in the `X-Api-Key` header.

- **Rental Lead key:** `OpTCmPTl9GzAcis2Yn7XXn8m4QhkQrBMBAt8DE+LzC+N4BmKJu17pMyxC0XTNwtR`
- **Owner/General/Ticket key:** `ILOH9vjA6NiYYM4B5xfL/akO65AxASrtuoP8gv2KOTnl5ggfNyDNvEB2Mwm3YuG0Ox4OyU+uYcYz7+mF1sd1pw`

---

## 1) External Rental Lead Intake API (v1)

- **URL:** `/api/leads/external/rentals`

### Required fields

- `organizationId` (string, GUID)
- `officeId` (integer, must be > 0)
- `firstName` (string, non-empty)
- `lastName` (string, non-empty)
- `email` (string, valid email format)
- `phone` (string, non-empty)

### Optional fields

- `desiredLocation` (string | null)
- `propertyRefId` (string | null)
- `estimatedArrivalDate` (string | null)
- `estimatedDepartureDate` (string | null)
- `maxMonthlyBudget` (number | null, if provided must be >= 0)
- `minBedrooms` (integer | null, if provided must be >= 0)
- `numberOfOccupants` (string | null)
- `whatBringsYouToTown` (string | null)
- `howDidYouFindUs` (string | null)
- `tellUsMoreAboutHowYouFoundUs` (string | null)
- `petFriendly` (boolean | null)
- `decisionDate` (string date `YYYY-MM-DD` | null)
- `organizationName` (string | null)
- `additionalInformation` (string | null)
- `iNeedAsap` (boolean, defaults to `false` if omitted)
- `emailPhoneConsent` (boolean, defaults to `false` if omitted)
- `smsConsent` (boolean, defaults to `false` if omitted)

### Example request

```json
{
  "organizationId": "11111111-1111-1111-1111-111111111111",
  "officeId": 1,
  "firstName": "John",
  "lastName": "Renter",
  "email": "john.renter@example.com",
  "phone": "555-333-4444",
  "desiredLocation": "Downtown",
  "maxMonthlyBudget": 2500,
  "minBedrooms": 2,
  "emailPhoneConsent": true,
  "smsConsent": true
}
```

---

## 2) External Owner Lead Intake API (v1)

- **URL:** `/api/leads/external/owners`

### Required fields

- `organizationId` (string, GUID)
- `officeId` (integer, must be > 0)
- `firstName` (string, non-empty)
- `lastName` (string, non-empty)
- `email` (string, valid email format)
- `phone` (string, non-empty)

### Optional fields

- `locationOfProperty` (string | null)
- `programInterest` (string | null)
- `whatIsPromptingContact` (string | null)
- `timeFrame` (boolean | null)
- `targetRentReadyDate` (string date `YYYY-MM-DD` | null)
- `propertyGoals` (string | null)
- `tellUsMoreAboutYourGoals` (string | null)
- `yearsOfExperienceWithRentals` (integer | null, if provided must be >= 0)
- `tellUsMoreAboutProperty` (string | null)
- `address` (string | null)
- `city` (string | null)
- `state` (string | null)
- `zip` (string | null)
- `numberOfBeds` (string | null)
- `numberOfBaths` (string | null)
- `approxSqFootage` (string | null)
- `typeOfProperty` (string | null)
- `tellUsWhatYouLikeMostAboutYourProperty` (string | null)
- `tellUsAnyDrawbacks` (string | null)
- `preferredContactMethod` (string | null)
- `timeDateForContact` (string | null)
- `emailPhoneConsent` (boolean, defaults to `false` if omitted)
- `smsConsent` (boolean, defaults to `false` if omitted)

### Example request

```json
{
  "organizationId": "11111111-1111-1111-1111-111111111111",
  "officeId": 1,
  "firstName": "Olivia",
  "lastName": "Owner",
  "email": "olivia.owner@example.com",
  "phone": "555-777-8888",
  "locationOfProperty": "Austin, TX",
  "yearsOfExperienceWithRentals": 3,
  "emailPhoneConsent": true,
  "smsConsent": true
}
```

---

## 3) External General Lead Intake API (v1)

- **URL:** `/api/leads/external/general`

### Required fields

- `organizationId` (string, GUID)
- `officeId` (integer, must be > 0)
- `firstName` (string, non-empty)
- `lastName` (string, non-empty)
- `email` (string, valid email format)
- `phone` (string, non-empty)
- `message` (string, non-empty)

### Optional fields

- None

### Example request

```json
{
  "organizationId": "11111111-1111-1111-1111-111111111111",
  "officeId": 1,
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phone": "555-111-2222",
  "message": "Interested in learning more."
}
```

---

## 4) External Ticket Intake API (v1)

- **URL:** `/api/ticket/external`

### Required fields

- `organizationId` (string, GUID)
- `officeId` (integer, must be > 0)
- `firstName` (string, non-empty)
- `lastName` (string, non-empty)
- `location` (string, non-empty)
- `email` (string, non-empty)
- `phone` (string, non-empty)
- `address` (string, non-empty)
- `hasPermissionToEnter` (boolean)
- `issueDescription` (string, non-empty)
- `communicationConsent` (boolean)

### Optional fields

- `smsConsent` (boolean | null)

### Example request

```json
{
  "organizationId": "11111111-1111-1111-1111-111111111111",
  "officeId": 1,
  "firstName": "Tina",
  "lastName": "Tenant",
  "location": "Unit 204",
  "email": "tina.tenant@example.com",
  "phone": "555-999-0000",
  "address": "123 Main St, Austin, TX 78701",
  "hasPermissionToEnter": true,
  "issueDescription": "AC is not cooling.",
  "communicationConsent": true,
  "smsConsent": true
}
```

