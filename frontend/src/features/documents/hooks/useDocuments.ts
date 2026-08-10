"use client";

import { useCallback, useEffect, useState } from "react";
import { deleteDocument, listDocuments } from "@/lib/api";
import type { Document } from "@/lib/types";

const POLL_INTERVAL_MS = 2500;

export function useDocuments() {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refetch = useCallback(async () => {
    try {
      const docs = await listDocuments();
      setDocuments(docs);
      setError(null);
    } catch {
      setError("Couldn't load documents.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const docs = await listDocuments();
        if (cancelled) return;
        setDocuments(docs);
        setError(null);
      } catch {
        if (!cancelled) setError("Couldn't load documents.");
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const hasProcessing = documents.some((doc) => doc.status === "Processing");

  // Only polls while something is actually in flight, and the interval is
  // torn down on unmount or once nothing is Processing anymore.
  useEffect(() => {
    if (!hasProcessing) return;

    const interval = setInterval(refetch, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [hasProcessing, refetch]);

  const removeDocument = useCallback(async (id: string) => {
    await deleteDocument(id);
    setDocuments((prev) => prev.filter((doc) => doc.id !== id));
  }, []);

  return { documents, isLoading, error, refetch, removeDocument };
}
