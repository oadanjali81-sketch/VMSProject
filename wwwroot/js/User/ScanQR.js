/**
 * QR SCANNER MODULE
 * Handles live camera scanning and QR pass verification.
 */

(function() {
    let videoStream = null;
    let scanningActive = false;
    let scanInterval = null;
    let lastScannedCode = null;
    let lastScanTime = 0;
    const SCAN_COOLDOWN = 3000;

    window.toggleManualEntry = function() {
        const card = document.getElementById('manualEntryCard');
        if (!card) return;
        card.style.display = card.style.display === 'none' ? 'block' : 'none';
        if (card.style.display === 'block') document.getElementById('manualPassInput').focus();
    };

    window.startScanner = async function() {
        const video = document.getElementById('scannerVideo');
        const status = document.getElementById('scannerStatus');
        const startBtn = document.getElementById('startScanBtn');
        const stopBtn = document.getElementById('stopScanBtn');

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            status.innerHTML = '<i class="fas fa-exclamation-triangle me-2 text-danger"></i>Insecure / Unsupported';
            alert("Camera access requires a secure (HTTPS) connection or localhost. Please check your browser settings.");
            return;
        }

        try {
            status.innerHTML = '<i class="fas fa-spinner fa-spin me-2 text-primary"></i>Requesting Camera...';
            
            let constraints = { 
                video: { 
                    facingMode: { ideal: "environment" },
                    width: { ideal: 1280 },
                    height: { ideal: 720 }
                } 
            };

            try {
                videoStream = await navigator.mediaDevices.getUserMedia(constraints);
            } catch (e) {
                console.warn("Preferred constraints failed, falling back to basic video...", e);
                videoStream = await navigator.mediaDevices.getUserMedia({ video: true });
            }

            console.log("Stream obtained:", videoStream.id);
            video.srcObject = videoStream;
            
            // Some browsers require explicit play call
            video.onloadedmetadata = async () => {
                try {
                    await video.play();
                    status.innerHTML = '<i class="fas fa-wifi fa-pulse me-2 text-success"></i>Scanner Online';
                    scanningActive = true;
                    startBtn.style.display = 'none';
                    stopBtn.style.display = 'inline-block';
                    
                    if (scanInterval) clearInterval(scanInterval);
                    scanInterval = setInterval(scanQRCode, 250);
                } catch (playErr) {
                    console.error("Playback Error:", playErr);
                    status.innerHTML = '<i class="fas fa-times-circle me-2 text-danger"></i>Playback Blocked';
                }
            };

        } catch (err) {
            console.error("Scanner Error:", err);
            let msg = "Camera access denied.";
            let detail = "Please check your browser permissions.";

            if (err.name === 'NotAllowedError') {
                msg = "Camera permission denied.";
                detail = "Please click the camera/lock icon in your browser address bar and allow camera access for this site.";
            } else if (err.name === 'NotFoundError') {
                msg = "No camera detected.";
                detail = "Ensure your camera is connected and recognized by your system.";
            } else if (err.name === 'NotReadableError') {
                msg = "Camera in use.";
                detail = "Another application is using your camera. Please close it and try again.";
            }
            
            status.innerHTML = `<i class="fas fa-times-circle me-2 text-danger"></i>${msg}`;
            alert(`${msg}\n\n${detail}`);
        }
    };

    window.stopScanner = function() {
        scanningActive = false;
        if (scanInterval) clearInterval(scanInterval);
        if (videoStream) videoStream.getTracks().forEach(t => t.stop());
        
        const video = document.getElementById('scannerVideo');
        if (video) video.srcObject = null;
        const status = document.getElementById('scannerStatus');
        if (status) status.innerHTML = '<i class="fas fa-power-off me-2 opacity-50"></i>Scanner Offline';
        const startBtn = document.getElementById('startScanBtn');
        const stopBtn = document.getElementById('stopScanBtn');
        if (startBtn) startBtn.style.display = 'inline-block';
        if (stopBtn) stopBtn.style.display = 'none';
    };

    function scanQRCode() {
        if (!scanningActive) return;
        const video = document.getElementById('scannerVideo');
        const canvas = document.getElementById('scannerCanvas');
        if (!video || !canvas) return;
        const ctx = canvas.getContext('2d');

        if (video.readyState === video.HAVE_ENOUGH_DATA) {
            canvas.height = video.videoHeight;
            canvas.width = video.videoWidth;
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
            if (typeof jsQR === 'undefined') {
                console.error("jsQR library not found");
                return;
            }
            const code = jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: "attemptBoth" });

            if (code) {
                const now = Date.now();
                if (code.data !== lastScannedCode || (now - lastScanTime) > SCAN_COOLDOWN) {
                    lastScannedCode = code.data;
                    lastScanTime = now;
                    processPass(code.data);
                }
            }
        }
    }

    function processPass(passNumber) {
        // Stop scanner immediately on first successful detection
        stopScanner();

        fetch('/User/ProcessQR', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'code=' + encodeURIComponent(passNumber)
        })
        .then(r => r.json())
        .then(res => {
            // Play success beep if valid scan
            if (res.success) {
                const beep = document.getElementById('scanBeep');
                if (beep) {
                    beep.currentTime = 0;
                    beep.play().catch(e => console.log("Audio play blocked:", e));
                }
            }
            
            displayResult(res, passNumber);
            if (res.success) addActivity(res.visitor, passNumber, res.message);
        })
        .catch(err => {
            console.error("API Error:", err);
            // Re-enable if needed or show error
        });
    }

    function displayResult(res, pass) {
        const modalEl = document.getElementById('scanResultModal');
        const header = document.getElementById('modalHeader');
        const title = document.getElementById('modalTitle');
        const content = document.getElementById('modalContent');
        const iconContainer = document.getElementById('modalIcon');
        
        if (!modalEl || !header || !title || !content || !iconContainer) return;

        const modal = new bootstrap.Modal(modalEl);
        
        // Style based on success
        header.style.backgroundColor = res.success ? '#10B981' : '#EF4444';
        title.innerText = res.message.toUpperCase();
        iconContainer.innerHTML = res.success 
            ? '<i class="fas fa-check-circle fa-4x animate-bounce"></i>' 
            : '<i class="fas fa-exclamation-triangle fa-4x"></i>';

        content.innerHTML = `
            <div class="result-info">
                <div class="result-field">
                    <span class="result-label">PASS IDENTIFIER</span>
                    <span class="result-value fw-800 text-primary font-monospace">${pass}</span>
                </div>
                <div class="result-field">
                    <span class="result-label">VISITOR NAME</span>
                    <span class="result-value fw-700 text-dark">${res.visitor || 'Unknown Visitor'}</span>
                </div>
                <div class="result-field">
                    <span class="result-label">COMPANY NAME</span>
                    <span class="result-value fw-700 text-dark">${res.companyName || 'Individual / Personal'}</span>
                </div>
            </div>
        `;

        modal.show();
    }

    function addActivity(name, pass, msg) {
        const list = document.getElementById('activityList');
        if (!list) return;
        const empty = list.querySelector('.activity-empty');
        if (empty) empty.remove();

        const item = document.createElement('div');
        item.className = 'activity-item';
        item.innerHTML = `
            <div class="d-flex justify-content-between align-items-start mb-2">
                <span class="fw-800 text-dark" style="font-size: 0.95rem;">${name || 'Anonymous'}</span>
                <span class="nexus-badge nexus-badge-info" style="font-size: 0.65rem;">${new Date().toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
            </div>
            <div class="text-muted small mb-2">${msg}</div>
            <div class="font-monospace fw-800 text-primary" style="font-size: 0.75rem;">${pass}</div>
        `;
        list.insertBefore(item, list.firstChild);
    }

    window.clearActivity = function() {
        const list = document.getElementById('activityList');
        if (list) {
            list.innerHTML = '<div class="text-center py-5 text-muted activity-empty"><div class="opacity-25 mb-3"><i class="fas fa-box-open fa-3x"></i></div><p class="small fw-600 mb-0">Waiting for first scan...</p></div>';
        }
    };

    window.processManualEntry = function() {
        const input = document.getElementById('manualPassInput');
        if (input && input.value.trim()) processPass(input.value.trim());
        if (input) input.value = '';
    };
})();
