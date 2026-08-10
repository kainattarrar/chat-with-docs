import type { Document } from "@/lib/types";

// Every backend call goes through this module — no other file should
// construct a request URL directly.
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status?: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  return fetch(`${API_BASE_URL}${path}`, init);
}

export async function checkHealth(): Promise<boolean> {
  try {
    const response = await apiFetch("/health");
    return response.ok;
  } catch {
    return false;
  }
}

export async function listDocuments(): Promise<Document[]> {
  const response = await apiFetch("/api/documents");
  if (!response.ok) {
    throw new ApiError("Couldn't load documents.", response.status);
  }
  return response.json();
}

export async function deleteDocument(id: string): Promise<void> {
  const response = await apiFetch(`/api/documents/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new ApiError("Couldn't delete the document.", response.status);
  }
}

// Uses XMLHttpRequest instead of fetch because fetch has no upload-progress
// event — onProgress needs real byte-level progress for the upload modal.
export function uploadDocument(
  file: File,
  onProgress?: (percent: number) => void,
): Promise<{ id: string; status: string }> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("POST", `${API_BASE_URL}/api/documents`);

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable && onProgress) {
        onProgress(Math.round((event.loaded / event.total) * 100));
      }
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve(JSON.parse(xhr.responseText));
        return;
      }

      let message = "Upload failed. Please try again.";
      try {
        const body = JSON.parse(xhr.responseText);
        if (body?.error) message = body.error;
      } catch {
        // Response wasn't JSON — fall back to the generic message.
      }
      reject(new ApiError(message, xhr.status));
    };

    xhr.onerror = () => reject(new ApiError("Upload failed. Please check your connection."));

    const formData = new FormData();
    formData.append("file", file);
    xhr.send(formData);
  });
}
