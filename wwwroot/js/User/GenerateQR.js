// GenerateQR.js — all JS for the Generate QR page
// VISITOR_ID, EXISTING_PASS, HAS_EXISTING are set as globals in the view

function toggleSidebar() {
    document.getElementById('sidebar').classList.toggle('collapsed');
    document.getElementById('mainContent').classList.toggle('expanded');
}

function showToast(msg) {
    const t = document.getElementById('toastMsg');
    t.innerText = msg; t.style.opacity = '1';
    setTimeout(() => t.style.opacity = '0', 3200);
}

// Filter dropdown — rebuild options (Chrome ignores display:none on <option>)
let _allOpts = null;
function filterVisitorDropdown(q) {
    const sel = document.getElementById('visitorSelect');
    if (!_allOpts) {
        _allOpts = Array.from(sel.options).map(o => ({
            value: o.value, text: o.text,
            name: o.dataset.name || '', company: o.dataset.company || '',
            sel: o.selected
        }));
    }
    const lower = q.toLowerCase().trim();
    const cur = sel.value;
    sel.innerHTML = '';
    _allOpts.forEach(o => {
        if (!o.value || !lower || o.name.includes(lower) || o.company.includes(lower)) {
            const opt = document.createElement('option');
            opt.value = o.value; opt.text = o.text;
            opt.dataset.name = o.name; opt.dataset.company = o.company;
            if (o.value === cur) opt.selected = true;
            sel.appendChild(opt);
        }
    });
}

// Render QR image into canvas div
function renderQR(passNumber) {
    const enc = encodeURIComponent(passNumber);
    const qrUrl = 'https://api.qrserver.com/v1/create-qr-code/?size=240x240&color=1e3c72&bgcolor=ffffff&qzone=1&data=' + enc;
    
    document.getElementById('qrCodeCanvas').innerHTML =
        '<img src="' + qrUrl + '" width="240" height="240" style="border-radius:12px;display:block;cursor:pointer;" id="qrImg" alt="QR" onclick="openViewQRModal()" />' +
        '<div class="qr-floating-actions">' +
            '<div class="f-action-btn" title="Download" onclick="downloadQR()"><i class="fas fa-download"></i></div>' +
            '<div class="f-action-btn" title="Share" onclick="sharePass()"><i class="fas fa-share-nodes"></i></div>' +
        '</div>';
        
    const tag = document.getElementById('passTag');
    tag.textContent = passNumber; tag.style.visibility = 'visible';
    const hint = document.getElementById('qrHint');
    if (hint) hint.style.display = 'block';
    const dl = document.getElementById('dlBtn'); if (dl) dl.style.display = 'inline-flex';
    const pr = document.getElementById('printBtn'); if (pr) pr.style.display = 'inline-flex';
}

function sharePass() {
    const name = (document.getElementById('vpName')?.textContent || 'Visitor').trim();
    const img = document.getElementById('qrImg');
    if (!img) return;
    if (navigator.share) {
        navigator.share({ title: 'Visitor Pass — ' + name, url: img.src }).catch(() => {});
    } else {
        window.open(img.src, '_blank');
    }
}

// Open QR modal
function openQRModal(passNumber, visitorName) {
    document.getElementById('qrModalName').textContent = visitorName;
    document.getElementById('qrModalPass').textContent = passNumber;
    document.getElementById('qrModalImg').src =
        'https://api.qrserver.com/v1/create-qr-code/?size=240x240&color=1e3c72&bgcolor=ffffff&qzone=2&data='
        + encodeURIComponent(passNumber);
    const m = document.getElementById('qrViewModal');
    m.style.visibility = 'visible'; m.style.opacity = '1';
}

function closeQRModal() {
    const m = document.getElementById('qrViewModal');
    m.style.opacity = '0';
    setTimeout(() => m.style.visibility = 'hidden', 250);
}

// View QR for existing pass
function openViewQRModal() {
    if (!window.EXISTING_PASS) { showToast('No QR found for this visitor'); return; }
    openQRModal(window.EXISTING_PASS, (document.getElementById('vpName')?.textContent || '').trim());
}

// Download QR inline image
function downloadQR() {
    const img = document.getElementById('qrImg');
    if (!img) { showToast('Generate QR first'); return; }
    const a = document.createElement('a');
    a.href = img.src; a.download = 'visitor-pass-' + window.VISITOR_ID + '.png'; a.target = '_blank';
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
}

// Download QR from modal
function downloadModalQR() {
    const img = document.getElementById('qrModalImg');
    if (!img || !img.src) return;
    const a = document.createElement('a');
    a.href = img.src; a.download = 'visitor-pass-' + window.VISITOR_ID + '.png'; a.target = '_blank';
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
}

