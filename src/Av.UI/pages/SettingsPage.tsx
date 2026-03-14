import { FormEvent, useEffect, useState } from "react";
import { Exclusion, ScheduleSettings, UpdateCadence } from "../models";
import { ipcClient } from "../services/ipcClient";

const defaultSchedule: ScheduleSettings = { frequency: "weekly", time: "02:00", quickScanDays: ["Mon"] };
const defaultCadence: UpdateCadence = { channel: "stable", checkIntervalMinutes: 120, autoApply: true };

export const SettingsPage = () => {
  const [exclusions, setExclusions] = useState<Exclusion[]>([]);
  const [schedule, setSchedule] = useState<ScheduleSettings>(defaultSchedule);
  const [cadence, setCadence] = useState<UpdateCadence>(defaultCadence);
  const [newExclusion, setNewExclusion] = useState("C:\\Temp");

  useEffect(() => {
    ipcClient.getExclusions().then(setExclusions).catch(console.error);
    ipcClient.getSchedule().then(setSchedule).catch(console.error);
    ipcClient.getUpdateCadence().then(setCadence).catch(console.error);
  }, []);

  const saveAll = async (event: FormEvent) => {
    event.preventDefault();
    await Promise.all([
      ipcClient.saveExclusions(exclusions),
      ipcClient.saveSchedule(schedule),
      ipcClient.saveUpdateCadence(cadence),
    ]);
  };

  return (
    <section aria-labelledby="settings-title">
      <h2 id="settings-title">Settings</h2>
      <form onSubmit={saveAll}>
        <fieldset>
          <legend>Exclusions</legend>
          <ul>
            {exclusions.map((exclusion) => (
              <li key={exclusion.id}>
                {exclusion.type}: {exclusion.value}
                <button
                  type="button"
                  onClick={() => setExclusions((current) => current.filter((x) => x.id !== exclusion.id))}
                  aria-label={`Remove exclusion ${exclusion.value}`}
                >
                  Remove
                </button>
              </li>
            ))}
          </ul>
          <label htmlFor="newExclusion">New exclusion path</label>
          <input id="newExclusion" value={newExclusion} onChange={(e) => setNewExclusion(e.target.value)} />
          <button
            type="button"
            onClick={() =>
              setExclusions((current) => [
                ...current,
                { id: crypto.randomUUID(), type: "folder", value: newExclusion },
              ])
            }
          >
            Add exclusion
          </button>
        </fieldset>

        <fieldset>
          <legend>Scan schedule</legend>
          <label>
            Frequency
            <select
              value={schedule.frequency}
              onChange={(e) => setSchedule((s) => ({ ...s, frequency: e.target.value as ScheduleSettings["frequency"] }))}
            >
              <option value="daily">Daily</option>
              <option value="weekly">Weekly</option>
              <option value="monthly">Monthly</option>
            </select>
          </label>
          <label>
            Time
            <input
              type="time"
              value={schedule.time}
              onChange={(e) => setSchedule((s) => ({ ...s, time: e.target.value }))}
            />
          </label>
        </fieldset>

        <fieldset>
          <legend>Update cadence</legend>
          <label>
            Channel
            <select
              value={cadence.channel}
              onChange={(e) => setCadence((c) => ({ ...c, channel: e.target.value as UpdateCadence["channel"] }))}
            >
              <option value="stable">Stable</option>
              <option value="preview">Preview</option>
            </select>
          </label>
          <label>
            Check interval (minutes)
            <input
              type="number"
              min={15}
              value={cadence.checkIntervalMinutes}
              onChange={(e) => setCadence((c) => ({ ...c, checkIntervalMinutes: Number(e.target.value) }))}
            />
          </label>
          <label>
            <input
              type="checkbox"
              checked={cadence.autoApply}
              onChange={(e) => setCadence((c) => ({ ...c, autoApply: e.target.checked }))}
            />
            Automatically apply updates
          </label>
        </fieldset>

        <button type="submit">Save settings</button>
      </form>
    </section>
  );
};
