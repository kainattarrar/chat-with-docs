import { DocumentsPanel } from "@/features/documents/components/DocumentsPanel";

export function Sidebar() {
  return (
    <aside className="w-64 shrink-0 border-r border-zinc-200 p-4 dark:border-zinc-800">
      <DocumentsPanel />
    </aside>
  );
}
