import { useEffect, useState } from "react";
import { ThreatRecord } from "../models";
import { ipcClient } from "../services/ipcClient";
import { SeverityBadge } from "../components/SeverityBadge";

export const ThreatHistoryPage = () => {
  const [threats, setThreats] = useState<ThreatRecord[]>([]);

  useEffect(() => {
    ipcClient.getThreatHistory().then(setThreats).catch(console.error);
  }, []);

  return (
    <section aria-labelledby="threat-history-title">
      <h2 id="threat-history-title">Threat history</h2>
      <table>
        <caption className="sr-only">Recently detected threats and actions taken</caption>
        <thead>
          <tr>
            <th scope="col">Threat</th>
            <th scope="col">Path</th>
            <th scope="col">Action</th>
            <th scope="col">Severity</th>
            <th scope="col">Detected</th>
          </tr>
        </thead>
        <tbody>
          {threats.map((threat) => (
            <tr key={threat.id}>
              <td>{threat.name}</td>
              <td>{threat.filePath}</td>
              <td>{threat.actionTaken}</td>
              <td><SeverityBadge severity={threat.severity} /></td>
              <td>{new Date(threat.detectedUtc).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
};
