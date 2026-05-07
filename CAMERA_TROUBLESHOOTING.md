# Camera Not Found - Troubleshooting Guide

## 🔍 Quick Diagnosis

### Step 1: Click "Test Camera" Button
On the QR Scanner page, click the **"Test Camera"** button to run diagnostics.

This will tell you:
- ✅ If browser supports camera
- ✅ How many cameras are detected
- ✅ If camera access works
- ❌ What's blocking camera access

---

## 🛠️ Common Solutions

### Solution 1: Check Device Manager (Windows)

1. Press `Windows + X`
2. Select "Device Manager"
3. Expand "Cameras" or "Imaging devices"
4. Look for your camera (e.g., "Integrated Camera", "USB Camera")

**If camera is there:**
- Right-click → Enable (if disabled)
- Right-click → Update driver

**If camera is NOT there:**
- Camera may not be connected
- Driver may not be installed
- Try restarting computer

---

### Solution 2: Close Other Apps Using Camera

Camera can only be used by ONE app at a time.

**Close these apps:**
- Zoom
- Microsoft Teams
- Skype
- Discord
- OBS Studio
- Any other video conferencing apps

**Also check:**
- Other browser tabs
- Windows Camera app
- Any app with video preview

---

### Solution 3: Check Browser Permissions

**Chrome/Edge:**
1. Click the lock icon (🔒) in address bar
2. Find "Camera" permission
3. Set to "Allow"
4. Refresh the page

**Firefox:**
1. Click the camera icon in address bar
2. Select "Allow"
3. Check "Remember this decision"
4. Refresh the page

---

### Solution 4: Enable Camera in Windows Settings

1. Press `Windows + I` (Settings)
2. Go to "Privacy & Security"
3. Click "Camera"
4. Turn ON "Camera access"
5. Turn ON "Let apps access your camera"
6. Turn ON "Let desktop apps access your camera"

---

### Solution 5: Check Physical Camera

**For Laptop:**
- Look for camera lens above screen
- Check if there's a physical privacy shutter
- Some laptops have F-key to enable/disable camera (e.g., F8, F10)

**For External Webcam:**
- Check USB connection
- Try different USB port
- Check if LED light works
- Try on another computer to verify it works

---

### Solution 6: Restart Browser

1. Close ALL browser windows
2. End browser process in Task Manager (Ctrl+Shift+Esc)
3. Reopen browser
4. Go back to QR Scanner page
5. Click "Start Scanner"

---

### Solution 7: Try Different Browser

If camera doesn't work in one browser, try another:

**Recommended browsers:**
- ✅ Google Chrome (best support)
- ✅ Microsoft Edge (best support)
- ✅ Firefox (good support)
- ❌ Internet Explorer (not supported)

---

### Solution 8: Update Camera Driver

1. Open Device Manager
2. Find your camera
3. Right-click → Update driver
4. Select "Search automatically for drivers"
5. Restart computer after update

---

### Solution 9: Check HTTPS/Localhost

Camera API requires secure connection:

**✅ Works on:**
- `https://yoursite.com`
- `http://localhost:5000`
- `http://127.0.0.1:5000`

**❌ Doesn't work on:**
- `http://192.168.x.x` (local IP)
- `http://yoursite.com` (non-HTTPS)

**Solution:** Use localhost for development or enable HTTPS for production.

---

## 🔧 Advanced Troubleshooting

### Check Browser Console

1. Press `F12` to open Developer Tools
2. Go to "Console" tab
3. Look for error messages
4. Common errors:

```
NotFoundError: Requested device not found
→ No camera detected

NotAllowedError: Permission denied
→ Camera permission blocked

NotReadableError: Could not start video source
→ Camera in use by another app

OverconstrainedError: Constraints not satisfied
→ Camera doesn't support requested resolution
```

### Test in Browser Console

Open browser console (F12) and run:

```javascript
// Test 1: Check if API exists
console.log('MediaDevices:', navigator.mediaDevices);

// Test 2: List devices
navigator.mediaDevices.enumerateDevices()
  .then(devices => {
    console.log('All devices:', devices);
    console.log('Cameras:', devices.filter(d => d.kind === 'videoinput'));
  });

// Test 3: Try to access camera
navigator.mediaDevices.getUserMedia({ video: true })
  .then(stream => {
    console.log('✓ Camera works!', stream);
    stream.getTracks().forEach(t => t.stop());
  })
  .catch(err => console.error('✗ Camera error:', err));
```

---

## 🎯 Alternative: Use Manual Entry

If camera still doesn't work, you can use **Manual Entry**:

1. Click "Manual Entry" button
2. Type the visitor's pass number (e.g., VIS-ABC123)
3. Click "Process"
4. Check-in/check-out happens without camera!

**This is a reliable backup method.**

---

## 📱 Try Mobile Device

If laptop camera doesn't work:

1. Open QR Scanner on your phone/tablet
2. Browser will use phone's camera
3. Works the same way
4. Mobile cameras often work better for QR scanning

---

## ✅ Verification Checklist

Before contacting support, verify:

- [ ] Camera shows in Device Manager
- [ ] Camera is enabled (not disabled)
- [ ] No other apps using camera
- [ ] Browser has camera permission
- [ ] Windows camera access is ON
- [ ] Using supported browser (Chrome/Edge/Firefox)
- [ ] On HTTPS or localhost
- [ ] Tried restarting browser
- [ ] Tried "Test Camera" button
- [ ] Checked browser console for errors

---

## 🆘 Still Not Working?

### Quick Workarounds:

1. **Use Manual Entry** - Type pass numbers instead of scanning
2. **Use Mobile Device** - Phone camera usually works better
3. **Use Different Computer** - Try another device
4. **Print QR Codes** - Use external QR scanner app

### Get Help:

1. Click "Test Camera" and screenshot the results
2. Press F12, go to Console tab, screenshot any errors
3. Note your:
   - Browser name and version
   - Operating system
   - Camera model (from Device Manager)
4. Contact IT support with this information

---

## 📊 Common Error Messages

| Error | Meaning | Solution |
|-------|---------|----------|
| No Camera Found | No camera detected | Check Device Manager, connect camera |
| Permission Denied | Camera access blocked | Allow in browser settings |
| Camera In Use | Another app using camera | Close other apps |
| Not Readable | Hardware error | Restart browser/computer |
| Not Supported | Browser too old | Update browser |
| HTTPS Required | Not on secure connection | Use localhost or HTTPS |

---

## 💡 Pro Tips

1. **Keep browser updated** - Latest version has best camera support
2. **Use Chrome** - Best camera API support
3. **Test regularly** - Run "Test Camera" before important use
4. **Have backup** - Always have Manual Entry available
5. **Check permissions** - Review browser camera permissions monthly

---

## 🎓 Understanding the Error

**"No Camera Found"** means:
- Browser's `enumerateDevices()` returned 0 video inputs
- This is a hardware/driver/permission issue
- Not a problem with the QR scanner code

**The scanner code is working correctly** - it just can't find a camera to use.

---

**Need immediate solution?** Use Manual Entry - it works without any camera! 🚀
