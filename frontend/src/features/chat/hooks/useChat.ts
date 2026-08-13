"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { deleteConversation, getConversation, listConversations, streamChat } from "@/lib/api";
import type { ConversationSummary } from "@/lib/types";

// Minimal shape shared by the live-streaming ChatSource and the persisted
// ConversationMessageSource (which has no documentId) — lets a message's
// sources come from either path without casting.
export interface ChatMessageSource {
  fileName: string;
  chunkIndex: number;
  snippet: string;
}

export interface ChatMessage {
  id: string;
  role: "user" | "assistant";
  content: string;
  sources?: ChatMessageSource[];
  isStreaming?: boolean;
  isError?: boolean;
}

export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null);

  const [conversations, setConversations] = useState<ConversationSummary[]>([]);
  const [isLoadingConversations, setIsLoadingConversations] = useState(true);
  const [conversationsError, setConversationsError] = useState<string | null>(null);
  const [isLoadingThread, setIsLoadingThread] = useState(false);

  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const list = await listConversations();
        if (cancelled) return;
        setConversations(list);
        setConversationsError(null);
      } catch {
        if (!cancelled) setConversationsError("Couldn't load conversations.");
      } finally {
        if (!cancelled) setIsLoadingConversations(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const sendMessage = useCallback(
    async (question: string) => {
      const trimmed = question.trim();
      if (!trimmed || isStreaming) return;

      const wasNewConversation = activeConversationId === null;
      const conversationIdAtSend = activeConversationId;

      const userMessage: ChatMessage = { id: crypto.randomUUID(), role: "user", content: trimmed };
      const assistantId = crypto.randomUUID();
      const assistantMessage: ChatMessage = { id: assistantId, role: "assistant", content: "", isStreaming: true };

      setMessages((prev) => [...prev, userMessage, assistantMessage]);
      setIsStreaming(true);

      const controller = new AbortController();
      abortRef.current = controller;

      const updateAssistant = (update: Partial<ChatMessage>) => {
        setMessages((prev) => prev.map((message) => (message.id === assistantId ? { ...message, ...update } : message)));
      };

      try {
        for await (const streamEvent of streamChat(trimmed, conversationIdAtSend ?? undefined, controller.signal)) {
          switch (streamEvent.event) {
            case "conversation": {
              const { conversationId } = streamEvent.data;
              setActiveConversationId(conversationId);
              if (wasNewConversation) {
                setConversations((prev) => [
                  { id: conversationId, title: trimmed, updatedAt: new Date().toISOString() },
                  ...prev,
                ]);
              }
              break;
            }
            case "token":
              updateAssistant({ content: (assistantMessage.content += streamEvent.data.text) });
              break;
            case "sources":
              updateAssistant({ sources: streamEvent.data });
              break;
            case "done": {
              updateAssistant({ isStreaming: false });
              const { conversationId, title } = streamEvent.data;
              setConversations((prev) => {
                const existing = prev.find((c) => c.id === conversationId);
                if (!existing) return prev;
                const updated: ConversationSummary = {
                  ...existing,
                  title: title ?? existing.title,
                  updatedAt: new Date().toISOString(),
                };
                return [updated, ...prev.filter((c) => c.id !== conversationId)];
              });
              break;
            }
            case "error":
              updateAssistant({ isStreaming: false, isError: true, content: streamEvent.data.message });
              break;
          }
        }
      } catch {
        updateAssistant({ isStreaming: false, isError: true, content: "Something went wrong. Please try again." });
      } finally {
        setIsStreaming(false);
        abortRef.current = null;
      }
    },
    [isStreaming, activeConversationId],
  );

  const startNewChat = useCallback(() => {
    if (isStreaming) return;
    abortRef.current?.abort();
    setMessages([]);
    setIsStreaming(false);
    setActiveConversationId(null);
  }, [isStreaming]);

  const selectConversation = useCallback(
    async (id: string) => {
      if (isStreaming || id === activeConversationId) return;

      setIsLoadingThread(true);
      try {
        const detail = await getConversation(id);
        setMessages(
          detail.messages.map((message) => ({
            id: message.id,
            role: message.role === "User" ? "user" : "assistant",
            content: message.content,
            sources: message.sources.length > 0 ? message.sources : undefined,
          })),
        );
        setActiveConversationId(id);
      } catch {
        setConversationsError("Couldn't load that conversation.");
      } finally {
        setIsLoadingThread(false);
      }
    },
    [isStreaming, activeConversationId],
  );

  const removeConversation = useCallback(
    async (id: string) => {
      await deleteConversation(id);
      setConversations((prev) => prev.filter((c) => c.id !== id));
      if (activeConversationId === id) {
        setMessages([]);
        setActiveConversationId(null);
      }
    },
    [activeConversationId],
  );

  return {
    messages,
    isStreaming,
    sendMessage,
    startNewChat,
    conversations,
    isLoadingConversations,
    conversationsError,
    isLoadingThread,
    activeConversationId,
    selectConversation,
    removeConversation,
  };
}
