import { StatusBadge } from "@/components/ui/StatusBadge";
import type { Document } from "@/lib/types";

interface DocumentRowProps {
  document: Document;
  onDelete: () => void;
}

export function DocumentRow({ document, onDelete }: DocumentRowProps) {
  return (
    <li className="flex items-center justify-between gap-2 rounded-md px-2 py-1.5 hover:bg-zinc-50 dark:hover:bg-zinc-800/50">
      <div className="min-w-0 flex-1">
        <p
          className="truncate text-sm text-zinc-700 dark:text-zinc-300"
          title={document.fileName}
        >
          {document.fileName}
        </p>
        <div className="mt-1">
          <StatusBadge status={document.status} />
        </div>
      </div>
      <button
        type="button"
        onClick={onDelete}
        aria-label={`Delete ${document.fileName}`}
        className="shrink-0 text-zinc-400 hover:text-red-600 dark:hover:text-red-400"
      >
        <svg
          className="h-4 w-4"
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden
        >
          <path
            fillRule="evenodd"
            d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z"
            clipRule="evenodd"
          />
        </svg>
      </button>
    </li>
  );
}
