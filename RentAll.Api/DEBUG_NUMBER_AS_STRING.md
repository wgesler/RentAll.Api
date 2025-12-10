# Why Numbers Come Across as Strings - Debugging Guide

## Common Causes in JavaScript/TypeScript

### 1. **HTML Form Input Fields**
HTML input elements **always return strings**, even for number inputs:

```javascript
// ❌ WRONG - This will be a string
const minStay = document.getElementById('minStay').value; // Returns "30" (string)

// ✅ CORRECT - Convert to number
const minStay = parseInt(document.getElementById('minStay').value, 10);
// OR
const minStay = Number(document.getElementById('minStay').value);
// OR
const minStay = +document.getElementById('minStay').value;
```

### 2. **FormData Serialization**
If you're using `FormData`, all values become strings:

```javascript
// ❌ WRONG
const formData = new FormData();
formData.append('minStay', 30); // This becomes "30" (string)
formData.append('accomodates', 2); // This becomes "2" (string)

// ✅ CORRECT - Use JSON instead
const data = {
  minStay: Number(formData.get('minStay')),
  accomodates: Number(formData.get('accomodates'))
};
```

### 3. **String Concatenation Before JSON**
If you concatenate with strings, JavaScript converts numbers to strings:

```javascript
// ❌ WRONG
const data = {
  minStay: "30", // Accidentally quoted
  accomodates: "2" // Accidentally quoted
};

// ❌ WRONG - String concatenation
const minStay = someStringValue + 30; // Results in string

// ✅ CORRECT
const data = {
  minStay: 30, // Number
  accomodates: 2 // Number
};
```

### 4. **TypeScript Type Definitions**
If your TypeScript interface allows `string | number`, TypeScript won't catch the error:

```typescript
// ❌ WRONG - Too permissive
interface PropertyDto {
  minStay: string | number; // Allows both
  accomodates: string | number;
}

// ✅ CORRECT - Strict typing
interface PropertyDto {
  minStay: number; // Only numbers
  accomodates: number;
}
```

### 5. **API Client Library Issues**
Some HTTP client libraries convert everything to strings:

```javascript
// ❌ WRONG - Some libraries do this
axios.post('/property', {
  minStay: "30", // Library converts to string
  accomodates: "2"
});

// ✅ CORRECT - Ensure proper serialization
axios.post('/property', {
  minStay: Number(30),
  accomodates: Number(2)
}, {
  headers: { 'Content-Type': 'application/json' }
});
```

### 6. **JSON.parse/JSON.stringify Issues**
If you're manually parsing/stringifying:

```javascript
// ❌ WRONG - If source data is already strings
const jsonString = '{"minStay": "30", "accomodates": "2"}';
const data = JSON.parse(jsonString); // Numbers are strings

// ✅ CORRECT - Ensure source JSON has numbers
const jsonString = '{"minStay": 30, "accomodates": 2}';
const data = JSON.parse(jsonString); // Numbers are numbers
```

### 7. **State Management (React/Vue/etc)**
If state is initialized from form inputs:

```javascript
// ❌ WRONG - Form state is strings
const [formData, setFormData] = useState({
  minStay: '', // String
  accomodates: '' // String
});

// ✅ CORRECT - Convert on submit
const handleSubmit = () => {
  const payload = {
    minStay: Number(formData.minStay),
    accomodates: Number(formData.accomodates)
  };
};
```

## How to Debug in Your UI

### Check 1: Inspect the Actual Payload
```javascript
// Before sending request
console.log('Payload:', JSON.stringify(payload, null, 2));
console.log('minStay type:', typeof payload.minStay);
console.log('accomodates type:', typeof payload.accomodates);
```

### Check 2: Verify in Network Tab
1. Open browser DevTools → Network tab
2. Find the POST request to `/property`
3. Click on it → Payload tab
4. Look for quotes around numbers:
   - ❌ `"minStay": "30"` (string - has quotes)
   - ✅ `"minStay": 30` (number - no quotes)

### Check 3: Type Checking
```javascript
// Add validation before sending
const validatePayload = (data) => {
  const errors = [];
  
  if (typeof data.minStay !== 'number') {
    errors.push('minStay must be a number, got: ' + typeof data.minStay);
  }
  
  if (typeof data.accomodates !== 'number') {
    errors.push('accomodates must be a number, got: ' + typeof data.accomodates);
  }
  
  if (errors.length > 0) {
    console.error('Validation errors:', errors);
    return false;
  }
  
  return true;
};

// Use before sending
if (validatePayload(payload)) {
  // Send request
}
```

## Quick Fix Solutions

### Solution 1: Convert on Submit
```javascript
const submitProperty = (formData) => {
  const payload = {
    ...formData,
    minStay: Number(formData.minStay) || 0,
    accomodates: Number(formData.accomodates) || 0,
    // Remove amount and amountTypeId if not needed
    // amount: undefined,
    // amountTypeId: undefined
  };
  
  // Remove undefined fields
  Object.keys(payload).forEach(key => {
    if (payload[key] === undefined) {
      delete payload[key];
    }
  });
  
  return api.post('/property', payload);
};
```

### Solution 2: Use a Serializer
```javascript
const serializeProperty = (data) => {
  return {
    ...data,
    minStay: parseInt(data.minStay, 10),
    maxStay: parseInt(data.maxStay, 10),
    accomodates: parseInt(data.accomodates, 10),
    squareFeet: parseInt(data.squareFeet, 10),
    monthlyRate: parseFloat(data.monthlyRate),
    dailyRate: parseFloat(data.dailyRate),
    bathrooms: parseFloat(data.bathrooms),
    // Remove fields not in API
    amount: undefined,
    amountTypeId: undefined
  };
};
```

### Solution 3: TypeScript Strict Typing
```typescript
interface CreatePropertyRequest {
  minStay: number; // Strict - no string allowed
  accomodates: number; // Strict - no string allowed
  // ... other fields
}

// TypeScript will catch errors at compile time
const payload: CreatePropertyRequest = {
  minStay: formData.minStay, // Error if formData.minStay is string
  accomodates: formData.accomodates
};
```

## Most Likely Cause in Your Case

Based on your payload showing `"2"` and `"30"` as strings, the most likely causes are:

1. **Form inputs** - HTML inputs return strings
2. **State management** - Form state initialized as strings
3. **Missing type conversion** - Not converting strings to numbers before sending

## Recommended Fix

Add a conversion function before sending the request:

```javascript
const preparePropertyPayload = (formData) => {
  // Convert string numbers to actual numbers
  const numericFields = ['minStay', 'maxStay', 'accomodates', 'squareFeet', 
                        'bedrooms', 'propertyStyle', 'propertyType', 
                        'propertyStatus', 'trashPickupId'];
  
  const decimalFields = ['monthlyRate', 'dailyRate', 'bathrooms'];
  
  const payload = { ...formData };
  
  // Convert integer fields
  numericFields.forEach(field => {
    if (payload[field] !== null && payload[field] !== undefined) {
      payload[field] = parseInt(payload[field], 10);
    }
  });
  
  // Convert decimal fields
  decimalFields.forEach(field => {
    if (payload[field] !== null && payload[field] !== undefined) {
      payload[field] = parseFloat(payload[field]);
    }
  });
  
  // Remove fields not in API
  delete payload.amount;
  delete payload.amountTypeId;
  
  return payload;
};
```



