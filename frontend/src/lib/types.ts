// Mirrors backend DTOs. Document/document-list endpoints don't exist yet
// (Phase 5); these types are defined now so later phases have one shared
// source of truth instead of each screen inventing its own shape.

export type DocumentStatus = "Processing" | "Ready" | "Failed";

export interface Document {
  id: string;
  fileName: string;
  status: DocumentStatus;
  createdAt: string;
  updatedAt: string;
}

export interface ChatSource {
  documentId: string;
  fileName: string;
  chunkIndex: number;
  snippet: string;
}

export interface ChatTokenEvent {
  text: string;
}

export type ChatSourcesEvent = ChatSource[];

export interface ChatErrorEvent {
  message: string;
}

// Discriminated union over the backend's SSE contract (POST /api/chat):
// sources -> token* -> done, or an error event in place of/interrupting that.
export type ChatStreamEvent =
  | { event: "sources"; data: ChatSourcesEvent }
  | { event: "token"; data: ChatTokenEvent }
  | { event: "done"; data: Record<string, never> }
  | { event: "error"; data: ChatErrorEvent };
