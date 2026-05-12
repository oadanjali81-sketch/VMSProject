/**
 * VISITOR MANAGEMENT MODULE
 * Handles View, Edit, and Registration interactions.
 */

(function () {
    // ── HELPERS FOR ID TRIGGERS ──
    window.openViewModalFromId = function (id) {
        if (typeof visitorsData === 'undefined' || !visitorsData) return;
        var v = visitorsData.find(x => x.VisitorId == id);
        if (v) openViewModal(v);
    };

    window.openEditModalFromId = function (id) {
        if (typeof visitorsData === 'undefined' || !visitorsData) return;
        var v = visitorsData.find(x => x.VisitorId == id);
        if (v) openEditModal(v);
    };

    // ── ADD VISITOR ──
    window.openAddModal = function () {
        if (typeof openModal === 'function') openModal('modalAdd');
        else console.error("openModal function not found");
    };

    // ── VIEW VISITOR ──
    function openViewModal(v) {
        document.getElementById('v_name_top').textContent = v.Name || 'Anonymous';
        document.getElementById('v_company_top').textContent = (v.CompanyName || 'INDIVIDUAL').toUpperCase();

        const setVal = (id, val) => {
            const el = document.getElementById(id);
            if (el && el.querySelector('span')) el.querySelector('span').textContent = val;
        };

        setVal('v_email', v.Email || '—');
        setVal('v_phone', v.Phone || '—');
        setVal('v_host', v.WhomeToMeet || '—');
        setVal('v_purpose', v.Purpose || '—');

        const photo = document.getElementById('v_photo');
        const initial = document.getElementById('v_initial');
        if (v.CapturePhoto) {
            photo.src = v.CapturePhoto;
            photo.classList.remove('d-none');
            initial.classList.add('d-none');
        } else {
            photo.classList.add('d-none');
            initial.classList.remove('d-none');
            initial.textContent = (v.Name || '?').charAt(0).toUpperCase();
        }

        document.getElementById('v_edit_btn').onclick = () => {
            if (typeof closeModal === 'function') closeModal('modalView');
            openEditModal(v);
        };

        if (typeof openModal === 'function') openModal('modalView');
    }

    // ── EDIT VISITOR ──
    function openEditModal(v) {
        document.getElementById('e_id').value = v.VisitorId;
        document.getElementById('e_name').value = v.Name || '';
        document.getElementById('e_email').value = v.Email || '';
        document.getElementById('e_phone').value = v.Phone || '';
        document.getElementById('e_vehicle').value = v.VehicleNumber || '';
        document.getElementById('e_company').value = v.CompanyName || '';
        document.getElementById('e_dept').value = v.Department || '';
        if (typeof openModal === 'function') openModal('modalEdit');
    }

    // ── CAMERA LOGIC ──
    let camStream = null;
    window.openCameraModal = function () {
        if (typeof openModal === 'function') openModal('cameraModal');
        const video = document.getElementById('cameraVideo');
        const status = document.getElementById('cameraStatus');
        navigator.mediaDevices.getUserMedia({ video: { facingMode: "user" } })
            .then(stream => {
                camStream = stream;
                video.srcObject = stream;
                status.classList.add('d-none');
            })
            .catch(err => {
                status.innerHTML = `<i class="fas fa-exclamation-circle text-danger mb-2"></i><p class="small">Camera access denied.</p>`;
            });
    }

    window.closeCameraModal = function () {
        if (camStream) { camStream.getTracks().forEach(track => track.stop()); camStream = null; }
        const modalEl = document.getElementById('cameraModal');
        if (typeof bootstrap === 'undefined') return;
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }

    window.capturePhoto = function () {
        const video = document.getElementById('cameraVideo');
        const canvas = document.getElementById('cameraCanvas');
        const preview = document.getElementById('add_photo_preview');
        const fileInput = document.getElementById('add_photo_file');

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext('2d').drawImage(video, 0, 0);
        const dataUrl = canvas.toDataURL('image/jpeg');

        preview.innerHTML = `<img src="${dataUrl}" style="width:100%; height:100%; object-fit:cover;" />`;

        fetch(dataUrl).then(res => res.blob()).then(blob => {
            const file = new File([blob], "captured_visitor.jpg", { type: "image/jpeg" });
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(file);
            fileInput.files = dataTransfer.files;
        });
        window.closeCameraModal();
    }

    window.handlePhotoUpload = function (input) {
        const preview = document.getElementById('add_photo_preview');
        if (input.files && input.files[0]) {
            const reader = new FileReader();
            reader.onload = (e) => {
                preview.innerHTML = `<img src="${e.target.result}" style="width:100%; height:100%; object-fit:cover;" />`;
            };
            reader.readAsDataURL(input.files[0]);
        }
    }

    // ── QR PASS ──
    let _currentQRData = { name: '', code: '', company: '' };
    window.openQRPassModal = function (id, name, qrCode, photo, company) {
        _currentQRData = { name, code: qrCode, company: company || 'INDIVIDUAL' };
        document.getElementById('qrp_name').textContent = name;
        document.getElementById('qrp_company').textContent = (company || 'INDIVIDUAL').toUpperCase();
        document.getElementById('qrp_token').textContent = qrCode;

        const photoImg = document.getElementById('qrp_photo');
        const initial = document.getElementById('qrp_initial');
        if (photo) {
            photoImg.src = photo;
            photoImg.classList.remove('d-none');
            initial.classList.add('d-none');
        } else {
            photoImg.classList.add('d-none');
            initial.classList.remove('d-none');
            initial.textContent = (name || '?').charAt(0).toUpperCase();
        }

        document.getElementById('qrp_img').src = `https://api.qrserver.com/v1/create-qr-code/?size=400x400&data=${encodeURIComponent(qrCode)}`;
        if (typeof openModal === 'function') openModal('modalQRPass');
    }

    window.printCurrentQR = function () {
        if (!_currentQRData.code) return;
        const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=400x400&data=${encodeURIComponent(_currentQRData.code)}`;
        const w = window.open('', '_blank');
        w.document.write(`<html><body style="font-family:'Inter',sans-serif;text-align:center;padding:40px;">
            <div style="border:2px solid #6366f1;border-radius:24px;padding:30px;display:inline-block;width:300px;">
                <h2 style="margin:0;color:#1e1e2d;">${_currentQRData.name}</h2>
                <p style="color:#6366f1;font-weight:bold;text-transform:uppercase;margin:5px 0 20px;">${_currentQRData.company}</p>
                <img src="${qrUrl}" width="200" style="margin-bottom:20px;"/>
                <div style="background:#f1f5f9;padding:10px;border-radius:10px;font-family:monospace;font-weight:bold;">${_currentQRData.code}</div>
            </div>
            <script>setTimeout(()=> {window.print(); window.close();},500);</` + `script>
        </body></html>`);
        w.document.close();
    }

    window.shareQRDirect = function () {
        const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=400x400&data=${encodeURIComponent(_currentQRData.code)}`;
        if (navigator.share) navigator.share({ title: 'Visitor Pass', url: qrUrl });
        else window.open(qrUrl, '_blank');
    }
})();
