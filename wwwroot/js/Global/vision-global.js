/**
 * VISION GLOBAL UTILITIES
 */

var getModal = function(id) {
    var el = document.getElementById(id);
    if (!el) return null;
    return bootstrap.Modal.getOrCreateInstance(el);
};

window.openModal = function(id) { 
    var m = getModal(id);
    if (m) m.show();
    else console.warn("Modal not found: " + id);
};

window.closeModal = function(id) { 
    var m = getModal(id);
    if (m) m.hide();
};

/**
 * Global Table Quick Filter
 */
function filterTable(inputId = "tableSearchInput", tableId = "standardTable") {
    const val = document.getElementById(inputId).value.toUpperCase();
    const table = document.getElementById(tableId);
    if (!table) return;
    const rows = table.getElementsByTagName("tr");
    for (let i = 1; i < rows.length; i++) {
        let found = false;
        const cells = rows[i].getElementsByTagName("td");
        for (let j = 0; j < cells.length; j++) {
            if (cells[j] && cells[j].textContent.toUpperCase().indexOf(val) > -1) { found = true; break; }
        }
        rows[i].style.display = found ? "" : "none";
    }
}

let pendingDeleteAction = null;

/**
 * Opens the universal delete confirmation modal
 * @param {HTMLFormElement|string} action - The form to submit or URL to redirect to
 * @param {string} itemName - The name of the item being deleted
 * @param {string} title - Optional custom title
 * @param {string} subtitle - Optional custom subtitle
 */
function openDeleteModal(action, itemName, title = "Delete Item", subtitle = "Action Confirmation Required") {
    pendingDeleteAction = action;
    
    // Update content
    const nameEl = document.getElementById('deleteItemName');
    if (nameEl) nameEl.textContent = itemName;
    
    const titleEl = document.getElementById('deleteModalTitle');
    if (titleEl) titleEl.textContent = title;
    
    const subEl = document.getElementById('deleteModalSubtitle');
    if (subEl) subEl.textContent = subtitle;
    
    // Reset button state
    const btn = document.getElementById('deleteConfirmBtn');
    if (btn) {
        btn.disabled = false;
        btn.innerHTML = '<i class="fas fa-trash"></i> Delete Now';
    }

    const modalEl = document.getElementById('deleteModal');
    if (modalEl) {
        new bootstrap.Modal(modalEl).show();
    }
}

/**
 * Closes the universal delete confirmation modal
 */
function closeDeleteModal() {
    const modalEl = document.getElementById('deleteModal');
    if (modalEl) {
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    }
    pendingDeleteAction = null;
}

/**
 * Executes the pending delete action
 */
function confirmDelete() {
    if (!pendingDeleteAction) return;

    // Show loading state on button
    const btn = document.getElementById('deleteConfirmBtn');
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
    }

    if (typeof pendingDeleteAction === 'string') {
        window.location.href = pendingDeleteAction;
    } else if (pendingDeleteAction.tagName === 'FORM') {
        pendingDeleteAction.submit();
    } else if (pendingDeleteAction instanceof HTMLElement && pendingDeleteAction.closest('form')) {
        pendingDeleteAction.closest('form').submit();
    }
}

// Global Event Listeners (Removed as Bootstrap handles Escape and Backdrop click natively)
