"use client";

import { useState } from "react";

// Minimal shape shared by both the live-streaming ChatSource (has
// documentId) and the persisted ConversationMessageSource (doesn't) — lets
// this component render sources from either path without casting.
interface SourcesListProps {
  sources: { fileName: string; chunkIndex: number; snippet: string }[];
}

export function SourcesList({ sources }: SourcesListProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="mt-2 border-t border-zinc-200 pt-2 dark:border-zinc-700">
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className="text-xs font-medium text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-zinc-200"
      >
        {isOpen ? "▾" : "▸"} Sources ({sources.length})
      </button>
      {isOpen && (
        <ul className="mt-2 space-y-2">
          {sources.map((source, index) => (
            <li
              key={`${source.fileName}-${source.chunkIndex}-${index}`}
              className="rounded-md bg-zinc-50 p-2 text-xs text-zinc-600 dark:bg-zinc-900 dark:text-zinc-400"
            >
              <p className="font-medium text-zinc-700 dark:text-zinc-300">
                {index + 1}. {source.fileName}
              </p>
              <p className="mt-1 line-clamp-3">{source.snippet}</p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
