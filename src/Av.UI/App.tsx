import { useMemo, useState } from "react";
import { useEngineEvents } from "./hooks/useEngineEvents";
import { ProtectionDashboardPage } from "./pages/ProtectionDashboardPage";
import { QuarantinePage } from "./pages/QuarantinePage";
import { ScansPage } from "./pages/ScansPage";
import { SettingsPage } from "./pages/SettingsPage";
import { StartupItemsPage } from "./pages/StartupItemsPage";
import { ThreatHistoryPage } from "./pages/ThreatHistoryPage";
import "./styles/app.css";

type Route = "dashboard" | "scans" | "history" | "quarantine" | "startup" | "settings";

const navItems: Array<{ key: Route; label: string }> = [
  { key: "dashboard", label: "Protection" },
  { key: "scans", label: "Scans" },
  { key: "history", label: "Threat history" },
  { key: "quarantine", label: "Quarantine" },
  { key: "startup", label: "Startup items" },
  { key: "settings", label: "Settings" },
];

export const App = () => {
  const [route, setRoute] = useState<Route>("dashboard");
  const { events, state, highSeverityCount } = useEngineEvents();

  const page = useMemo(() => {
    switch (route) {
      case "dashboard":
        return (
          <ProtectionDashboardPage
            activeThreats={state?.activeThreats ?? 0}
            realtimeProtection={state?.realtimeProtection ?? true}
            definitionsVersion={state?.definitionsVersion ?? "Unknown"}
            highSeverityCount={highSeverityCount}
            latestEvents={events.slice(0, 8)}
          />
        );
      case "scans":
        return <ScansPage />;
      case "history":
        return <ThreatHistoryPage />;
      case "quarantine":
        return <QuarantinePage />;
      case "startup":
        return <StartupItemsPage />;
      case "settings":
        return <SettingsPage />;
      default:
        return null;
    }
  }, [events, highSeverityCount, route, state]);

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">Skip to main content</a>
      <header>
        <h1>Windows 7 AV Console</h1>
      </header>
      <div className="layout">
        <nav aria-label="Primary navigation">
          <ul>
            {navItems.map((item) => (
              <li key={item.key}>
                <button
                  className={route === item.key ? "active" : ""}
                  onClick={() => setRoute(item.key)}
                  aria-current={route === item.key ? "page" : undefined}
                >
                  {item.label}
                </button>
              </li>
            ))}
          </ul>
        </nav>
        <main id="main-content" tabIndex={-1}>{page}</main>
      </div>
    </div>
  );
};
