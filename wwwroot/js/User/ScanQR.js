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

        try {
            status.innerHTML = '<i class="fas fa-spinner fa-spin me-2 text-primary"></i>Accessing Camera...';
            videoStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" } });
            video.srcObject = videoStream;
            await video.play();
            
            status.innerHTML = '<i class="fas fa-wifi fa-pulse me-2 text-success"></i>Scanner Online';
            scanningActive = true;
            startBtn.style.display = 'none';
            stopBtn.style.display = 'inline-block';
            
            scanInterval = setInterval(scanQRCode, 200);
        } catch (err) {
            console.error("Scanner Error:", err);
            status.innerHTML = '<i class="fas fa-times-circle me-2 text-danger"></i>Access Denied';
            alert("Camera access is required for scanning.");
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
        fetch('/User/ProcessQR', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'code=' + encodeURIComponent(passNumber)
        })
        .then(r => r.json())
        .then(res => {
            displayResult(res, passNumber);
            if (res.success) addActivity(res.visitor, passNumber, res.message);
        })
        .catch(err => console.error("API Error:", err));
    }

    function displayResult(res, pass) {
        const card = document.getElementById('resultCard');
        const header = document.getElementById('resultHeader');
        const title = document.getElementById('resultTitle');
        const content = document.getElementById('resultContent');
        if (!card || !header || !title || !content) return;

        card.style.display = 'block';
        header.style.backgroundColor = res.success ? '#10B981' : '#EF4444';
        title.innerHTML = res.success ? '<i class="fas fa-check-double me-2"></i>ACCESS GRANTED' : '<i class="fas fa-times-circle me-2"></i>ACCESS DENIED';

        content.innerHTML = `
            <div class="result-info">
                <div class="result-field"><span class="result-label">PASS IDENTIFIER</span><span class="result-value fw-800 text-primary">${pass}</span></div>
                <div class="result-field"><span class="result-label">VISITOR NAME</span><span class="result-value">${res.visitor || 'N/A'}</span></div>
                <div class="result-field"><span class="result-label">SYSTEM MESSAGE</span><span class="result-value">${res.message}</span></div>
            </div>
            <button class="premium-btn secondary w-100 mt-4 rounded-pill fw-800" onclick="document.getElementById('resultCard').style.display='none'">DISMISS</button>
        `;
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
