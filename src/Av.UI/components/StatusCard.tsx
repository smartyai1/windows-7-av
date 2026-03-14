import { ReactNode } from "react";
import { Severity } from "../models";
import { SeverityBadge } from "./SeverityBadge";

interface StatusCardProps {
  title: string;
  value: string;
  severity: Severity;
  description?: string;
  action?: ReactNode;
}

export const StatusCard = ({ title, value, severity, description, action }: StatusCardProps) => (
  <article className="status-card" tabIndex={0} aria-label={`${title} status`}>
    <header>
      <h3>{title}</h3>
      <SeverityBadge severity={severity} />
    </header>
    <div className="value">{value}</div>
    {description ? <p>{description}</p> : null}
    {action ? <div className="status-card-action">{action}</div> : null}
  </article>
);
