import { useEffect, useState } from "react";
import { StartupItem } from "../models";
import { SeverityBadge } from "../components/SeverityBadge";
import { ipcClient } from "../services/ipcClient";

export const StartupItemsPage = () => {
  const [items, setItems] = useState<StartupItem[]>([]);

  const refresh = () => ipcClient.getStartupItems().then(setItems).catch(console.error);

  useEffect(() => {
    refresh();
  }, []);

  return (
    <section aria-labelledby="startup-title">
      <h2 id="startup-title">Startup items</h2>
      <ul>
        {items.map((item) => (
          <li key={item.id} className="list-item-card">
            <div>
              <strong>{item.name}</strong>
              <p>{item.publisher}</p>
              <code>{item.path}</code>
            </div>
            <div className="action-row">
              <SeverityBadge severity={item.risk} />
              <label>
                <input
                  type="checkbox"
                  checked={item.enabled}
                  onChange={async (e) => {
                    await ipcClient.toggleStartupItem(item.id, e.target.checked);
                    refresh();
                  }}
                />
                Enabled
              </label>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
};
