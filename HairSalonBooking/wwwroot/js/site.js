// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", function () {
    const confirmForms = document.querySelectorAll("form[data-confirm]");

    confirmForms.forEach(form => {
        form.addEventListener("submit", function (e) {
            const message = form.getAttribute("data-confirm") || "Czy na pewno?";
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });

    const autoSubmitElements = document.querySelectorAll("[data-auto-submit='true']");

    autoSubmitElements.forEach(element => {
        element.addEventListener("change", function () {
            if (element.form) {
                element.form.submit();
            }
        });
    });
});
