// Mirrors backend DTOs — one shared source of truth instead of each screen
// inventing its own shape.

export type DocumentStatus = "Processing" | "Ready" | "Failed";

export interface Document {
  id: string;
  fileName: string;
  status: DocumentStatus;
  createdAt: string;
  // Not returned by GET /api/documents (DocumentSummary omits it); present
  // only where the backend actually sends it.
  updatedAt?: string;
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

export interface ChatConversationEvent {
  conversationId: string;
}

export interface ChatDoneEvent {
  conversationId: string;
  // Non-null only when this exchange just generated one (i.e. it was the
  // conversation's first exchange).
  title: string | null;
}

// Discriminated union over the backend's SSE contract (POST /api/chat):
// conversation -> sources -> token* -> done, or an error event in place
// of/interrupting that.
export type ChatStreamEvent =
  | { event: "conversation"; data: ChatConversationEvent }
  | { event: "sources"; data: ChatSourcesEvent }
  | { event: "token"; data: ChatTokenEvent }
  | { event: "done"; data: ChatDoneEvent }
  | { event: "error"; data: ChatErrorEvent };

// Persisted conversation history (GET /api/conversations, GET /api/conversations/{id}).
// Message.sources here mirrors the backend's denormalized MessageSource snapshot —
// note it has no documentId, unlike the live-streaming ChatSource above, since it's
// stored independently of the source document (and survives that document's deletion).

export interface ConversationSummary {
  id: string;
  title: string | null;
  updatedAt: string;
}

export interface ConversationMessageSource {
  fileName: string;
  snippet: string;
  chunkIndex: number;
}

export type ConversationMessageRole = "User" | "Assistant";

export interface ConversationMessage {
  id: string;
  role: ConversationMessageRole;
  content: string;
  sources: ConversationMessageSource[];
  createdAt: string;
}

export interface ConversationDetail {
  id: string;
  title: string | null;
  createdAt: string;
  updatedAt: string;
  messages: ConversationMessage[];
}
