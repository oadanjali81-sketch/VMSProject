/**
 * VISION GLOBAL UTILITIES
 * Single-instance modal management with full backdrop cleanup.
 */

/** Internal: purge all stale .modal-backdrop elements from DOM */
function _purgeBackdrops() {
    document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
}

/** Internal: get or create a Bootstrap Modal instance (never duplicate) */
var getModal = function (id) {
    var el = document.getElementById(id);
    if (!el) return null;
    var existing = bootstrap.Modal.getInstance(el);
    if (existing) return existing;
    return new bootstrap.Modal(el, { backdrop: true, keyboard: true });
};

/**
 * Open a modal by ID. Ensures:
 * - All currently-open modals are hidden first
 * - All stale backdrops are purged
 * - body.modal-open is cleaned up before re-opening
 */
window.openModal = function (id) {
    // 1. Hide any currently visible modals
    document.querySelectorAll('.modal.show').forEach(m => {
        const inst = bootstrap.Modal.getInstance(m);
        if (inst) {
            inst.hide();
            m.classList.remove('show');
            m.style.display = 'none';
            m.setAttribute('aria-hidden', 'true');
            m.removeAttribute('aria-modal');
        }
    });

    // 2. Remove ALL lingering backdrops and body classes
    _purgeBackdrops();
    document.body.classList.remove('modal-open');
    document.body.style.overflow = '';
    document.body.style.paddingRight = '';

    // 3. Open the requested modal
    var m = getModal(id);
    if (m) {
        m.show();
    } else {
        console.warn('[VMS] Modal element not found: #' + id);
    }
};

/**
 * Close a modal by ID and fully clean up the DOM.
 */
window.closeModal = function (id) {
    var el = document.getElementById(id);
    if (!el) return;
    var m = bootstrap.Modal.getInstance(el);
    if (m) {
        m.hide();
    } else {
        // Force-hide if no instance exists
        el.classList.remove('show');
        el.style.display = 'none';
        el.setAttribute('aria-hidden', 'true');
        el.removeAttribute('aria-modal');
        _purgeBackdrops();
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }
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

    if (typeof openModal === 'function') openModal('deleteModal');
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

// Global: purge ALL backdrops + restore scroll whenever any modal hides
document.addEventListener('hidden.bs.modal', function () {
    if (document.querySelectorAll('.modal.show').length === 0) {
        _purgeBackdrops();
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }
});
