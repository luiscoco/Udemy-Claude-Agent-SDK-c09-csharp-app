/* Local Prism integration: never evaluates source code or inserts unescaped source HTML. */
(() => {
  const Prism = globalThis.Prism;
  if (!Prism) return;
  for (const name of ["javascript", "typescript"]) {
    Prism.languages.insertBefore(name, "keyword", {
      "object-property": { pattern: /\b[A-Za-z_$][\w$]*(?=\s*:)/, alias: "property" },
      "member-property": { pattern: /(\.\s*)[A-Za-z_$][\w$]*(?![\w$]|\s*\()/, lookbehind: true, alias: "property" },
    });
  }
  // Compact C# grammar (the bundled Prism build has no csharp component).
  Prism.languages.csharp = Prism.languages.extend("clike", {
    string: [
      { pattern: /"""[\s\S]*?"""/, greedy: true },
      { pattern: /\$?@?"(?:""|\[\s\S]|[^\\"])*"/, greedy: true },
      { pattern: /'(?:\.|[^\'])'/, greedy: true },
    ],
    "class-name": [
      { pattern: /(\b(?:class|record|struct|interface|enum|new|is|as|catch)\s+)[A-Z]\w*/, lookbehind: true },
      /\b[A-Z]\w*(?=\s*(?:<[^>]*>)?\s+\w+\s*[=;,)(])/,
    ],
    keyword: /\b(?:abstract|as|async|await|base|bool|break|case|catch|class|const|continue|default|do|double|else|enum|false|finally|for|foreach|get|if|in|init|int|interface|internal|is|long|namespace|new|not|null|or|out|override|private|protected|public|readonly|record|required|return|sealed|set|static|string|struct|switch|this|throw|true|try|using|var|void|when|while|with)\b/,
    number: /\b0x[\da-f]+\b|(?:\b\d+(?:\.\d*)?|\B\.\d+)(?:e[+-]?\d+)?[dfmlu]*/i,
  });
  Prism.languages.insertBefore("csharp", "keyword", {
    attribute: { pattern: /^\s*\[[A-Z][^\]]*\]/m, alias: "property" },
  });
  Prism.languages.cs = Prism.languages.csharp;
  Prism.languages.dotenv = {
    comment: { pattern: /^\s*#.*$/m, greedy: true },
    property: { pattern: /(^|\n)\s*(?:export\s+)?[A-Z_][A-Z_0-9]*(?=\s*=)/, lookbehind: true },
    string: { pattern: /([=]\s*)[^\r\n]+/, lookbehind: true },
    operator: /=/,
  };
  const highlighted = new WeakMap();
  function language(code) {
    const declared = (code.className + " " + code.parentElement.className).match(/(?:language|lang)-([\w-]+)/);
    if (declared && Prism.languages[declared[1]]) return declared[1];
    const pre = code.parentElement;
    if (["evidence", "live-evidence"].includes(pre.id)) return "json";
    if (pre.id === "code") return "csharp";
    const source = code.textContent.trim();
    if (/^(?:\s*(?:#.*)?\n)*(?:[A-Z_][A-Z_0-9]*=)/.test(source)) return "dotenv";
    if (/^(?:dotnet\s|node\s|npm\s|npx\s|Copy-Item\s|notepad\s|cd\s|\$[\w]+\s*=)/.test(source)) return "powershell";
    if (/^[\[{]/.test(source)) {
      try { JSON.parse(source); return "json"; } catch { /* May be a JS fragment. */ }
    }
    const figure = pre.closest("figure");
    const label = (figure?.querySelector("figcaption")?.textContent || pre.previousElementSibling?.textContent || "");
    if (/\.(?:js|cjs|mjs)\b/.test(label)) return "javascript";
    if (/\.ts\b/.test(label)) return "typescript";
    return "csharp";
  }
  function refresh() {
    for (const pre of document.querySelectorAll("pre")) {
      let code = pre.querySelector(":scope > code");
      if (!code) {
        if (!pre.textContent.trim()) continue;
        code = document.createElement("code");
        code.textContent = pre.textContent;
        pre.replaceChildren(code);
      }
      const source = code.textContent;
      if (!source.trim()) continue;
      const lang = language(code);
      const fingerprint = lang + "\n" + source;
      if (highlighted.get(code) === fingerprint) continue;
      code.className = code.className.replace(/\b(?:language|lang)-[\w-]+\b/g, "").trim() + " language-" + lang;
      Prism.highlightElement(code);
      highlighted.set(code, fingerprint);
      code.dataset.syntaxHighlighted = "true";
    }
  }
  let scheduled = false;
  const observer = new MutationObserver((records) => {
    // Observe text inserted by the app, including source/evidence loaded after startup.
    if (!records.some(record => record.target.nodeType === 3 ? record.target.parentElement?.closest("pre") : record.target.closest?.("pre") || [...record.addedNodes].some(node => node.nodeType === 1 && (node.matches("pre") || node.querySelector("pre"))))) return;
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(() => { scheduled = false; refresh(); });
  });
  refresh();
  observer.observe(document.body, { childList: true, characterData: true, subtree: true });
})();
