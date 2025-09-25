// Update summary on form changes
document.addEventListener('DOMContentLoaded', function () {
    // Title update
    const titleInput = document.querySelector('#Title');
    titleInput?.addEventListener('input', function () {
        document.getElementById('summaryTitle').textContent = this.value || 'Custom Academic Report';
    });

    // Date updates
    const startDateInput = document.querySelector('#StartDate');
    const endDateInput = document.querySelector('#EndDate');

    startDateInput?.addEventListener('change', function () {
        const date = new Date(this.value);
        document.getElementById('summaryStartDate').textContent = date.toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
        });
    });

    endDateInput?.addEventListener('change', function () {
        const date = new Date(this.value);
        document.getElementById('summaryEndDate').textContent = date.toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
        });
    });

    // Format update
    const formatSelect = document.querySelector('#Format');
    formatSelect?.addEventListener('change', function () {
        const formats = {
            'txt': 'Text Document (.txt)',
            'csv': 'Spreadsheet (.csv)',
            'json': 'Data File (.json)'
        };
        document.getElementById('summaryFormatText').textContent = formats[this.value] || 'Text Document (.txt)';
    });

    // Content checkboxes
    const checkboxes = ['IncludeTerms', 'IncludeCourses', 'IncludeAssessments'];
    checkboxes.forEach(name => {
        const checkbox = document.querySelector(`#${name}`);
        checkbox?.addEventListener('change', updateContentSummary);
    });
});

function updateContentSummary() {
    const content = document.getElementById('summaryContent');
    const items = [
        { id: 'IncludeTerms', label: 'Terms' },
        { id: 'IncludeCourses', label: 'Courses' },
        { id: 'IncludeAssessments', label: 'Assessments' }
    ];

    content.innerHTML = items.map(item => {
        const checked = document.querySelector(`#${item.id}`).checked;
        const iconClass = checked ? 'check text-success' : 'times text-muted';
        return `<li class="${checked ? 'text-success' : 'text-muted'}">
                    <i class="fas fa-${iconClass} me-1"></i>${item.label}
                </li>`;
    }).join('');
}

function applyTemplate(templateType) {
    const now = new Date();
    const threeMonthsAgo = new Date(now.getFullYear(), now.getMonth() - 3, now.getDate());

    switch (templateType) {
        case 'comprehensive':
            document.querySelector('#Title').value = 'Comprehensive Academic Report';
            document.querySelector('#IncludeTerms').checked = true;
            document.querySelector('#IncludeCourses').checked = true;
            document.querySelector('#IncludeAssessments').checked = true;
            break;
        case 'progress':
            document.querySelector('#Title').value = 'Academic Progress Summary';
            document.querySelector('#IncludeTerms').checked = true;
            document.querySelector('#IncludeCourses').checked = true;
            document.querySelector('#IncludeAssessments').checked = false;
            break;
        case 'assessments':
            document.querySelector('#Title').value = 'Assessment Report';
            document.querySelector('#IncludeTerms').checked = false;
            document.querySelector('#IncludeCourses').checked = false;
            document.querySelector('#IncludeAssessments').checked = true;
            break;
        case 'recent':
            document.querySelector('#Title').value = 'Recent Activity Report';
            document.querySelector('#StartDate').value = threeMonthsAgo.toISOString().split('T')[0];
            document.querySelector('#IncludeTerms').checked = true;
            document.querySelector('#IncludeCourses').checked = true;
            document.querySelector('#IncludeAssessments').checked = true;
            break;
    }

    // Trigger events to update summary
    document.querySelector('#Title').dispatchEvent(new Event('input'));
    updateContentSummary();
}

function previewCustomReport() {
    // Implement preview functionality
    alert('Preview functionality would be implemented here');
}