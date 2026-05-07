# Visitor Management System - Architecture

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    VISITOR MANAGEMENT SYSTEM                     │
│                         (VMS Project)                            │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Frontend   │───▶│   Backend    │───▶│   Database   │
│  (Razor/JS)  │◀───│ (ASP.NET MVC)│◀───│  (SQL Server)│
└──────────────┘    └──────────────┘    └──────────────┘
```

---

## 📊 Complete Workflow Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        STEP 1: REGISTRATION                      │
└─────────────────────────────────────────────────────────────────┘

Admin Dashboard (/Home/Dashboard)
        │
        ├─→ Quick Check-In Form
        │   └─→ POST /api/qci/register
        │       └─→ Creates Visitor + Visit (Pending)
        │
        └─→ Add Visitor (/Visitor/Add)
            └─→ POST /Visitor/Add
                └─→ Creates Visitor (No QR yet)

                        ↓

┌─────────────────────────────────────────────────────────────────┐
│                     STEP 2: QR GENERATION                        │
└─────────────────────────────────────────────────────────────────┘

Visitor List (/Visitor/Index)
        │
        └─→ Generate QR (/Visitor/GenerateQR/{id})
            │
            ├─→ Display visitor details
            ├─→ Show visit history
            │
            └─→ Click "Generate QR" button
                │
                └─→ POST /api/qci/generate/{visitorId}
                    │
                    ├─→ Check if visitor has QRCode
                    │   ├─→ Yes: Reuse existing
                    │   └─→ No: Generate new (VIS-XXXXXXXXXX)
                    │
                    ├─→ Create Visit record
                    │   └─→ Status: "Pending"
                    │       PassNumber: VIS-XXXXXXXXXX
                    │       CheckInTime: Now
                    │
                    └─→ Return QR data
                        └─→ Display QR code image
                            └─→ Download/Print options

                        ↓

┌─────────────────────────────────────────────────────────────────┐
│                    STEP 3: CHECK-IN (ENTRY)                      │
└─────────────────────────────────────────────────────────────────┘

QR Scanner Page (/Visit/ScanQR)
        │
        ├─→ Click "Start Scanner"
        │   └─→ Request camera permission
        │       └─→ Open camera feed
        │           └─→ Start scanning loop (250ms)
        │
        └─→ Visitor shows QR code
            │
            └─→ Camera detects QR
                │
                └─→ Extract pass number
                    │
                    └─→ POST /api/qci/scanpass
                        │
                        Body: { passNumber: "VIS-ABC123" }
                        │
                        └─→ Backend Logic:
                            │
                            ├─→ Find Visit by PassNumber
                            │   WHERE Status IN ('Pending', 'Active')
                            │
                            ├─→ Check current status
                            │   │
                            │   └─→ IF Status = "Pending"
                            │       │
                            │       ├─→ UPDATE Status = "Active"
                            │       ├─→ SET CheckInTime = NOW()
                            │       ├─→ SET VisitDate = NOW()
                            │       ├─→ SAVE to database
                            │       │
                            │       └─→ Return response:
                            │           {
                            │             action: "checkin",
                            │             name: "John Doe",
                            │             status: "Active",
                            │             dateTime: "08 Apr 2026, 02:30 PM"
                            │           }
                            │
                            └─→ Frontend displays:
                                ├─→ Success toast
                                ├─→ Visitor details card
                                └─→ Activity log entry

                        ↓

┌─────────────────────────────────────────────────────────────────┐
│                   STEP 4: CHECK-OUT (EXIT)                       │
└─────────────────────────────────────────────────────────────────┘

QR Scanner Page (still running)
        │
        └─→ Visitor shows SAME QR code
            │
            └─→ Camera detects QR
                │
                └─→ Extract pass number
                    │
                    └─→ POST /api/qci/scanpass
                        │
                        Body: { passNumber: "VIS-ABC123" }
                        │
                        └─→ Backend Logic:
                            │
                            ├─→ Find Visit by PassNumber
                            │   WHERE Status IN ('Pending', 'Active')
                            │
                            ├─→ Check current status
                            │   │
                            │   └─→ IF Status = "Active"
                            │       │
                            │       ├─→ UPDATE Status = "CheckedOut"
                            │       ├─→ SET CheckOutTime = NOW()
                            │       ├─→ CALCULATE Duration
                            │       │   = CheckOutTime - CheckInTime
                            │       ├─→ SAVE to database
                            │       │
                            │       └─→ Return response:
                            │           {
                            │             action: "checkout",
                            │             name: "John Doe",
                            │             status: "CheckedOut",
                            │             checkInTime: "02:30 PM",
                            │             checkOutTime: "04:15 PM",
                            │             duration: "1h 45m"
                            │           }
                            │
                            └─→ Frontend displays:
                                ├─→ Success toast
                                ├─→ Visitor details with duration
                                └─→ Activity log entry

                        ↓

┌─────────────────────────────────────────────────────────────────┐
│                   AUTOMATIC STATUS UPDATES                       │
└─────────────────────────────────────────────────────────────────┘

Dashboard (/Home/Dashboard)
        │
        ├─→ Stats auto-update:
        │   ├─→ Total Visits
        │   ├─→ Active Visits (Status = "Active")
        │   ├─→ Pending Visits (Status = "Pending")
        │   └─→ Completed Visits (Status = "CheckedOut")
        │
        └─→ Visitor History Log
            └─→ Shows all visits with current status
                └─→ Color-coded badges

Visit Management (/Visit/Index)
        │
        └─→ All visits listed with:
            ├─→ Current status
            ├─→ Check-in time
            ├─→ Check-out time
            ├─→ Duration
            └─→ Action buttons (if applicable)
```

