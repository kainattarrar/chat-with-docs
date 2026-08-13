import { StatusBadge } from "@/components/ui/StatusBadge";
import { TrashIcon } from "@/components/ui/TrashIcon";
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
        <TrashIcon className="h-4 w-4" />
      </button>
    </li>
  );
}
