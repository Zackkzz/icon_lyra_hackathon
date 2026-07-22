// Date helpers shared across screens. All "day" math is done on local dates.

export function toISODate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function getMonday(from = new Date()): Date {
  const d = new Date(from);
  const day = d.getDay(); // 0 = Sun
  const diff = (day === 0 ? -6 : 1) - day;
  d.setDate(d.getDate() + diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

export function addDays(d: Date, n: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + n);
  return r;
}

export function weekDates(monday: Date): Date[] {
  return Array.from({ length: 7 }, (_, i) => addDays(monday, i));
}

// Add/subtract days on a date-only ISO string without timezone drift.
export function addDaysIso(iso: string, n: number): string {
  const [y, m, d] = iso.slice(0, 10).split("-").map(Number);
  const dt = new Date(y, (m || 1) - 1, d || 1);
  dt.setDate(dt.getDate() + n);
  return toISODate(dt);
}

// Strip any time component (handles both "2026-07-29" and "2026-07-29T00:00:00Z").
export function toDateOnly(iso: string): string {
  return iso.slice(0, 10);
}

// Whole days until a best-before date (negative = expired). Parses the date
// portion into local parts to avoid timezone off-by-one shifts.
export function daysUntil(iso: string): number {
  const [y, m, d] = iso.slice(0, 10).split("-").map(Number);
  const target = new Date(y, (m || 1) - 1, d || 1);
  target.setHours(0, 0, 0, 0);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return Math.round((target.getTime() - today.getTime()) / 86_400_000);
}

// Human-friendly best-before, e.g. "Wed 29 Jul".
export function formatFriendly(iso: string): string {
  const [y, m, d] = iso.slice(0, 10).split("-").map(Number);
  if (!y || !m || !d) return iso;
  return new Date(y, m - 1, d).toLocaleDateString(undefined, {
    weekday: "short",
    day: "numeric",
    month: "short",
  });
}

export function shortDay(d: Date): string {
  return d.toLocaleDateString(undefined, { weekday: "short" });
}

export function dayNum(d: Date): string {
  return String(d.getDate());
}
