/**
 * DEPARTMENT MANAGEMENT MODULE
 * Handles View and Edit interactions for organizational units.
 */

(function() {
    window.openEditDepartmentModal = async function(id) {
        try {
            const response = await fetch(`/User/GetDepartment/${id}`);
            const data = await response.json();
            document.getElementById('edit_DeptId').value = data.id || data.DepartmentId || data.Id;
            document.getElementById('edit_DeptName').value = data.name || data.DepartmentName || data.Name;
            if (typeof openModal === 'function') openModal('editDepartmentModal');
        } catch (err) { 
            console.error("Error loading department for edit:", err); 
        }
    };

    window.openViewDepartmentModal = async function(id) {
        try {
            const response = await fetch(`/User/GetDepartment/${id}`);
            const data = await response.json();
            document.getElementById('view_DeptName').textContent = data.name || data.DepartmentName || data.Name;
            const countEl = document.getElementById('view_DeptCount');
            if (countEl && countEl.querySelector('span')) {
                countEl.querySelector('span').textContent = data.employeeCount || data.EmployeeCount || 0;
            }
            document.getElementById('view_DeptEditBtn').onclick = () => {
                if (typeof closeModal === 'function') closeModal('viewDepartmentModal');
                window.openEditDepartmentModal(id);
            };
            if (typeof openModal === 'function') openModal('viewDepartmentModal');
        } catch (err) { 
            console.error("Error loading department for view:", err); 
        }
    };
})();
