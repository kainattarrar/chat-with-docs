"use client";

import { useCallback, useRef, useState } from "react";
import type { DragEvent } from "react";
import { ApiError, uploadDocument } from "@/lib/api";

interface UploadModalProps {
  onClose: () => void;
  onUploaded: () => void;
}

export function UploadModal({ onClose, onUploaded }: UploadModalProps) {
  const [isDragging, setIsDragging] = useState(false);
  const [progress, setProgress] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const isUploading = progress !== null;

  const handleFile = useCallback(
    async (file: File) => {
      setError(null);
      setProgress(0);
      try {
        await uploadDocument(file, setProgress);
        onUploaded();
        onClose();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Upload failed. Please try again.");
        setProgress(null);
      }
    },
    [onUploaded, onClose],
  );

  const onDrop = useCallback(
    (event: DragEvent<HTMLDivElement>) => {
      event.preventDefault();
      setIsDragging(false);
      if (isUploading) return;
      const file = event.dataTransfer.files?.[0];
      if (file) handleFile(file);
    },
    [handleFile, isUploading],
  );

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-label="Upload document"
    >
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl dark:bg-zinc-900">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">
            Upload document
          </h2>
          <button
            type="button"
            onClick={onClose}
            disabled={isUploading}
            className="text-zinc-400 hover:text-zinc-600 disabled:opacity-40 dark:hover:text-zinc-200"
            aria-label="Close"
          >
            ✕
          </button>
        </div>

        <div
          onDragOver={(event) => {
            event.preventDefault();
            if (!isUploading) setIsDragging(true);
          }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={onDrop}
          onClick={() => !isUploading && fileInputRef.current?.click()}
          className={`flex flex-col items-center justify-center rounded-md border-2 border-dashed p-8 text-center transition-colors ${
            isUploading ? "cursor-not-allowed opacity-60" : "cursor-pointer"
          } ${
            isDragging
              ? "border-zinc-400 bg-zinc-50 dark:border-zinc-500 dark:bg-zinc-800"
              : "border-zinc-200 dark:border-zinc-700"
          }`}
        >
          <p className="text-sm text-zinc-600 dark:text-zinc-400">
            Drag and drop a PDF here, or click to browse
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept="application/pdf"
            className="hidden"
            onChange={(event) => {
              const file = event.target.files?.[0];
              event.target.value = "";
              if (file) handleFile(file);
            }}
          />
        </div>

        {isUploading && (
          <div className="mt-4">
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-800">
              <div
                className="h-full rounded-full bg-zinc-900 transition-all dark:bg-zinc-100"
                style={{ width: `${progress}%` }}
              />
            </div>
            <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Uploading… {progress}%
            </p>
          </div>
        )}

        {error && (
          <p className="mt-4 text-sm text-red-600 dark:text-red-400" role="alert">
            {error}
          </p>
        )}
      </div>
    </div>
  );
}
