import { Severity } from "../models";

interface SeverityBadgeProps {
  severity: Severity;
}

export const SeverityBadge = ({ severity }: SeverityBadgeProps) => (
  <span className={`severity-badge ${severity}`} role="status" aria-label={`Severity ${severity}`}>
    {severity.toUpperCase()}
  </span>
);
