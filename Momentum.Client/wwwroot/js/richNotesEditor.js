// Rich Notes editor JS helpers — minimal interop for contenteditable editing.
// Toolbar buttons call preventDefault on mousedown so the editor never loses focus
// during formatting; this is wired in RichNotesEditor.razor via @onmousedown:preventDefault.

window.richNotesEditor = {

    // Initialize the editor element: set initial HTML content and attach paste handler.
    init: function (id, html) {
        const el = document.getElementById(id);
        if (!el) return;
        el.innerHTML = html || '';
        // Strip external formatting on paste — insert plain text only.
        el.addEventListener('paste', function (e) {
            e.preventDefault();
            const text = (e.clipboardData || window.clipboardData).getData('text/plain');
            document.execCommand('insertText', false, text);
        });
    },

    // Return the current innerHTML of the editor (what gets sent to the server).
    getContent: function (id) {
        const el = document.getElementById(id);
        return el ? el.innerHTML : '';
    },

    // Apply a formatting command to the current selection.
    format: function (command) {
        document.execCommand(command, false, null);
    },

    // Move keyboard focus into the editor.
    focus: function (id) {
        const el = document.getElementById(id);
        if (el) el.focus();
    }
};
