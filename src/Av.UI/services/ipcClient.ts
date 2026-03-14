import {
  EngineEvent,
  Exclusion,
  ProtectionState,
  QuarantineItem,
  ScanRequest,
  ScheduleSettings,
  StartupItem,
  ThreatRecord,
  UpdateCadence,
} from "../models";

/**
 * IPC abstraction for connecting to Av.Service over loopback REST.
 * Service can be hosted by a named-pipe bridge or gRPC-gateway.
 */
export class IpcClient {
  constructor(private readonly baseUrl = "http://127.0.0.1:5392/api") {}

  async getProtectionState(): Promise<ProtectionState> {
    return this.get<ProtectionState>("/status");
  }

  async startScan(request: ScanRequest): Promise<void> {
    await this.post("/scans/start", request);
  }

  async getThreatHistory(): Promise<ThreatRecord[]> {
    return this.get<ThreatRecord[]>("/threats/history");
  }

  async getQuarantine(): Promise<QuarantineItem[]> {
    return this.get<QuarantineItem[]>("/quarantine");
  }

  async restoreQuarantineItem(id: string): Promise<void> {
    await this.post(`/quarantine/${encodeURIComponent(id)}/restore`);
  }

  async deleteQuarantineItem(id: string): Promise<void> {
    await this.post(`/quarantine/${encodeURIComponent(id)}/delete`);
  }

  async getStartupItems(): Promise<StartupItem[]> {
    return this.get<StartupItem[]>("/startup-items");
  }

  async toggleStartupItem(id: string, enabled: boolean): Promise<void> {
    await this.post(`/startup-items/${encodeURIComponent(id)}/toggle`, { enabled });
  }

  async getExclusions(): Promise<Exclusion[]> {
    return this.get<Exclusion[]>("/settings/exclusions");
  }

  async saveExclusions(exclusions: Exclusion[]): Promise<void> {
    await this.post("/settings/exclusions", exclusions);
  }

  async getSchedule(): Promise<ScheduleSettings> {
    return this.get<ScheduleSettings>("/settings/schedule");
  }

  async saveSchedule(schedule: ScheduleSettings): Promise<void> {
    await this.post("/settings/schedule", schedule);
  }

  async getUpdateCadence(): Promise<UpdateCadence> {
    return this.get<UpdateCadence>("/settings/update-cadence");
  }

  async saveUpdateCadence(cadence: UpdateCadence): Promise<void> {
    await this.post("/settings/update-cadence", cadence);
  }

  subscribeToEvents(onEvent: (event: EngineEvent) => void): () => void {
    const stream = new EventSource(`${this.baseUrl}/events/stream`);
    stream.onmessage = (message) => {
      onEvent(JSON.parse(message.data) as EngineEvent);
    };

    stream.onerror = () => {
      // Let browser reconnect automatically.
    };

    return () => stream.close();
  }

  private async get<T>(path: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`);
    if (!response.ok) {
      throw new Error(`GET ${path} failed (${response.status})`);
    }

    return (await response.json()) as T;
  }

  private async post(path: string, payload?: unknown): Promise<void> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: payload === undefined ? undefined : JSON.stringify(payload),
    });

    if (!response.ok) {
      throw new Error(`POST ${path} failed (${response.status})`);
    }
  }
}

export const ipcClient = new IpcClient();
