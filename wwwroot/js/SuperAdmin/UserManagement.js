// ── SUPER ADMIN USER MANAGEMENT LOGIC ──

function openSAModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.style.display = 'flex';
}

function closeSAModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.style.display = 'none';
}

// ── MODAL 1: ✏ EDIT USER ──
function openUserEditModal(user) {
    document.getElementById('editUserId').value = user.CompanyId;
    document.getElementById('editAdminName').value = user.AdminName || '';
    document.getElementById('editAdminEmail').value = user.AdminEmail || '';
    
    const isMaster = user.AdminEmail.toLowerCase() === 'anjali@gmail.com';

    // Set Role Radio
    const roleRadios = document.getElementsByName('role');
    roleRadios.forEach(r => {
        if (r.value === user.Role) r.checked = true;
        r.disabled = isMaster; // Prohibit self-demotion
    });

    // Set Status Radio
    const statusRadios = document.getElementsByName('isActive');
    statusRadios.forEach(s => {
        const val = s.value === 'true';
        if (val === user.IsActive) s.checked = true;
        s.disabled = isMaster; // Prohibit self-deactivation
    });

    openSAModal('saUserEditModal');
}

// ── MODAL 2: ❌ DELETE USER ──
function openUserDeleteModal(id, name) {
    document.getElementById('saDeleteUserForm').action = `/SuperAdmin/DeleteUser/${id}`;
    document.getElementById('saDeleteUserName').textContent = `User: ${name}`;
    openSAModal('saUserDeleteModal');
}

// ── MODAL 3: 🔁 STATUS TOGGLE ──
function openStatusToggleModal(id, name, currentStatus) {
    document.getElementById('saToggleStatusForm').action = `/SuperAdmin/ToggleStatus/${id}`;
    document.getElementById('saToggleUserName').textContent = name;
    document.getElementById('saCurrentStatusLabel').textContent = currentStatus ? 'Active' : 'Inactive';
    document.getElementById('saNextStatusLabel').textContent = currentStatus ? 'Inactive' : 'Active';
    openSAModal('saStatusToggleModal');
}

$(document).ready(function() {
    // Close on overlay click
    $('.sa-modal-overlay').on('click', function(e) {
        if ($(e.target).hasClass('sa-modal-overlay')) {
            $(this).hide();
        }
    });

    // Escape key
    $(document).keydown(function(e) {
        if (e.key === "Escape") $('.sa-modal-overlay').hide();
    });
});
