"use client";

import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { useDocuments } from "@/features/documents/hooks/useDocuments";
import { DocumentRow } from "@/features/documents/components/DocumentRow";
import { UploadModal } from "@/features/documents/components/UploadModal";

export function DocumentsPanel() {
  const { documents, isLoading, error, refetch, removeDocument } = useDocuments();
  const [isUploadOpen, setIsUploadOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const handleDelete = async (id: string, fileName: string) => {
    if (!window.confirm(`Delete "${fileName}"? This cannot be undone.`)) return;

    setDeleteError(null);
    try {
      await removeDocument(id);
    } catch {
      setDeleteError("Couldn't delete the document. Please try again.");
    }
  };

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center justify-between">
        <h2 className="text-xs font-medium tracking-wide text-zinc-500 uppercase dark:text-zinc-400">
          Documents
        </h2>
        <Button
          variant="secondary"
          className="px-2 py-1 text-xs"
          onClick={() => setIsUploadOpen(true)}
        >
          Upload
        </Button>
      </div>

      {error && <p className="mt-3 text-xs text-red-600 dark:text-red-400">{error}</p>}
      {deleteError && (
        <p className="mt-2 text-xs text-red-600 dark:text-red-400">{deleteError}</p>
      )}

      {isLoading ? (
        <p className="mt-3 text-sm text-zinc-400 dark:text-zinc-600">Loading…</p>
      ) : documents.length === 0 ? (
        <p className="mt-3 text-sm text-zinc-400 dark:text-zinc-600">
          No documents yet — upload one to get started.
        </p>
      ) : (
        <ul className="mt-3 space-y-1">
          {documents.map((document) => (
            <DocumentRow
              key={document.id}
              document={document}
              onDelete={() => handleDelete(document.id, document.fileName)}
            />
          ))}
        </ul>
      )}

      {isUploadOpen && (
        <UploadModal onClose={() => setIsUploadOpen(false)} onUploaded={refetch} />
      )}
    </div>
  );
}
