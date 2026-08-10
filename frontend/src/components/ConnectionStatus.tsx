"use client";

import { useEffect, useState } from "react";
import { checkHealth } from "@/lib/api";

type Status = "checking" | "connected" | "disconnected";

export function ConnectionStatus() {
  const [status, setStatus] = useState<Status>("checking");

  useEffect(() => {
    let cancelled = false;

    checkHealth().then((ok) => {
      if (!cancelled) setStatus(ok ? "connected" : "disconnected");
    });

    return () => {
      cancelled = true;
    };
  }, []);

  const label =
    status === "checking"
      ? "Checking…"
      : status === "connected"
        ? "Connected"
        : "Disconnected";

  const dotColor =
    status === "checking"
      ? "bg-zinc-400"
      : status === "connected"
        ? "bg-green-500"
        : "bg-red-500";

  return (
    <span className="flex items-center gap-1.5 text-xs text-zinc-500 dark:text-zinc-400">
      <span className={`h-2 w-2 rounded-full ${dotColor}`} aria-hidden />
      {label}
    </span>
  );
}
