import { ConnectionStatus } from "@/components/ui/ConnectionStatus";

export function Header() {
  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-zinc-200 px-4 dark:border-zinc-800">
      <div className="flex items-center gap-3">
        <h1 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">
          Chat With Your Documents
        </h1>
        <ConnectionStatus />
      </div>
      <button
        type="button"
        disabled
        className="cursor-not-allowed rounded-md border border-zinc-200 px-3 py-1.5 text-sm text-zinc-400 dark:border-zinc-800 dark:text-zinc-600"
      >
        New chat
      </button>
    </header>
  );
}
