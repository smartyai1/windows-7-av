import { useEffect, useState } from "react";
import { QuarantineItem } from "../models";
import { ipcClient } from "../services/ipcClient";

export const QuarantinePage = () => {
  const [items, setItems] = useState<QuarantineItem[]>([]);

  const refresh = () => ipcClient.getQuarantine().then(setItems).catch(console.error);

  useEffect(() => {
    refresh();
  }, []);

  return (
    <section aria-labelledby="quarantine-title">
      <h2 id="quarantine-title">Quarantine manager</h2>
      <ul>
        {items.map((item) => (
          <li key={item.id} className="list-item-card">
            <div>
              <strong>{item.threatName}</strong>
              <p>{item.originalPath}</p>
            </div>
            <div className="action-row">
              <button onClick={async () => { await ipcClient.restoreQuarantineItem(item.id); refresh(); }}>
                Restore
              </button>
              <button onClick={async () => { await ipcClient.deleteQuarantineItem(item.id); refresh(); }}>
                Delete
              </button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
};
