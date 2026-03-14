import { useEffect, useMemo, useState } from "react";
import { EngineEvent, ProtectionState } from "../models";
import { ipcClient } from "../services/ipcClient";

export const useEngineEvents = () => {
  const [events, setEvents] = useState<EngineEvent[]>([]);
  const [state, setState] = useState<ProtectionState | null>(null);

  useEffect(() => {
    ipcClient.getProtectionState().then(setState).catch(console.error);

    const unsubscribe = ipcClient.subscribeToEvents((event) => {
      setEvents((current) => [event, ...current].slice(0, 100));

      if (event.type === "status") {
        ipcClient.getProtectionState().then(setState).catch(console.error);
      }
    });

    return unsubscribe;
  }, []);

  const highSeverityCount = useMemo(
    () => events.filter((x) => x.severity === "high" || x.severity === "critical").length,
    [events],
  );

  return {
    events,
    state,
    highSeverityCount,
  };
};
