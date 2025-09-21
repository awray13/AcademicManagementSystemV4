// Reports Index Page JavaScript

// Global variables
let currentReportType = '';
let currentItemId = null;

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeReportsPage();
});

// Main initialization function
function initializeReportsPage() {
    initializeTooltips();
    initializeFormValidation();
    attachEventListeners();
    
    console.log('Reports page initialized');
}

// Initialize Bootstrap tooltips
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Initialize form validation
function initializeFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });
}

// Attach event listeners
function attachEventListeners() {
    // Download from preview modal
    const downloadBtn = document.getElementById('downloadFromPreview');
    if (downloadBtn) {
        downloadBtn.addEventListener('click', function() {
            const modal = bootstrap.Modal.getInstance(document.getElementById('previewModal'));
            modal.hide();
            generateReport(currentReportType, currentItemId, 'txt');
        });
    }

    // Modal event listeners
    const termModal = document.getElementById('termSelectionModal');
    if (termModal) {
        termModal.addEventListener('hidden.bs.modal', function() {
            document.getElementById('termSelector').value = '';
        });
    }

    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        // Ctrl/Cmd + G for quick progress report
        if ((e.ctrlKey || e.metaKey) && e.key === 'g') {
            e.preventDefault();
            generateReport('progress', null, 'txt');
        }
        
        // Escape to close modals
        if (e.key === 'Escape') {
            const openModals = document.querySelectorAll('.modal.show');
            openModals.forEach(modal => {
                const modalInstance = bootstrap.Modal.getInstance(modal);
                if (modalInstance) {
                    modalInstance.hide();
                }
            });
        }
    });
}

// Generate report function
function generateReport(reportType, itemId = null, format = 'txt') {
    showLoadingState(true);
    
    try {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = getReportUrl(reportType);
        
        // Add anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            const tokenInput = document.createElement('input');
            tokenInput.type = 'hidden';
            tokenInput.name = '__RequestVerificationToken';
            tokenInput.value = token.value;
            form.appendChild(tokenInput);
        }

        // Add format parameter
        const formatInput = document.createElement('input');
        formatInput.type = 'hidden';
        formatInput.name = 'format';
        formatInput.value = format;
        form.appendChild(formatInput);

        // Add item ID if provided
        if (itemId) {
            const idInput = document.createElement('input');
            idInput.type = 'hidden';
            idInput.name = getIdParameterName(reportType);
            idInput.value = itemId;
            form.appendChild(idInput);
        }

        document.body.appendChild(form);
        form.submit();
        
        // Clean up
        setTimeout(() => {
            if (form.parentNode) {
                document.body.removeChild(form);
            }
            showLoadingState(false);
        }, 1000);
        
        showAlert(`Generating ${reportType} report...`, 'info');
        
    } catch (error) {
        console.error('Error generating report:', error);
        showAlert('Error generating report. Please try again.', 'danger');
        showLoadingState(false);
    }
}

// Generate term report
function generateTermReport() {
    const termId = document.getElementById('termSelector').value;
    if (!termId) {
        showAlert('Please select a term first.', 'warning');
        return;
    }

    const modal = bootstrap.Modal.getInstance(document.getElementById('termSelectionModal'));
    modal.hide();

    generateReport('term', termId, 'txt');
}

// Export all reports
function exportAllReports() {
    const format = document.getElementById('exportFormat').value;
    const modal = bootstrap.Modal.getInstance(document.getElementById('exportAllModal'));
    modal.hide();

    showAlert('Preparing comprehensive report...', 'info');
    generateReport('exportall', null, format);
}

// Preview report
function previewReport(reportType, itemId = null) {
    currentReportType = reportType;
    currentItemId = itemId;

    const modal = new bootstrap.Modal(document.getElementById('previewModal'));
    modal.show();

    // Reset preview content
    resetPreviewContent();

    // Make AJAX request for preview
    fetchReportPreview(reportType, itemId);
}

// Reset preview modal content
function resetPreviewContent() {
    document.getElementById('previewContent').innerHTML = `
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading preview...</span>
            </div>
            <p class="mt-3 text-muted">Generating preview...</p>
        </div>
    `;
    document.getElementById('downloadFromPreview').style.display = 'none';
}

