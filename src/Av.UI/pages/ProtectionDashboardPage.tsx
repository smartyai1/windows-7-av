import { EngineEvent } from "../models";
import { StatusCard } from "../components/StatusCard";

interface ProtectionDashboardPageProps {
  activeThreats: number;
  realtimeProtection: boolean;
  definitionsVersion: string;
  highSeverityCount: number;
  latestEvents: EngineEvent[];
}

export const ProtectionDashboardPage = ({
  activeThreats,
  realtimeProtection,
  definitionsVersion,
  highSeverityCount,
  latestEvents,
}: ProtectionDashboardPageProps) => (
  <section aria-labelledby="protection-title">
    <h2 id="protection-title">Protection status</h2>
    <div className="status-card-grid" role="list" aria-label="Real-time protection status cards">
      <StatusCard
        title="Real-time Protection"
        value={realtimeProtection ? "Enabled" : "Disabled"}
        severity={realtimeProtection ? "low" : "high"}
      />
      <StatusCard
        title="Active threats"
        value={`${activeThreats}`}
        severity={activeThreats > 0 ? "critical" : "low"}
      />
      <StatusCard
        title="High severity alerts"
        value={`${highSeverityCount}`}
        severity={highSeverityCount > 0 ? "high" : "low"}
      />
      <StatusCard title="Definitions" value={definitionsVersion} severity="medium" />
    </div>

    <h3>Latest engine events</h3>
    <ul className="event-list" aria-live="polite">
      {latestEvents.map((event) => (
        <li key={event.id}>
          <span>{new Date(event.timestamp).toLocaleTimeString()}</span>
          <span>{event.message}</span>
          <span className={`inline-severity ${event.severity}`}>{event.severity}</span>
        </li>
      ))}
    </ul>
  </section>
);
