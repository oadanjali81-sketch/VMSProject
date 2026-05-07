# QR Scanner Troubleshooting Guide

## Common Issues and Solutions

### 1. "Camera access denied or unavailable"

#### Cause:
Browser has blocked camera access or camera is not available.

#### Solutions:

**For Chrome/Edge:**
1. Click the camera icon (🎥) in the address bar (left side)
2. Select "Allow" for camera permission
3. Refresh the page
4. Click "Start Scanner" again

**For Firefox:**
1. Click the camera icon in the address bar
2. Select "Allow" and check "Remember this decision"
3. Refresh the page
4. Try again

**For Safari:**
1. Go to Safari → Settings → Websites → Camera
2. Find your website and set to "Allow"
3. Refresh the page
4. Try again

**Alternative:**
- Use the "Manual Entry" button to type pass numbers directly
- No camera required for manual entry

---

### 2. "HTTPS Required" Error

#### Cause:
Modern browsers require HTTPS for camera access (except localhost).

#### Solutions:

**If on localhost (development):**
- Should work automatically
- Make sure URL is `http://localhost:port` or `http://127.0.0.1:port`

**If on production server:**
- Must use HTTPS (SSL certificate required)
- Contact your system administrator to enable HTTPS
- Use "Manual Entry" as alternative until HTTPS is configured

---

### 3. "No Camera Found"

#### Cause:
No camera device detected on your computer/device.

#### Solutions:
1. **Check Hardware:**
   - Ensure camera is connected (for external webcams)
   - Check if camera is enabled in Device Manager (Windows)
   - Try unplugging and reconnecting USB camera

2. **Check Drivers:**
   - Update camera drivers
   - Restart computer

3. **Use Alternative:**
   - Use "Manual Entry" button
   - Use a device with a camera (phone/tablet)

---

### 4. "Camera In Use"

#### Cause:
Another application is using the camera.

#### Solutions:
1. Close other applications that might use camera:
   - Zoom, Teams, Skype
   - Other browser tabs with camera access
   - Camera app
   
2. Restart browser

3. Use "Manual Entry" as alternative

---

### 5. QR Code Not Scanning

#### Cause:
Poor lighting, damaged QR code, or wrong distance.

#### Solutions:
1. **Improve Lighting:**
   - Ensure good lighting on QR code
   - Avoid glare or shadows

2. **Adjust Distance:**
   - Hold QR code 6-12 inches from camera
   - Keep QR code steady

3. **Check QR Code:**
   - Ensure QR code is not damaged or blurry
   - Print new QR code if needed

4. **Use Manual Entry:**
   - Type the pass number shown below QR code

---

### 6. Scanner Freezes or Stops

#### Cause:
Browser performance issue or memory problem.

#### Solutions:
1. Click "Stop Scanner"
2. Close other browser tabs
3. Refresh the page
4. Click "Start Scanner" again

---

## Browser Compatibility

### ✅ Fully Supported:
- Chrome 53+ (Desktop & Mobile)
- Edge 79+ (Desktop & Mobile)
- Firefox 36+ (Desktop & Mobile)
- Safari 11+ (Desktop & Mobile)
- Opera 40+

### ⚠️ Limited Support:
- Internet Explorer: Not supported (use Edge instead)
- Older browsers: May not support camera API

---

## Manual Entry Instructions

If camera scanning doesn't work, you can always use Manual Entry:

1. Click "Manual Entry" button
2. Type the visitor's pass number (e.g., VIS-ABC123)
3. Click "Process"
4. System will check-in or check-out the visitor

**Note:** Pass number is case-sensitive and must match exactly.

---

## Testing Camera Access

### Quick Test:
1. Open browser console (F12)
2. Run: `navigator.mediaDevices.getUserMedia({video:true})`
3. If permission dialog appears → Camera API works
4. If error appears → Check browser settings

### Check Permissions:
1. Click "Check Permission" button (if visible)
2. Follow on-screen instructions

---

## Security Notes

### Why HTTPS is Required:
- Modern browsers restrict camera access to secure contexts
- This protects user privacy
- Localhost is exempt from this requirement

### Permission Persistence:
- Browser remembers your camera permission choice
- You can revoke permission anytime in browser settings
- Permission is per-website

---

## Best Practices

### For Administrators:
1. Test scanner before visitor arrival
2. Keep backup manual entry option available
3. Ensure good lighting at scanning station
4. Print QR codes clearly (minimum 2x2 inches)

### For Visitors:
1. Have QR code ready before approaching
2. Keep QR code clean and undamaged
3. Hold steady when scanning
4. If scan fails, provide pass number verbally

---

## Still Having Issues?

### Diagnostic Steps:
1. Check browser console for errors (F12 → Console tab)
2. Try different browser
3. Try different device
4. Use Manual Entry as workaround

### Contact Support:
- Provide browser name and version
- Provide error message from console
- Describe exact steps that cause the issue
- Include screenshot if possible

---

## FAQ

**Q: Do I need to install anything?**
A: No, the scanner works directly in the browser.

**Q: Does it work on mobile phones?**
A: Yes, works on iOS and Android browsers.

**Q: Can I use it offline?**
A: Camera scanning requires the page to be loaded, but once loaded, it works without internet for scanning. However, check-in/check-out requires internet to save to database.

**Q: Why does it ask for camera permission every time?**
A: Make sure to check "Remember this decision" when granting permission.

**Q: Can multiple people scan at the same time?**
A: Yes, each device/browser can scan independently.

**Q: What if the QR code is on a phone screen?**
A: It works! Just ensure screen brightness is high and avoid glare.

---

## Technical Details

### Camera Requirements:
- Minimum resolution: 640x480
- Frame rate: 15 fps or higher
- Focus: Auto-focus recommended

### QR Code Specifications:
- Format: Standard QR Code
- Error correction: High (H level)
- Size: Minimum 2x2 inches when printed
- Content: Pass number (e.g., VIS-ABC123)

### Browser APIs Used:
- MediaDevices API (camera access)
- getUserMedia (video stream)
- jsQR library (QR detection)
- Canvas API (frame processing)

---

## Version Information
- Scanner Version: 1.0
- jsQR Library: 1.4.0
- Last Updated: 2026-04-08