---

## 🗄️ Database Schema

```sql
┌─────────────────────────────────────────────────────────────────┐
│                         VISITORS TABLE                           │
└─────────────────────────────────────────────────────────────────┘

Visitors
├── VisitorId (PK, INT, Identity)
├── Name (NVARCHAR, Required)
├── Phone (NVARCHAR, Required)
├── Email (NVARCHAR)
├── CompanyName (NVARCHAR)
├── CompanyAddress (NVARCHAR)
├── WhomeToMeet (NVARCHAR)
├── Department (NVARCHAR)
├── PurposeOfVisit (NVARCHAR)
├── VehicleNumber (NVARCHAR)
├── CapturePhoto (NVARCHAR) -- Path to photo
├── UploadId (NVARCHAR) -- Path to ID document
└── QRCode (NVARCHAR) -- Permanent QR: VIS-XXXXXXXXXX

┌─────────────────────────────────────────────────────────────────┐
│                          VISITS TABLE                            │
└─────────────────────────────────────────────────────────────────┘

Visits
├── VisitId (PK, INT, Identity)
├── VisitorId (FK → Visitors.VisitorId)
├── EmployeeId (FK → Employees.EmployeeId, Nullable)
├── Purpose (NVARCHAR)
├── VisitDate (DATETIME)
├── CheckInTime (DATETIME)
├── CheckOutTime (DATETIME, Nullable)
├── Status (NVARCHAR) -- "Pending", "Active", "CheckedOut"
├── PassNumber (NVARCHAR) -- Same as Visitor.QRCode
└── CreatedBy (INT)

┌─────────────────────────────────────────────────────────────────┐
│                        EMPLOYEES TABLE                           │
└─────────────────────────────────────────────────────────────────┘

Employees
├── EmployeeId (PK, INT, Identity)
├── Name (NVARCHAR)
├── Designation (NVARCHAR)
├── Phone (NVARCHAR)
├── Email (NVARCHAR)
├── DepartmentId (FK → Departments.DepartmentId)
└── PhotoPath (NVARCHAR)

┌─────────────────────────────────────────────────────────────────┐
│                       DEPARTMENTS TABLE                          │
└─────────────────────────────────────────────────────────────────┘

Departments
├── DepartmentId (PK, INT, Identity)
├── DepartmentName (NVARCHAR)
└── Description (NVARCHAR)
```

