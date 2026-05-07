# Complete Visitor Management Workflow

## 🎯 Your System - Fully Automated

```
Register Visitor
       ↓
Generate QR Code
       ↓
Scan at Entry → Check-In (Status: Pending → Active)
       ↓
Scan at Exit → Check-Out (Status: Active → CheckedOut)
       ↓
Status Updated Automatically
```

---

## 📋 Step-by-Step Workflow

### Step 1: Register Visitor
**Location**: `/Visitor/Add` or Dashboard → Quick Check-In

**Admin Actions:**
1. Navigate to Visitors → Add Visitor
2. Fill in visitor details:
   - Name (required)
   - Phone (required)
   - Email
   - Company Name
   - Whom to Meet
   - Purpose of Visit
   - Department
   - Vehicle Number
   - Upload Photo (optional)
3. Click "Add Visitor"

**System Actions:**
- Creates visitor record in database
- Visitor ID assigned automatically
- Status: Ready for QR generation
- No QR code yet (generated in next step)

**Database:**
```sql
INSERT INTO Visitors (Name, Phone, Email, CompanyName, WhomeToMeet, PurposeOfVisit, ...)
VALUES ('John Doe', '1234567890', 'john@company.com', 'ABC Corp', 'Jane Smith', 'Meeting', ...)
```

---

### Step 2: Generate QR Code
**Location**: `/Visitor/GenerateQR/{visitorId}`

**Admin Actions:**
1. Navigate to Visitors → Select Visitor → Generate QR
2. Click "Generate QR" button
3. Download or Print QR code
4. Give to visitor (printed pass or send via email)

**System Actions:**
- Checks if visitor already has permanent QR code
- If not, generates unique pass number: `VIS-XXXXXXXXXX`
- Stores QR code in `Visitor.QRCode` field
- Creates new Visit record with status "Pending"
- Generates QR code image (via API or external service)

**API Call:**
```javascript
POST /api/qci/generate/{visitorId}
Response: {
  visitId: 123,
  passNumber: "VIS-ABC1234567",
  name: "John Doe",
  mobile: "1234567890",
  isReused: true
}
```

**Database:**
```sql
-- Update visitor with permanent QR
UPDATE Visitors SET QRCode = 'VIS-ABC1234567' WHERE VisitorId = 5

-- Create visit record
INSERT INTO Visits (VisitorId, EmployeeId, Purpose, Status, PassNumber, CheckInTime, VisitDate)
VALUES (5, 10, 'Meeting', 'Pending', 'VIS-ABC1234567', GETDATE(), GETDATE())
```

**QR Code Contains:**
- Pass Number: `VIS-ABC1234567`
- Encoded as standard QR code
- Can be scanned by any QR reader

---

### Step 3: Visitor Arrives - Scan at Entry (Check-In)
**Location**: `/Visit/ScanQR`

**Security Guard Actions:**
1. Open QR Scanner page
2. Click "Start Scanner" (camera opens)
3. Visitor shows QR code
4. Hold QR code in front of camera
5. **AUTOMATIC**: System detects and processes

**System Actions:**
1. Camera detects QR code
2. Extracts pass number: `VIS-ABC1234567`
3. Calls API: `POST /api/qci/scanpass`
4. Finds visit with matching pass number and status "Pending"
5. Updates visit status: `Pending → Active`
6. Records check-in time
7. Displays success message
8. Updates activity log

**API Call:**
```javascript
POST /api/qci/scanpass
Body: { passNumber: "VIS-ABC1234567" }

Response: {
  action: "checkin",
  visitId: 123,
  name: "John Doe",
  phone: "1234567890",
  company: "ABC Corp",
  host: "Jane Smith",
  department: "IT",
  purpose: "Meeting",
  passNumber: "VIS-ABC1234567",
  dateTime: "08 Apr 2026, 02:30 PM",
  status: "Active"
}
```

**Database:**
```sql
UPDATE Visits 
SET Status = 'Active', 
    CheckInTime = GETDATE(), 
    VisitDate = GETDATE()
WHERE PassNumber = 'VIS-ABC1234567' 
  AND Status = 'Pending'
```

