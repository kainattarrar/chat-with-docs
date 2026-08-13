import { ConversationsPanel } from "@/features/chat/components/ConversationsPanel";
import { DocumentsPanel } from "@/features/documents/components/DocumentsPanel";

export function Sidebar() {
  return (
    <aside className="flex w-64 shrink-0 flex-col divide-y divide-zinc-200 overflow-hidden border-r border-zinc-200 dark:divide-zinc-800 dark:border-zinc-800">
      <div className="flex-1 overflow-y-auto p-4">
        <ConversationsPanel />
      </div>
      <div className="flex-1 overflow-y-auto p-4">
        <DocumentsPanel />
      </div>
    </aside>
  );
}
