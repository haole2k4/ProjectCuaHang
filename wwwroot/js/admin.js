// Admin Panel JavaScript Functions

// Download file function for exports
window.downloadFile = function(filename, base64Content) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = "data:text/csv;base64," + base64Content;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Confirm dialog wrapper
window.confirmDialog = function(message) {
    return confirm(message);
};

// Alert wrapper
window.alertDialog = function(message) {
    alert(message);
};

// Print function
window.printPage = function() {
    window.print();
};

// Copy to clipboard
window.copyToClipboard = function(text) {
    navigator.clipboard.writeText(text).then(function() {
        console.log('Copied to clipboard');
    }, function(err) {
        console.error('Could not copy text: ', err);
    });
};

// Auto-hide success/error messages after delay
window.autoHideMessage = function(elementId, delay = 3000) {
    setTimeout(function() {
        const element = document.getElementById(elementId);
        if (element) {
            element.style.display = 'none';
        }
    }, delay);
};
