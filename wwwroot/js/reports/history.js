// Search and filter functionality
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchInput');
    const statusFilter = document.getElementById('statusFilter');
    const dateFilter = document.getElementById('dateFilter');
    const sortFilter = document.getElementById('sortFilter');

    [searchInput, statusFilter, dateFilter, sortFilter].forEach(element => {
        element?.addEventListener('input', filterAndSortTable);
        element?.addEventListener('change', filterAndSortTable);
    });
});

function filterAndSortTable() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const statusFilter = document.getElementById('statusFilter').value;
    const dateFilter = document.getElementById('dateFilter').value;
    const sortFilter = document.getElementById('sortFilter').value;

    const table = document.getElementById('historyTable');
    const rows = Array.from(table.querySelectorAll('tbody tr'));

    // Filter rows
    const filteredRows = rows.filter(row => {
        const type = row.dataset.type.toLowerCase();
        const status = row.dataset.status;
        const date = new Date(row.dataset.date);
        const now = new Date();

        // Search filter
        if (searchTerm && !type.includes(searchTerm)) return false;

        // Status filter
        if (statusFilter && status !== statusFilter) return false;

        // Date filter
        if (dateFilter) {
            const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
            const weekAgo = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);
            const monthAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
            const quarterAgo = new Date(today.getTime() - 90 * 24 * 60 * 60 * 1000);

            switch (dateFilter) {
                case 'today':
                    if (date < today) return false;
                    break;
                case 'week':
                    if (date < weekAgo) return false;
                    break;
                case 'month':
                    if (date < monthAgo) return false;
                    break;
                case 'quarter':
                    if (date < quarterAgo) return false;
                    break;
            }
        }

        return true;
    });

    // Sort rows
    filteredRows.sort((a, b) => {
        switch (sortFilter) {
            case 'date-desc':
                return new Date(b.dataset.date) - new Date(a.dataset.date);
            case 'date-asc':
                return new Date(a.dataset.date) - new Date(b.dataset.date);
            case 'type-asc':
                return a.dataset.type.localeCompare(b.dataset.type);
            case 'size-desc':
                // Simple size comparison (would need more sophisticated parsing in real app)
                return parseFloat(b.querySelector('td:nth-child(3)').textContent) -
                    parseFloat(a.querySelector('td:nth-child(3)').textContent);
            default:
                return 0;
        }
    });

    // Update table
    const tbody = table.querySelector('tbody');
    tbody.innerHTML = '';
    filteredRows.forEach(row => tbody.appendChild(row));

    // Show/hide "no results" message
    if (filteredRows.length === 0) {
        tbody.innerHTML = `
                    <tr>
                        <td colspan="5" class="text-center py-4">
                            <i class="fas fa-search fa-2x text-muted mb-2"></i>
                            <p class="text-muted mb-0">No reports match your filter criteria.</p>
                        </td>
                    </tr>
                `;
    }
}

function clearFilters() {
    document.getElementById('searchInput').value = '';
    document.getElementById('statusFilter').value = '';
    document.getElementById('dateFilter').value = '';
    document.getElementById('sortFilter').value = 'date-desc';
    filterAndSortTable();
}

function clearHistory() {
    if (confirm('Are you sure you want to clear all report history? This action cannot be undone.')) {
        // In a real application, this would make an AJAX call to delete history
        alert('History clearing functionality would be implemented here.');
    }
}

function regenerateReport(reportType) {
    // Redirect to appropriate report generation
    const type = reportType.toLowerCase().replace(' report', '');
    switch (type) {
        case 'progress':
            window.location.href = '@Url.Action("Index")';
            break;
        case 'assessment':
            window.location.href = '@Url.Action("Index")';
            break;
        case 'term':
            window.location.href = '@Url.Action("Index")';
            break;
        case 'custom':
            window.location.href = '@Url.Action("Custom")';
            break;
        default:
            window.location.href = '@Url.Action("Index")';
    }
}

function removeFromHistory(reportType, generatedDate) {
    if (confirm(`Are you sure you want to remove this ${reportType} from history?`)) {
        // In a real application, this would make an AJAX call to remove the item
        alert('Remove from history functionality would be implemented here.');
    }
}

function showReportDetails(reportType, generatedDate, fileSize) {
    const content = document.getElementById('reportDetailsContent');
    const date = new Date(generatedDate);

    content.innerHTML = `
                <div class="row">
                    <div class="col-md-6">
                        <h6 class="fw-bold">Report Information</h6>
                        <table class="table table-sm">
                            <tr>
                                <td class="fw-semibold">Type:</td>
                                <td>${reportType}</td>
                            </tr>
                            <tr>
                                <td class="fw-semibold">Generated:</td>
                                <td>${date.toLocaleString()}</td>
                            </tr>
                            <tr>
                                <td class="fw-semibold">File Size:</td>
                                <td>${fileSize}</td>
                            </tr>
                            <tr>
                                <td class="fw-semibold">Status:</td>
                                <td><span class="badge bg-success">Completed</span></td>
                            </tr>
                        </table>
                    </div>
                    <div class="col-md-6">
                        <h6 class="fw-bold">Report Options</h6>
                        <p class="text-muted small">This report contains academic data for the specified time period.</p>
                        <div class="d-grid gap-2">
                            <button class="btn btn-outline-primary btn-sm" onclick="regenerateReport('${reportType}')">
                                <i class="fas fa-redo me-1"></i>Generate Again
                            </button>
                            <button class="btn btn-outline-info btn-sm" onclick="shareReport('${reportType}')">
                                <i class="fas fa-share me-1"></i>Share Report
                            </button>
                        </div>
                    </div>
                </div>
            `;
}

function shareReport(reportType) {
    alert('Share functionality would be implemented here.');
}