// Print pass — build HTML string without any literal </body> that Razor could parse
function printPass() {
    const name    = (document.getElementById('vpName')?.textContent    || '').trim();
    const company = (document.getElementById('vpCompany')?.textContent || '').trim();
    const meet    = (document.getElementById('vpMeet')?.textContent    || '').trim();
    const purpose = (document.getElementById('vpPurpose')?.textContent || '').trim();
    const pass    = (document.getElementById('passTag')?.textContent   || '').trim();
    const img     = document.getElementById('qrImg');
    const qrSrc   = img ? img.src : '';
    const w = window.open('', '_blank');
    if (!w) { showToast('Allow popups to print'); return; }
    const parts = [
        '<!DOCTYPE html><html>',
        '<head><title>Visitor Pass</title>',
        '<style>',
        'body{font-family:Inter,sans-serif;padding:40px;text-align:center;}',
        '.card{border:2px solid #1e3c72;border-radius:20px;padding:30px;max-width:380px;margin:0 auto;}',
        'h2{color:#1e3c72;margin-bottom:16px;}',
        'img{border-radius:12px;margin:16px 0;}',
        '.info{text-align:left;margin:16px 0;}',
        '.info p{margin:6px 0;font-size:14px;}',
        '.pass{font-family:monospace;background:#eef2ff;padding:6px 14px;border-radius:20px;font-size:13px;color:#2a5298;}',
        '</style></head>',
        '<body>',
        '<div class="card">',
        '<h2>VMS &#8212; Visitor Pass</h2>',
        qrSrc ? '<img src="' + qrSrc + '" width="180" height="180" />' : '',
        '<div class="info">',
        '<p><strong>Name:</strong> ' + name + '</p>',
        '<p><strong>Company:</strong> ' + company + '</p>',
        '<p><strong>Meeting:</strong> ' + meet + '</p>',
        '<p><strong>Purpose:</strong> ' + purpose + '</p>',
        '</div>',
        '<div class="pass">' + pass + '</div>',
        '<p style="font-size:12px;color:#94a3b8;margin-top:12px;">Show this pass at entry/exit</p>',
        '</div>',
        '<' + '/body' + '>' + '<' + '/html' + '>'
    ];
    w.document.write(parts.join(''));
    w.document.close();
    w.print();
}

// Dropdown change → redirect to that visitor's page
function onVisitorChange(id) {
    if (id && parseInt(id) !== window.VISITOR_ID) {
        window.location.href = '/Visitor/GenerateQR/' + id;
    }
}

// Generate QR button click
function generateQR() {
    const sel = document.getElementById('visitorSelect');
    generateQRForVisitor((sel && sel.value) ? parseInt(sel.value) : window.VISITOR_ID);
}

// Generate QR via API
function generateQRForVisitor(visitorId) {
    const btn = document.getElementById('genBtn');
    if (!btn || btn.disabled) return;
    btn.disabled = true; btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Generating...';
    fetch('/api/qci/generate/' + visitorId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
    })
    .then(r => {
        if (!r.ok) return r.json().then(e => Promise.reject(e.error || 'Error ' + r.status));
        return r.json();
    })
    .then(res => {
        if (res.error) throw res.error;
        renderQR(res.passNumber);
        document.getElementById('qrStatus').innerHTML =
            '<span class="gqr-status-ok"><i class="fas fa-circle-check"></i> QR generated — Pass: ' + res.passNumber + '</span>';
        btn.innerHTML = '<i class="fas fa-qrcode"></i> Generate QR';
        btn.classList.add('gqr-btn-disabled');
        btn.title = 'QR already generated for this visit';
        // Add View QR button if not already there
        if (!document.getElementById('viewQRBtn')) {
            const vb = document.createElement('button');
            vb.id = 'viewQRBtn'; vb.className = 'btn-add gqr-btn-sm';
            vb.innerHTML = '<i class="fas fa-eye"></i> View QR';
            vb.onclick = function() {
                openQRModal(res.passNumber, (document.getElementById('vpName')?.textContent || '').trim());
            };
            btn.insertAdjacentElement('afterend', vb);
        }
        
        // Add Scan QR for Check-In button
        const scanBtn = document.createElement('a');
        scanBtn.href = '/Visit/ScanQR';
        scanBtn.className = 'btn-add gqr-btn-sm';
        scanBtn.style.background = '#16a34a';
        scanBtn.style.textDecoration = 'none';
        scanBtn.innerHTML = '<i class="fas fa-camera"></i> Scan QR for Check-In';
        
        const viewBtn = document.getElementById('viewQRBtn');
        if (viewBtn) {
            viewBtn.insertAdjacentElement('afterend', scanBtn);
        }
        
        showToast('QR generated — Pass: ' + res.passNumber);
        openQRModal(res.passNumber, (document.getElementById('vpName')?.textContent || '').trim());
    })
    .catch(function(err) {
        showToast('Error: ' + (typeof err === 'string' ? err : 'Server error'));
        btn.disabled = false; btn.innerHTML = '<i class="fas fa-qrcode"></i> Generate QR';
    });
}

// Modal close handlers
document.addEventListener('keydown', function(e) { if (e.key === 'Escape') closeQRModal(); });
document.addEventListener('DOMContentLoaded', function() {
    const m = document.getElementById('qrViewModal');
    if (m) m.addEventListener('click', function(e) { if (e.target === m) closeQRModal(); });
});
