# QR Code Scanner Implementation Guide

## Overview
Your Visitor Management System now has a complete QR code scanning solution for check-in and check-out processing.

## Features Implemented

### 1. QR Scanner Page (`/Visit/ScanQR`)
- **Camera Scanner**: Real-time QR code scanning using device camera
- **Manual Entry**: Fallback option to manually enter pass numbers
- **Live Stats**: Display of total visits, active, pending, and completed visits
- **Scan Results**: Detailed visitor information after successful scan
- **Activity Log**: Recent check-in/check-out history

### 2. API Endpoints

#### `/api/qci/scanpass` (POST)
Processes QR code scans by pass number.

**Request:**
```json
{
  "passNumber": "VIS-ABC123"
}
```

**Response (Check-In):**
```json
{
  "action": "checkin",
  "visitId": 123,
  "name": "John Doe",
  "phone": "1234567890",
  "company": "ABC Corp",
  "host": "Jane Smith",
  "department": "IT",
  "purpose": "Meeting",
  "passNumber": "VIS-ABC123",
  "dateTime": "08 Apr 2026, 02:30 PM",
  "status": "Active"
}
```

**Response (Check-Out):**
```json
{
  "action": "checkout",
  "visitId": 123,
  "name": "John Doe",
  "checkInTime": "08 Apr 2026, 02:30 PM",
  "checkOutTime": "08 Apr 2026, 04:15 PM",
  "duration": "1h 45m",
  "status": "CheckedOut"
}
```

### 3. How It Works

#### Workflow:
1. **Visitor Registration**: Admin registers visitor and generates QR code
2. **QR Code Generation**: System creates unique pass number (e.g., VIS-ABC123)
3. **Visitor Receives QR**: QR code can be downloaded/printed
4. **Check-In**: Visitor shows QR at entry → Scanner reads code → Status changes to "Active"
5. **Check-Out**: Visitor shows same QR at exit → Scanner reads code → Status changes to "CheckedOut"

#### Status Flow:
```
Pending → Active → CheckedOut
   ↑        ↑         ↑
   QR Gen   Check-In  Check-Out
```

### 4. Camera Access
The scanner uses the browser's `getUserMedia` API to access the device camera:
- **Desktop**: Uses webcam
- **Mobile**: Automatically uses back camera
- **Permissions**: Browser will request camera permission on first use

### 5. QR Code Library
Uses **jsQR** library for client-side QR code detection:
- Fast and accurate scanning
- Works in real-time (scans every 300ms)
- 3-second cooldown between scans to prevent duplicates

### 6. Navigation
QR Scanner is accessible from:
- **Sidebar**: "QR Scanner" menu item
- **Dashboard**: "Scan QR" button in Quick Check-In section
- **Direct URL**: `/Visit/ScanQR`

## Usage Instructions

### For Administrators:

1. **Start Scanner**:
   - Navigate to QR Scanner page
   - Click "Start Scanner" button
   - Allow camera access when prompted
   - Point camera at visitor's QR code

2. **Manual Entry** (if camera unavailable):
   - Click "Manual Entry" button
   - Type pass number (e.g., VIS-ABC123)
   - Click "Process"

3. **View Results**:
   - Scan result shows visitor details
   - Activity log tracks recent scans
   - Stats update in real-time

### For Visitors:

1. **Get QR Code**:
   - Admin generates QR from Visitor → Generate QR page
   - Download or print the QR code pass
   - Keep QR code accessible on phone or paper

2. **Check-In**:
   - Show QR code to admin at entry
   - Admin scans code
   - System records check-in time

3. **Check-Out**:
   - Show same QR code at exit
   - Admin scans code
   - System records check-out time and calculates duration

## Technical Details

### Files Created:
- `Views/Visit/ScanQR.cshtml` - Scanner page view
- `wwwroot/css/ScanQR.css` - Scanner styling
- `wwwroot/js/ScanQR.js` - Scanner logic and camera handling
- `Controllers/VisitController.cs` - Added ScanQR action
- `Controllers/QuickCheckinController.cs` - Added scanpass endpoint

### Dependencies:
- **jsQR**: QR code detection library (loaded from CDN)
- **Font Awesome**: Icons
- **Inter Font**: Typography

### Browser Compatibility:
- Chrome/Edge: Full support
- Firefox: Full support
- Safari: Full support (iOS 11+)
- Mobile browsers: Full support with back camera

### Security:
- Camera access requires user permission
- API validates pass numbers before processing
- Only active/pending visits can be scanned
- Duplicate scan prevention (3-second cooldown)

## Troubleshooting

### Camera Not Working:
1. Check browser permissions (Settings → Privacy → Camera)
2. Ensure HTTPS connection (camera requires secure context)
3. Try different browser
4. Use manual entry as fallback

### QR Not Scanning:
1. Ensure good lighting
2. Hold QR code steady within scanner frame
3. Try adjusting distance (6-12 inches optimal)
4. Ensure QR code is not damaged or blurry

### Invalid QR Code Error:
1. Verify pass number is correct
2. Check if visit status is Pending or Active
3. Ensure visitor has been registered in system

## Future Enhancements (Optional):
- [ ] Sound/vibration feedback on successful scan
- [ ] Batch scanning mode
- [ ] Offline scanning with sync
- [ ] QR code expiration
- [ ] Multi-camera support
- [ ] Scan history export
- [ ] Mobile app integration

## Support
For issues or questions, check the application logs or contact system administrator.
