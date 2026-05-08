/**
 * EMPLOYEE MANAGEMENT MODULE
 * Handles View and Edit interactions for internal staff.
 */

(function() {
    window.openEditEmployeeModal = async function(id) {
        try {
            const response = await fetch(`/User/GetEmployee/${id}`);
            const data = await response.json();
            document.getElementById('edit_EmployeeId').value = data.id || data.EmployeeId;
            document.getElementById('edit_Name').value = data.name || data.Name;
            document.getElementById('edit_Phone').value = data.phone || data.Phone || '';
            document.getElementById('edit_Email').value = data.email || data.Email || '';
            document.getElementById('edit_Designation').value = data.designation || data.Designation || '';
            document.getElementById('edit_DepartmentId').value = data.departmentId || data.DepartmentId;
            
            const previewWrap = document.getElementById('edit_photo_preview_container');
            const previewImg = document.getElementById('edit_photo_preview');
            const path = data.photoPath || data.PhotoPath;
            if (path) { 
                previewImg.src = path; 
                previewWrap.style.display = 'block'; 
            } else { 
                previewWrap.style.display = 'none'; 
            }
            if (typeof openModal === 'function') openModal('editEmployeeModal');
        } catch (err) { 
            console.error("Error loading employee for edit:", err); 
        }
    };

    window.openViewEmployeeModal = async function(id) {
        try {
            const response = await fetch(`/User/GetEmployee/${id}`);
            const data = await response.json();
            const name = data.name || data.Name || 'Unknown';
            document.getElementById('view_name').textContent = name;
            document.getElementById('view_designation').textContent = (data.designation || data.Designation || 'Staff').toUpperCase();
            document.getElementById('view_id').querySelector('span').textContent = `#EMP-100${id}`;
            document.getElementById('view_email').querySelector('span').textContent = data.email || data.Email || '—';
            document.getElementById('view_phone').querySelector('span').textContent = data.phone || data.Phone || '—';
            document.getElementById('view_department').querySelector('span').textContent = data.departmentName || data.DepartmentName || 'General';
            
            const photo = document.getElementById('view_photo');
            const initial = document.getElementById('view_initial');
            const path = data.photoPath || data.PhotoPath;
            if (path) { 
                photo.src = path; 
                photo.classList.remove('d-none'); 
                initial.classList.add('d-none'); 
            } else { 
                photo.classList.add('d-none'); 
                initial.classList.remove('d-none'); 
                initial.textContent = name.charAt(0).toUpperCase(); 
            }
            
            document.getElementById('view_edit_btn').onclick = () => { 
                if (typeof closeModal === 'function') closeModal('viewEmployeeModal'); 
                window.openEditEmployeeModal(id); 
            };
            if (typeof openModal === 'function') openModal('viewEmployeeModal');
        } catch (err) { 
            console.error("Error loading employee for view:", err); 
        }
    };
})();
