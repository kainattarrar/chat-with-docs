import { Spinner } from "@/components/ui/Spinner";
import type { DocumentStatus } from "@/lib/types";

const STYLES: Record<DocumentStatus, string> = {
  Processing: "bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-400",
  Ready: "bg-green-50 text-green-700 dark:bg-green-950 dark:text-green-400",
  Failed: "bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-400",
};

export function StatusBadge({ status }: { status: DocumentStatus }) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${STYLES[status]}`}
    >
      {status === "Processing" && <Spinner className="h-2.5 w-2.5" />}
      {status}
    </span>
  );
}
