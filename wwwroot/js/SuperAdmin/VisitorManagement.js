// ── SUPER ADMIN VISITOR MANAGEMENT LOGIC ──

function openSAModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.style.display = 'flex';
}

function closeSAModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.style.display = 'none';
}

// ── MODAL 1: 👁 VIEW VISITOR ──
function openSAViewModal(visitor) {
    const content = document.getElementById('saViewContent');
    content.innerHTML = `
        <div style="display:grid; grid-template-columns: 1fr 1fr; gap: 25px; text-align: left;">
            <div>
                <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">NAME</p>
                <p style="margin:0 0 15px; font-weight:800;">${visitor.Name || '—'}</p>
                
                <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">MOBILE</p>
                <p style="margin:0 0 15px; font-weight:800;">${visitor.Phone || '—'}</p>
                
                <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">COMPANY</p>
                <p style="margin:0 0 15px; font-weight:800;">${visitor.CompanyName || 'Guest'}</p>
            </div>
            <div>
                <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">PURPOSE</p>
                <p style="margin:0 0 15px; font-weight:800;">${visitor.Purpose || '—'}</p>
                
                <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">HOST</p>
                <p style="margin:0 0 15px; font-weight:800;">${visitor.HostName || '—'}</p>

                <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">VISITING COMPANY</p>
                <p style="margin:0 0 15px; font-weight:800; color:var(--sa-primary);">${visitor.HostCompany || '—'}</p>
                
                <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">CHECK-IN</p>
                <p style="margin:0 0 15px; font-weight:800;">${visitor.CheckInTime ? new Date(visitor.CheckInTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) : '--'}</p>
            </div>
        </div>
        <div style="margin-top:15px; border-top:1px solid var(--sa-border); padding-top:15px; text-align:left;">
            <p style="margin:0 0 5px; font-weight:700; color:var(--sa-text-muted); font-size:0.75rem;">CURRENT STATUS</p>
            <p style="margin:0; font-weight:800; color:var(--sa-primary);">${visitor.Status || 'Registered'}</p>
        </div>
    `;
    openSAModal('saViewModal');
}

// ── MODAL 2: ✏ EDIT VISITOR ──
function openSAEditModal(visitor) {
    document.getElementById('saEditId').value = visitor.VisitorId;
    document.getElementById('saEditName').value = visitor.Name || '';
    document.getElementById('saEditPhone').value = visitor.Phone || '';
    document.getElementById('saEditCompany').value = visitor.CompanyName || '';
    document.getElementById('saEditPurpose').value = visitor.Purpose || '';
    document.getElementById('saEditHost').value = visitor.WhomeToMeet || '';
    openSAModal('saEditModal');
}

// ── MODAL 3: ❌ DELETE CONFIRMATION ──
function openSADeleteConfirm(id, name) {
    document.getElementById('saDeleteForm').action = `/SuperAdmin/DeleteVisitor/${id}`;
    document.getElementById('saDeleteName').textContent = `Visitor: ${name}`;
    openSAModal('saDeleteModal');
}

// ── MODAL 4: 📷 QR CODE VIEW ──
function openSAQRModal(id, name, qrCode) {
    const wrap = document.getElementById('saQRWrap');
    const token = document.getElementById('saQRToken');
    const dlBtn = document.getElementById('saQRDlBtn');

    if (qrCode) {
        const url = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(qrCode)}`;
        wrap.innerHTML = `<img src="${url}" alt="QR" style="width:100%;height:100%;object-fit:cover;">`;
        token.innerHTML = `<span style="color:var(--sa-text-muted)">Token:</span> ${qrCode}`;
        dlBtn.onclick = () => {
            const a = document.createElement('a');
            a.href = url;
            a.download = `SA_Pass_${qrCode}.png`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        };
        dlBtn.style.display = 'block';
    } else {
        wrap.innerHTML = '<div style="text-align:center;padding:20px;color:var(--sa-text-muted);"><i class="fas fa-qrcode fa-5x" style="display:block;margin-bottom:10px;opacity:0.2;"></i><p>No QR Data</p></div>';
        token.textContent = 'Token: ---';
        dlBtn.style.display = 'none';
    }
    openSAModal('saQRModal');
}

// ── MODAL 5: 🕒 VISIT LOGS / HISTORY ──
function openSALogsModal(id, name) {
    const content = document.getElementById('saLogsContent');
    const hCin = document.getElementById('saHCin');
    const hCout = document.getElementById('saHCout');

    content.innerHTML = '<div style="padding:40px;text-align:center;color:var(--sa-text-muted);"><i class="fas fa-circle-notch fa-spin fa-2x"></i><p>Syncing Logs...</p></div>';
    hCin.textContent = '--';
    hCout.textContent = '--';
    openSAModal('saLogsModal');

    fetch(`/SuperAdmin/GetVisitorHistory/${id}`)
        .then(r => r.json())
        .then(data => {
            if (!data || data.length === 0) {
                content.innerHTML = '<div style="padding:40px;text-align:center;color:var(--sa-text-muted);"><i class="fas fa-history fa-2x" style="opacity:0.2;"></i><p>No visit history found.</p></div>';
                return;
            }

            // Latest Session
            const latest = data[0];
            hCin.textContent = latest.checkIn.split(',')[1].trim();
            hCout.textContent = latest.checkOut !== '--' ? latest.checkOut.split(',')[1].trim() : '--';

            // History Stream
            let html = '';
            data.slice(1).forEach(log => {
                const isOut = log.status === 'Checked Out';
                html += `
                    <div style="padding:15px; border-bottom:1px solid rgba(0,0,0,0.05); display:flex; justify-content:space-between; align-items:center;">
                        <div style="display:flex; align-items:center; gap:12px;">
                            <div style="width:8px; height:8px; border-radius:50%; background:${isOut ? '#10b981' : '#3b82f6'}"></div>
                            <div>
                                <div style="font-weight:800; font-size:0.85rem;">${log.checkIn.split(',')[0]}</div>
                                <div style="font-size:0.75rem; color:var(--sa-text-muted);">In: ${log.checkIn.split(',')[1]}</div>
                            </div>
                        </div>
                        <div style="font-weight:700; font-size:0.75rem; color:${isOut ? '#10b981' : '#3b82f6'}; text-transform:uppercase;">
                            ${isOut ? 'Completed' : log.status}
                        </div>
                    </div>
                `;
            });
            
            if (html === '') html = '<div style="padding:20px; text-align:center; color:var(--sa-text-muted); font-size:0.8rem;">No previous sessions.</div>';
            content.innerHTML = html;
        })
        .catch(err => {
            content.innerHTML = '<p style="color:#ef4444;text-align:center;padding:20px;">Stream Load Failed.</p>';
        });
}

// ── INIT ──
$(document).ready(function() {
    // Escape key handling
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            $('.sa-modal-overlay').hide();
        }
    });

    // Global Pagination for SA Visitor Table
    if (typeof initSAPagination === 'function') {
        initSAPagination({
            tableId: 'saVisitorTable',
            footerId: 'saVisitorFooter',
            pageSize: 10
        });
    }
});
