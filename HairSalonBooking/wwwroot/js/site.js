// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", function () {
    const confirmForms = document.querySelectorAll("form[data-confirm]");

    confirmForms.forEach(form => {
        form.addEventListener("submit", function (e) {
            const submitter = e.submitter;
            const message = submitter?.getAttribute("data-confirm")
                || form.getAttribute("data-confirm");

            if (message && !confirm(message)) {
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

    setupFilterTables();
    setupSelectionScopes();
    setupBulkEditForms();
});

function setupFilterTables() {
    const filterTables = document.querySelectorAll("[data-filter-table]");

    filterTables.forEach(table => {
        const filters = Array.from(table.querySelectorAll("[data-filter-key]"));
        const rows = Array.from(table.querySelectorAll("[data-filter-row]"));
        const emptyState = table.querySelector("[data-empty-state]");
        const visibleCount = table.querySelector("[data-visible-count]");
        const clearButton = table.querySelector("[data-clear-table-filters]");

        const normalize = value => (value ?? "").toString().trim().toLocaleLowerCase("pl-PL");

        const applyFilters = () => {
            let shownRows = 0;

            rows.forEach(row => {
                const matches = filters.every(filter => {
                    const filterValue = normalize(filter.value);
                    if (!filterValue) {
                        return true;
                    }

                    const rowValue = normalize(row.getAttribute(`data-filter-${filter.dataset.filterKey}`));
                    const exactMatch = filter.dataset.filterMatch === "exact";

                    return exactMatch ? rowValue === filterValue : rowValue.includes(filterValue);
                });

                row.hidden = !matches;

                if (matches) {
                    shownRows++;
                }
            });

            if (visibleCount) {
                visibleCount.textContent = shownRows.toString();
            }

            if (emptyState) {
                emptyState.hidden = shownRows !== 0;
            }

            table.dispatchEvent(new CustomEvent("filters:changed", { bubbles: true }));
        };

        filters.forEach(filter => {
            filter.addEventListener("input", applyFilters);
            filter.addEventListener("change", applyFilters);
        });

        if (clearButton) {
            clearButton.addEventListener("click", function () {
                filters.forEach(filter => {
                    filter.value = "";
                });

                applyFilters();
            });
        }

        applyFilters();
    });
}

function setupSelectionScopes() {
    const scopes = document.querySelectorAll("[data-selection-scope]");

    scopes.forEach(scope => {
        const selectAll = scope.querySelector("[data-select-all]");
        const selectedCount = scope.querySelector("[data-selected-count]");
        const actionButtons = Array.from(scope.querySelectorAll("[data-requires-selection]"));
        const items = Array.from(scope.querySelectorAll("[data-select-item]"));

        const visibleItems = () => items.filter(item => {
            const row = item.closest("tr");
            return row && !row.hidden;
        });

        const updateState = () => {
            const checkedItems = items.filter(item => item.checked);
            const checkedVisibleItems = visibleItems().filter(item => item.checked);

            if (selectedCount) {
                selectedCount.textContent = checkedItems.length.toString();
            }

            actionButtons.forEach(button => {
                button.disabled = checkedItems.length === 0;
            });

            items.forEach(item => {
                const row = item.closest("tr");
                if (row) {
                    row.classList.toggle("admin-row-selected", item.checked);
                }
            });

            if (!selectAll) {
                return;
            }

            const currentlyVisibleItems = visibleItems();
            selectAll.disabled = currentlyVisibleItems.length === 0;
            selectAll.checked = currentlyVisibleItems.length > 0 && checkedVisibleItems.length === currentlyVisibleItems.length;
            selectAll.indeterminate = checkedVisibleItems.length > 0 && checkedVisibleItems.length < currentlyVisibleItems.length;
        };

        items.forEach(item => {
            item.addEventListener("change", updateState);
        });

        if (selectAll) {
            selectAll.addEventListener("change", function () {
                const shouldCheck = selectAll.checked;

                visibleItems().forEach(item => {
                    item.checked = shouldCheck;
                });

                updateState();
            });
        }

        scope.addEventListener("filters:changed", updateState);
        updateState();
    });
}

function setupBulkEditForms() {
    const bulkEditForms = document.querySelectorAll("[data-bulk-edit-form]");

    bulkEditForms.forEach(form => {
        const applyService = form.querySelector("[data-bulk-apply='service']");
        const applyStatus = form.querySelector("[data-bulk-apply='status']");
        const shiftMinutesInput = form.querySelector("[data-bulk-shift-minutes]");
        const shiftButton = form.querySelector("[data-bulk-shift-button]");
        const serviceInputs = form.querySelectorAll("[data-bulk-service]");
        const statusInputs = form.querySelectorAll("[data-bulk-status]");
        const startInputs = form.querySelectorAll("[data-bulk-start]");
        const endInputs = form.querySelectorAll("[data-bulk-end]");

        if (applyService) {
            applyService.addEventListener("change", function () {
                if (!applyService.value) {
                    return;
                }

                serviceInputs.forEach(input => {
                    input.value = applyService.value;
                });
            });
        }

        if (applyStatus) {
            applyStatus.addEventListener("change", function () {
                if (applyStatus.value === "") {
                    return;
                }

                const isBooked = applyStatus.value === "true";

                statusInputs.forEach(input => {
                    input.checked = isBooked;
                });
            });
        }

        if (shiftButton && shiftMinutesInput) {
            shiftButton.addEventListener("click", function () {
                const shiftMinutes = Number.parseInt(shiftMinutesInput.value, 10);

                if (Number.isNaN(shiftMinutes) || shiftMinutes === 0) {
                    return;
                }

                startInputs.forEach(input => shiftDateTimeInput(input, shiftMinutes));
                endInputs.forEach(input => shiftDateTimeInput(input, shiftMinutes));
                shiftMinutesInput.value = "";
            });
        }
    });
}

function shiftDateTimeInput(input, minutes) {
    if (!input.value) {
        return;
    }

    const date = new Date(input.value);
    if (Number.isNaN(date.getTime())) {
        return;
    }

    date.setMinutes(date.getMinutes() + minutes);
    input.value = formatDateTimeLocal(date);
}

function formatDateTimeLocal(date) {
    const pad = value => value.toString().padStart(2, "0");

    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
