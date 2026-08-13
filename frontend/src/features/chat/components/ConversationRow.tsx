import { TrashIcon } from "@/components/ui/TrashIcon";
import type { ConversationSummary } from "@/lib/types";

interface ConversationRowProps {
  conversation: ConversationSummary;
  isActive: boolean;
  disabled: boolean;
  onSelect: () => void;
  onDelete: () => void;
}

export function ConversationRow({ conversation, isActive, disabled, onSelect, onDelete }: ConversationRowProps) {
  const label = conversation.title?.trim() || "New chat";

  return (
    <li
      className={`group flex items-center justify-between gap-2 rounded-md px-2 py-1.5 ${
        isActive ? "bg-zinc-100 dark:bg-zinc-800" : "hover:bg-zinc-50 dark:hover:bg-zinc-800/50"
      }`}
    >
      <button
        type="button"
        onClick={onSelect}
        disabled={disabled}
        title={label}
        className="min-w-0 flex-1 truncate text-left text-sm text-zinc-700 disabled:cursor-not-allowed disabled:opacity-50 dark:text-zinc-300"
      >
        {label}
      </button>
      <button
        type="button"
        onClick={onDelete}
        disabled={disabled}
        aria-label={`Delete ${label}`}
        className="shrink-0 text-zinc-400 opacity-0 group-hover:opacity-100 hover:text-red-600 disabled:cursor-not-allowed disabled:opacity-50 dark:hover:text-red-400"
      >
        <TrashIcon className="h-4 w-4" />
      </button>
    </li>
  );
}