---

## 🔄 Status State Machine

```
┌─────────────────────────────────────────────────────────────────┐
│                      VISIT STATUS FLOW                           │
└─────────────────────────────────────────────────────────────────┘

                    ┌──────────────┐
                    │   PENDING    │
                    │ (QR Generated)│
                    └──────┬───────┘
                           │
                           │ Scan at Entry
                           │ (Check-In)
                           ↓
                    ┌──────────────┐
                    │    ACTIVE    │
                    │ (Checked In) │
                    └──────┬───────┘
                           │
                           │ Scan at Exit
                           │ (Check-Out)
                           ↓
                    ┌──────────────┐
                    │  CHECKEDOUT  │
                    │  (Complete)  │
                    └──────────────┘

Rules:
• Pending → Active: Only via QR scan at entry
• Active → CheckedOut: Only via QR scan at exit
• CheckedOut: Final state (no further changes)
• Cannot skip states
• Cannot go backwards
```

---

## 🔌 API Endpoints

```
┌─────────────────────────────────────────────────────────────────┐
│                      API ENDPOINTS                               │
└─────────────────────────────────────────────────────────────────┘

QuickCheckinController (/api/qci)
│
├── POST /api/qci/register
│   └─→ Register new visitor + create pending visit
│
├── POST /api/qci/generate/{visitorId}
│   └─→ Generate QR code for visitor
│
├── POST /api/qci/scanpass
│   └─→ Process QR scan (check-in or check-out)
│
├── POST /api/qci/scan
│   └─→ Process by visit ID
│
├── GET /api/qci/visitors
│   └─→ Get all visitors
│
├── GET /api/qci/visitordetail/{id}
│   └─→ Get visitor details + visit status
│
├── GET /api/qci/employees
│   └─→ Get all employees
│
├── GET /api/qci/history
│   └─→ Get visit history (Active + CheckedOut only)
│
└── GET /api/qci/qrimage/{passNumber}
    └─→ Generate QR code image (PNG)

VisitorController (/Visitor)
│
├── GET /Visitor/Index
│   └─→ List all visitors
│
├── GET /Visitor/Add
│   └─→ Add visitor form
│
├── POST /Visitor/Add
│   └─→ Create new visitor
│
├── GET /Visitor/Edit/{id}
│   └─→ Edit visitor form
│
├── POST /Visitor/Edit/{id}
│   └─→ Update visitor
│
├── GET /Visitor/View/{id}
│   └─→ View visitor details
│
├── GET /Visitor/GenerateQR/{id}
│   └─→ QR generation page
│
├── POST /Visitor/Delete/{id}
│   └─→ Delete visitor
│
└── GET /Visitor/AllQRCodes
    └─→ View all generated QR codes

VisitController (/Visit)
│
├── GET /Visit/Index
│   └─→ List all visits (with filters)
│
├── GET /Visit/View/{id}
│   └─→ View visit details
│
├── GET /Visit/ScanQR
│   └─→ QR scanner page
│
├── GET /Visit/Report
│   └─→ Day-wise visit report
│
├── GET /Visit/ExportCsv
│   └─→ Export visits to CSV
│
├── GET /Visit/HistoryLog
│   └─→ Detailed visit history
│
├── POST /Visit/CheckIn/{id}
│   └─→ Manual check-in
│
├── POST /Visit/CheckOut/{id}
│   └─→ Manual check-out
│
└── POST /Visit/Delete/{id}
    └─→ Delete visit record
```

---

## 🎨 Frontend Components

