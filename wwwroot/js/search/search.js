// Focus the search box when the page loads
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.querySelector('input[name="query"]');
    if (searchInput) {
        searchInput.focus();
        // Place cursor at the end
        const val = searchInput.value;
        searchInput.value = '';
        searchInput.value = val;
    }

    // Highlight search terms
    highlightSearchTerms();
});

function highlightSearchTerms() {
    const query = '@Model.Query';
    if (!query) return;

    const resultsText = document.querySelectorAll('.list-group-item-action p, .list-group-item-action h6');

    resultsText.forEach(element => {
        const originalText = element.innerHTML;
        if (originalText.includes('<')) return; // Skip if already has HTML

        const regex = new RegExp('(' + escapeRegExp(query) + ')', 'gi');
        element.innerHTML = originalText.replace(regex, '<span class="search-highlight">$1</span>');
    });
}

function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}