window.deepr = {
    downloadFile: function (fileName, base64Content, contentType) {
        const bytes = Uint8Array.from(atob(base64Content), c => c.charCodeAt(0));
        const blob = new Blob([bytes], { type: contentType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};