```
┌─────────────────────────────────────────────────────────────────┐
│                     FRONTEND STRUCTURE                           │
└─────────────────────────────────────────────────────────────────┘

Views/
│
├── Home/
│   └── Dashboard.cshtml
│       ├─→ Stats cards
│       ├─→ Quick check-in form
│       ├─→ Recent visitors
│       ├─→ Employee directory
│       └─→ Visitor history log
│
├── Visitor/
│   ├── Index.cshtml (List)
│   ├── Add.cshtml (Create)
│   ├── Edit.cshtml (Update)
│   ├── View.cshtml (Details)
│   ├── GenerateQR.cshtml (QR Generation)
│   └── AllQRCodes.cshtml (QR List)
│
├── Visit/
│   ├── Index.cshtml (List with filters)
│   ├── View.cshtml (Details)
│   ├── ScanQR.cshtml (QR Scanner) ⭐
│   ├── Report.cshtml (Day report)
│   └── HistoryLog.cshtml (Full history)
│
├── Employee/
│   ├── Index.cshtml
│   ├── Add.cshtml
│   ├── Edit.cshtml
│   └── View.cshtml
│
├── Department/
│   ├── Index.cshtml
│   ├── Add.cshtml
│   ├── Edit.cshtml
│   └── View.cshtml
│
└── Shared/
    └── _Layout.cshtml (Master layout)

wwwroot/
│
├── js/
│   ├── Dashboard.js (Dashboard logic)
│   ├── ScanQR.js (QR scanner logic) ⭐
│   └── Visit.js (Visit management)
│
└── css/
    ├── Dashboard.css
    ├── ScanQR.css ⭐
    ├── Visit.css
    ├── Visitor.css
    └── GenerateQR.css
```

---

## 🔐 Security & Validation

```
┌─────────────────────────────────────────────────────────────────┐
│                    SECURITY MEASURES                             │
└─────────────────────────────────────────────────────────────────┘

Authentication:
├─→ Session-based authentication
├─→ Login required for all pages
└─→ Admin role verification

Authorization:
├─→ Only authenticated users can access system
├─→ Session timeout after inactivity
└─→ Logout functionality

Data Validation:
├─→ Required fields enforced
├─→ Phone number format validation
├─→ Email format validation
└─→ Pass number uniqueness

QR Scanning:
├─→ 2-second cooldown (duplicate prevention)
├─→ Status validation (only valid transitions)
├─→ Pass number verification
└─→ Visit existence check

Database:
├─→ Foreign key constraints
├─→ Transaction support
├─→ Audit trail (timestamps)
└─→ Data integrity checks
```

---

## 📈 Performance Optimization

```
┌─────────────────────────────────────────────────────────────────┐
│                   PERFORMANCE FEATURES                           │
└─────────────────────────────────────────────────────────────────┘

QR Scanner:
├─→ 250ms scan interval (4 scans/second)
├─→ Canvas-based image processing
├─→ Efficient QR detection (jsQR library)
└─→ Minimal DOM updates

Database:
├─→ Indexed columns (VisitorId, PassNumber, Status)
├─→ Efficient queries with WHERE clauses
├─→ Include() for eager loading
└─→ Pagination for large datasets

Frontend:
├─→ Minimal JavaScript libraries
├─→ CSS animations (GPU-accelerated)
├─→ Lazy loading for images
└─→ Cached static assets

API:
├─→ RESTful design
├─→ JSON responses
├─→ Async/await patterns
└─→ Error handling
```

---

## 🎯 System Capabilities

✅ **Visitor Management**
- Register visitors
- Update visitor information
- View visitor history
- Search and filter

✅ **QR Code System**
- Generate unique QR codes
- Reusable QR codes per visitor
- Print/download QR codes
- Camera-based scanning

✅ **Check-In/Check-Out**
- Automatic status updates
- Real-time processing
- Duration calculation
- Activity logging

✅ **Reporting**
- Visit history
- Day-wise reports
- CSV export
- Analytics dashboard

✅ **User Interface**
- Responsive design
- Modern UI/UX
- Toast notifications
- Real-time updates

---

**Your system architecture is complete and production-ready!** 🚀