// Fetch report preview via AJAX
async function fetchReportPreview(reportType, itemId = null) {
    try {
        const response = await fetch('/Reports/PreviewReport', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                reportType: reportType,
                itemId: itemId
            })
        });

        const data = await response.json();
        displayPreviewResult(data, reportType);
        
    } catch (error) {
        console.error('Preview error:', error);
        document.getElementById('previewContent').innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>
                Error loading preview: ${error.message}
            </div>
        `;
    }
}

// Display preview result
function displayPreviewResult(data, reportType) {
    const previewContent = document.getElementById('previewContent');
    
    if (data.success) {
        previewContent.innerHTML = `
            <div class="alert alert-info mb-3">
                <div class="row text-center">
                    <div class="col-md-4">
                        <strong>Report Size:</strong><br>
                        <span class="h6 text-primary">${formatBytes(data.fullSize)}</span>
                    </div>
                    <div class="col-md-4">
                        <strong>Lines:</strong><br>
                        <span class="h6 text-success">${data.estimatedLines.toLocaleString()}</span>
                    </div>
                    <div class="col-md-4">
                        <strong>Type:</strong><br>
                        <span class="h6 text-info">${reportType.toUpperCase()}</span>
                    </div>
                </div>
            </div>
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h6 class="mb-0">Preview (First 1000 characters)</h6>
                    <small class="text-muted">Scroll to view more</small>
                </div>
                <div class="card-body p-0">
                    <pre class="bg-light p-3 m-0" style="max-height: 400px; overflow-y: auto; font-size: 0.875rem; line-height: 1.4; border-radius: 0 0 0.375rem 0.375rem;">${escapeHtml(data.preview)}</pre>
                </div>
            </div>
        `;
        document.getElementById('downloadFromPreview').style.display = 'inline-block';
    } else {
        previewContent.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>
                ${data.message}
            </div>
        `;
    }
}

// Show/hide loading state
function showLoadingState(show) {
    const buttons = document.querySelectorAll('.btn:not([data-bs-dismiss])');
    buttons.forEach(btn => {
        if (show) {
            btn.disabled = true;
            if (!btn.querySelector('.spinner-border')) {
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>' + btn.innerHTML;
            }
        } else {
            btn.disabled = false;
            const spinner = btn.querySelector('.spinner-border');
            if (spinner) {
                spinner.remove();
            }
        }
    });
}

// Helper functions
function getReportUrl(reportType) {
    const baseUrl = '/Reports';
    switch (reportType.toLowerCase()) {
        case 'term': return `${baseUrl}/GenerateTermReport`;
        case 'progress': return `${baseUrl}/GenerateProgressReport`;
        case 'assessment': return `${baseUrl}/GenerateAssessmentReport`;
        case 'exportall': return `${baseUrl}/ExportAll`;
        default: return `${baseUrl}/GenerateProgressReport`;
    }
}

function getIdParameterName(reportType) {
    switch (reportType.toLowerCase()) {
        case 'term': return 'termId';
        default: return 'id';
    }
}

function formatBytes(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showAlert(message, type = 'info', duration = 5000) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.position-fixed.alert');
    existingAlerts.forEach(alert => alert.remove());
    
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    alertDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px; max-width: 400px;';
    
    const icon = getAlertIcon(type);
    alertDiv.innerHTML = `
        <i class="${icon} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    document.body.appendChild(alertDiv);
    
    // Auto-remove after duration
    setTimeout(() => {
        if (alertDiv.parentNode) {
            const alert = new bootstrap.Alert(alertDiv);
            alert.close();
        }
    }, duration);
}

function getAlertIcon(type) {
    switch (type) {
        case 'success': return 'fas fa-check-circle';
        case 'danger': return 'fas fa-exclamation-triangle';
        case 'warning': return 'fas fa-exclamation-circle';
        case 'info': 
        default: return 'fas fa-info-circle';
    }
}

// Utility function to copy text to clipboard
function copyToClipboard(text) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(() => {
            showAlert('Copied to clipboard!', 'success', 2000);
        }).catch(err => {
            console.error('Failed to copy: ', err);
            showAlert('Failed to copy to clipboard', 'warning');
        });
    } else {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = text;
        document.body.appendChild(textArea);
        textArea.select();
        try {
            //document.execCommand('copy');
            showAlert('Copied to clipboard!', 'success', 2000);
        } catch (err) {
            console.error('Fallback copy failed: ', err);
            showAlert('Failed to copy to clipboard', 'warning');
        }
        document.body.removeChild(textArea);
    }
}

// Export functions for global access
window.ReportsPage = {
    generateReport,
    generateTermReport,
    exportAllReports,
    previewReport,
    showAlert,
    copyToClipboard
};