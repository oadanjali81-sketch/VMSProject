/* ======================================
   EDIT PAGE COMPLETE SCRIPT (FIXED)
====================================== */

document.addEventListener("DOMContentLoaded", function () {

    const progressBar = document.getElementById("progressBar");

    /* ======================================
       STEP NAVIGATION
    ====================================== */

    window.nextStep = function () {

        const step1 = document.getElementById("step1");

        // ❗ file input ignore validation (EDIT MODE FIX)
        const inputs = step1.querySelectorAll("input:not([type='file']), select");

        for (let input of inputs) {
            if (!input.checkValidity()) {
                input.reportValidity();
                return;
            }
        }

        step1.classList.remove("active");
        document.getElementById("step2").classList.add("active");

        document.getElementById("indicator1").classList.remove("active");
        document.getElementById("indicator2").classList.add("active");

        if (progressBar)
            progressBar.style.width = "100%";
    };


    window.prevStep = function () {

        document.getElementById("step2").classList.remove("active");
        document.getElementById("step1").classList.add("active");

        document.getElementById("indicator2").classList.remove("active");
        document.getElementById("indicator1").classList.add("active");

        if (progressBar)
            progressBar.style.width = "50%";
    };


    /* ======================================
       FILE UPLOAD DISPLAY NAME
    ====================================== */

    window.handleFile = function (input) {

        const formGroup = input.parentElement;
        const fileText = formGroup.querySelector(".file-name");

        if (input.files && input.files.length > 0) {
            formGroup.classList.add("file-filled");
            fileText.textContent = input.files[0].name;
        } else {
            formGroup.classList.remove("file-filled");
            fileText.textContent = " ";
        }
    };


    /* ======================================
       PASSWORD SHOW / HIDE
    ====================================== */

    document.querySelectorAll(".eye").forEach(function (eye) {

        eye.addEventListener("click", function () {

            const input = this.parentElement.querySelector("input");
            const icon = this.querySelector("i");

            if (!input) return;

            if (input.type === "password") {
                input.type = "text";
                icon.classList.remove("fa-eye");
                icon.classList.add("fa-eye-slash");
            }
            else {
                input.type = "password";
                icon.classList.remove("fa-eye-slash");
                icon.classList.add("fa-eye");
            }
        });
    });

    /* ======================================
       SUCCESS MODAL CONTROL
    ====================================== */

    const modal = document.getElementById("successModal");

    window.closeModal = function () {
        if (modal) modal.classList.remove("active");
    };

    window.goToCompanies = function () {
        window.location.href = "/Company/Index";
    };

    /* ======================================
       SUCCESS FLAG CHECK
    ====================================== */

    const successFlag = document.body.getAttribute("data-success");

    if (successFlag === "True") {
        setTimeout(() => {
            if (modal) modal.classList.add("active");
        }, 300);
    }

});