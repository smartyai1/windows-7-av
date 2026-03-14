import { FormEvent, useState } from "react";
import { ipcClient } from "../services/ipcClient";

export const ScansPage = () => {
  const [customPath, setCustomPath] = useState("C:\\");
  const [busy, setBusy] = useState(false);

  const runScan = async (mode: "quick" | "full" | "custom", event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    try {
      await ipcClient.startScan({ mode, customPath: mode === "custom" ? customPath : undefined });
    } finally {
      setBusy(false);
    }
  };

  return (
    <section aria-labelledby="scan-title">
      <h2 id="scan-title">Scan controls</h2>
      <div className="scan-controls">
        <form onSubmit={(e) => runScan("quick", e)}>
          <h3>Quick Scan</h3>
          <button disabled={busy} aria-label="Run quick scan">Run quick scan</button>
        </form>
        <form onSubmit={(e) => runScan("full", e)}>
          <h3>Full Scan</h3>
          <button disabled={busy} aria-label="Run full scan">Run full scan</button>
        </form>
        <form onSubmit={(e) => runScan("custom", e)}>
          <h3>Custom Scan</h3>
          <label htmlFor="customPath">Folder or file path</label>
          <input
            id="customPath"
            value={customPath}
            onChange={(e) => setCustomPath(e.target.value)}
            aria-describedby="customPathHelp"
          />
          <p id="customPathHelp">Enter a full Windows path for targeted scanning.</p>
          <button disabled={busy} aria-label="Run custom scan">Run custom scan</button>
        </form>
      </div>
    </section>
  );
};
