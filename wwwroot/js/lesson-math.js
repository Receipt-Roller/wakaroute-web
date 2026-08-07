(() => {
    const mathContent = document.querySelector("[data-math-content]");

    if (!mathContent || typeof window.renderMathInElement !== "function") {
        return;
    }

    window.renderMathInElement(mathContent, {
        delimiters: [
            { left: "\\[", right: "\\]", display: true },
            { left: "\\(", right: "\\)", display: false },
            { left: "$$", right: "$$", display: true },
            { left: "$", right: "$", display: false }
        ],
        ignoredTags: ["pre", "code", "script", "style", "textarea"],
        throwOnError: false
    });
})();
