import type {
  ChatConversationEvent,
  ChatDoneEvent,
  ChatErrorEvent,
  ChatSourcesEvent,
  ChatStreamEvent,
  ChatTokenEvent,
  ConversationDetail,
  ConversationSummary,
  Document,
} from "@/lib/types";

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

export async function listConversations(): Promise<ConversationSummary[]> {
  const response = await apiFetch("/api/conversations");
  if (!response.ok) {
    throw new ApiError("Couldn't load conversations.", response.status);
  }
  return response.json();
}

export async function getConversation(id: string): Promise<ConversationDetail> {
  const response = await apiFetch(`/api/conversations/${id}`);
  if (!response.ok) {
    throw new ApiError("Couldn't load the conversation.", response.status);
  }
  return response.json();
}

export async function deleteConversation(id: string): Promise<void> {
  const response = await apiFetch(`/api/conversations/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new ApiError("Couldn't delete the conversation.", response.status);
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

// EventSource can't be used here — it's GET-only, and this endpoint takes a
// JSON POST body. So this reads the raw stream via fetch + ReadableStream and
// parses SSE by hand. Network reads don't align to event boundaries, so text
// is buffered and only complete events (delimited by a blank line) are parsed.
export async function* streamChat(
  question: string,
  conversationId: string | undefined,
  signal?: AbortSignal,
): AsyncGenerator<ChatStreamEvent> {
  const response = await apiFetch("/api/chat", {
    method: "POST",
    // JSON.stringify drops keys with an undefined value, so conversationId is
    // naturally omitted for a fresh chat rather than sent as null/"undefined".
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ question, conversationId }),
    signal,
  });

  if (!response.ok || !response.body) {
    throw new ApiError("Couldn't reach the chat service.", response.status);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });

    let separatorIndex: number;
    while ((separatorIndex = buffer.indexOf("\n\n")) !== -1) {
      const rawEvent = buffer.slice(0, separatorIndex);
      buffer = buffer.slice(separatorIndex + 2);

      const parsed = parseSseEvent(rawEvent);
      if (parsed) yield parsed;
    }
  }
}

function parseSseEvent(raw: string): ChatStreamEvent | null {
  let eventName: string | null = null;
  const dataLines: string[] = [];

  for (const line of raw.split("\n")) {
    if (line.startsWith("event:")) {
      eventName = line.slice("event:".length).trim();
    } else if (line.startsWith("data:")) {
      dataLines.push(line.slice("data:".length).trim());
    }
  }

  if (!eventName) return null;

  const dataText = dataLines.join("\n");
  let data: unknown;
  try {
    data = dataText ? JSON.parse(dataText) : {};
  } catch {
    return null;
  }

  switch (eventName) {
    case "conversation":
      return { event: "conversation", data: data as ChatConversationEvent };
    case "sources":
      return { event: "sources", data: data as ChatSourcesEvent };
    case "token":
      return { event: "token", data: data as ChatTokenEvent };
    case "done":
      return { event: "done", data: data as ChatDoneEvent };
    case "error":
      return { event: "error", data: data as ChatErrorEvent };
    default:
      return null;
  }
}