**On Screen:**
```
✓ CHECK-IN SUCCESS: John Doe
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Visitor: John Doe
Phone: 1234567890
Company: ABC Corp
Host: Jane Smith
Purpose: Meeting
Time: 08 Apr 2026, 02:30 PM
Status: Active
Pass: VIS-ABC1234567
```

---

### Step 4: Visitor Leaves - Scan at Exit (Check-Out)
**Location**: `/Visit/ScanQR` (same scanner)

**Security Guard Actions:**
1. Scanner is still running (no need to restart)
2. Visitor shows SAME QR code
3. Hold QR code in front of camera
4. **AUTOMATIC**: System detects and processes

**System Actions:**
1. Camera detects QR code
2. Extracts pass number: `VIS-ABC1234567`
3. Calls API: `POST /api/qci/scanpass`
4. Finds visit with matching pass number and status "Active"
5. Updates visit status: `Active → CheckedOut`
6. Records check-out time
7. Calculates visit duration
8. Displays success message
9. Updates activity log

**API Call:**
```javascript
POST /api/qci/scanpass
Body: { passNumber: "VIS-ABC1234567" }

Response: {
  action: "checkout",
  visitId: 123,
  name: "John Doe",
  phone: "1234567890",
  company: "ABC Corp",
  host: "Jane Smith",
  department: "IT",
  purpose: "Meeting",
  passNumber: "VIS-ABC1234567",
  checkInTime: "08 Apr 2026, 02:30 PM",
  checkOutTime: "08 Apr 2026, 04:15 PM",
  duration: "1h 45m",
  dateTime: "08 Apr 2026, 04:15 PM",
  status: "CheckedOut"
}
```

**Database:**
```sql
UPDATE Visits 
SET Status = 'CheckedOut', 
    CheckOutTime = GETDATE()
WHERE PassNumber = 'VIS-ABC1234567' 
  AND Status = 'Active'
```

**On Screen:**
```
✓ CHECK-OUT SUCCESS: John Doe
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Visitor: John Doe
Phone: 1234567890
Company: ABC Corp
Host: Jane Smith
Purpose: Meeting
Check-In: 08 Apr 2026, 02:30 PM
Check-Out: 08 Apr 2026, 04:15 PM
Duration: 1h 45m
Status: CheckedOut
Pass: VIS-ABC1234567
```

---

## 🔄 Status Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    VISITOR LIFECYCLE                     │
└─────────────────────────────────────────────────────────┘

1. REGISTRATION
   ┌──────────────┐
   │   Visitor    │
   │  Registered  │
   └──────┬───────┘
          │
          ↓
2. QR GENERATION
   ┌──────────────┐
   │    Visit     │
   │   Created    │
   │ Status: PENDING │
   └──────┬───────┘
          │
          ↓
3. CHECK-IN (Scan at Entry)
   ┌──────────────┐
   │  QR Scanned  │
   │ Status: ACTIVE │
   │ Time Recorded │
   └──────┬───────┘
          │
          ↓
4. CHECK-OUT (Scan at Exit)
   ┌──────────────┐
   │  QR Scanned  │
   │Status:CHECKEDOUT│
   │Time & Duration│
   └──────────────┘
