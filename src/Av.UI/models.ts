export type Severity = "low" | "medium" | "high" | "critical";

export interface EngineEvent {
  id: string;
  type: "status" | "threat" | "scan" | "update";
  source: string;
  message: string;
  severity: Severity;
  timestamp: string;
  metadata?: Record<string, string | number | boolean>;
}

export interface ProtectionState {
  realtimeProtection: boolean;
  definitionsVersion: string;
  lastUpdateUtc: string;
  activeThreats: number;
  lastScanUtc?: string;
}

export interface ScanRequest {
  mode: "quick" | "full" | "custom";
  customPath?: string;
}

export interface ThreatRecord {
  id: string;
  name: string;
  filePath: string;
  actionTaken: "blocked" | "quarantined" | "removed";
  severity: Severity;
  detectedUtc: string;
}

export interface QuarantineItem {
  id: string;
  threatName: string;
  originalPath: string;
  quarantinedUtc: string;
}

export interface StartupItem {
  id: string;
  name: string;
  publisher: string;
  path: string;
  enabled: boolean;
  risk: Severity;
}

export interface Exclusion {
  id: string;
  type: "file" | "folder" | "process" | "extension";
  value: string;
}

export interface ScheduleSettings {
  frequency: "daily" | "weekly" | "monthly";
  time: string;
  quickScanDays: string[];
}

export interface UpdateCadence {
  channel: "stable" | "preview";
  checkIntervalMinutes: number;
  autoApply: boolean;
}