```

---

## 🎯 Automatic Status Updates

### Status Transitions:

| Current Status | Action | New Status | Trigger |
|---------------|--------|------------|---------|
| (none) | Generate QR | **Pending** | Admin clicks "Generate QR" |
| Pending | Scan at Entry | **Active** | QR scanned at entry |
| Active | Scan at Exit | **CheckedOut** | QR scanned at exit |
| CheckedOut | - | (Final) | Visit complete |

### What Gets Updated Automatically:

**On Check-In (Pending → Active):**
- ✅ Status changes to "Active"
- ✅ CheckInTime recorded
- ✅ VisitDate updated to current date
- ✅ Database saved automatically
- ✅ Dashboard stats updated
- ✅ Activity log updated

**On Check-Out (Active → CheckedOut):**
- ✅ Status changes to "CheckedOut"
- ✅ CheckOutTime recorded
- ✅ Duration calculated (CheckOutTime - CheckInTime)
- ✅ Database saved automatically
- ✅ Dashboard stats updated
- ✅ Activity log updated

---

## 📊 Real-Time Updates

### Dashboard Updates:
- Total Visits count
- Active Visits count (currently checked in)
- Pending Visits count (QR generated, not checked in)
- Completed Visits count (checked out)
- Today's Visits count

### Visit History Log:
- Shows all visits with current status
- Updates automatically when status changes
- Color-coded badges:
  - 🟡 Yellow: Pending
  - 🟢 Green: Active
  - ⚪ Gray: CheckedOut

---

## 🔐 Security Features

### Duplicate Prevention:
- 2-second cooldown between scans
- Prevents accidental double check-in/check-out
- Same QR code can't be processed twice within cooldown

### Validation:
- Only "Pending" visits can be checked in
- Only "Active" visits can be checked out
- Invalid QR codes are rejected
- Pass number must match exactly

### Audit Trail:
- All check-in/check-out times recorded
- Visit duration calculated
- Activity log maintained
- Full history preserved

---

## 📱 User Roles & Access

### Admin:
- Register visitors
- Generate QR codes
- View all visits
- Export reports
- Manage visitors/employees/departments

### Security Guard:
- Access QR scanner
- Check-in visitors (scan QR)
- Check-out visitors (scan QR)
- View recent activity
- Manual entry (if QR fails)

### Visitor:
- Receives QR code (printed or digital)
- Shows QR at entry (check-in)
- Shows QR at exit (check-out)
- No system access needed

---

## 🎉 Key Features

### ✅ Fully Automated:
- No manual status updates needed
- Automatic time recording
- Automatic duration calculation
- Real-time dashboard updates

### ✅ Fast & Efficient:
- QR scan takes < 1 second
- Instant check-in/check-out
- No typing required
- Continuous scanning mode

### ✅ Reliable:
- Works offline (after page load)
- Manual entry fallback
- Error handling
- Duplicate prevention

### ✅ User-Friendly:
- Simple 1-button operation
- Clear visual feedback
- Toast notifications
- Activity log

---

## 📈 Reports & Analytics

### Available Reports:
1. **Visit History**: All visits with status
2. **Day Report**: Visits by date
3. **CSV Export**: Download visit data
4. **Duration Analysis**: Average visit time
5. **Active Visitors**: Currently checked in

### Access Reports:
- Dashboard → Visitor History Log
- Visits → Report
- Visits → Export CSV

---

## 🚀 Quick Reference

### For Daily Operations:

**Morning:**
1. Open `/Visit/ScanQR`
2. Click "Start Scanner"
3. Leave running all day

**When Visitor Arrives:**
1. Visitor shows QR code
2. Hold in front of camera
3. Wait for "CHECK-IN SUCCESS"
4. Done!

**When Visitor Leaves:**
1. Visitor shows same QR code
2. Hold in front of camera
3. Wait for "CHECK-OUT SUCCESS"
4. Done!

**End of Day:**
1. Click "Stop Scanner"
2. Review activity log
3. Check dashboard stats

---

## 🎓 Training Checklist

### For Admins:
- [ ] Register new visitor
- [ ] Generate QR code
- [ ] Download/print QR code
- [ ] View visit history
- [ ] Export reports

### For Security Guards:
- [ ] Open QR scanner
- [ ] Start camera
- [ ] Scan QR for check-in
- [ ] Scan QR for check-out
- [ ] Use manual entry
- [ ] Read activity log

---

## ✨ System Benefits

1. **Speed**: Check-in/out in < 2 seconds
2. **Accuracy**: No manual data entry errors
3. **Tracking**: Complete audit trail
4. **Reporting**: Real-time analytics
5. **Scalability**: Handle multiple visitors simultaneously
6. **Compliance**: Automatic record keeping

---

**Your system is complete and production-ready!** 🎊

All components are working together seamlessly:
- ✅ Visitor Registration
- ✅ QR Code Generation
- ✅ Automatic Check-In
- ✅ Automatic Check-Out
- ✅ Status Updates
- ✅ Real-time Dashboard
- ✅ Activity Logging
- ✅ Reports & Analytics
