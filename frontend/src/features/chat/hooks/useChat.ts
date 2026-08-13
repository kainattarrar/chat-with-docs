"use client";

import { useCallback, useRef, useState } from "react";
import { streamChat } from "@/lib/api";
import type { ChatSource } from "@/lib/types";

export interface ChatMessage {
  id: string;
  role: "user" | "assistant";
  content: string;
  sources?: ChatSource[];
  isStreaming?: boolean;
  isError?: boolean;
}

// The backend answers each question independently (no multi-turn memory), so
// only the current question is ever sent — prior messages are visual history
// only, never replayed as context.
export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  const sendMessage = useCallback(
    async (question: string) => {
      const trimmed = question.trim();
      if (!trimmed || isStreaming) return;

      const userMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: "user",
        content: trimmed,
      };
      const assistantId = crypto.randomUUID();
      const assistantMessage: ChatMessage = {
        id: assistantId,
        role: "assistant",
        content: "",
        isStreaming: true,
      };

      setMessages((prev) => [...prev, userMessage, assistantMessage]);
      setIsStreaming(true);

      const controller = new AbortController();
      abortRef.current = controller;

      const updateAssistant = (update: Partial<ChatMessage>) => {
        setMessages((prev) =>
          prev.map((message) => (message.id === assistantId ? { ...message, ...update } : message)),
        );
      };

      try {
        for await (const streamEvent of streamChat(trimmed, controller.signal)) {
          switch (streamEvent.event) {
            case "token":
              setMessages((prev) =>
                prev.map((message) =>
                  message.id === assistantId
                    ? { ...message, content: message.content + streamEvent.data.text }
                    : message,
                ),
              );
              break;
            case "sources":
              updateAssistant({ sources: streamEvent.data });
              break;
            case "done":
              updateAssistant({ isStreaming: false });
              break;
            case "error":
              updateAssistant({
                isStreaming: false,
                isError: true,
                content: streamEvent.data.message,
              });
              break;
          }
        }
      } catch {
        updateAssistant({
          isStreaming: false,
          isError: true,
          content: "Something went wrong. Please try again.",
        });
      } finally {
        setIsStreaming(false);
        abortRef.current = null;
      }
    },
    [isStreaming],
  );

  const clearChat = useCallback(() => {
    abortRef.current?.abort();
    setMessages([]);
    setIsStreaming(false);
  }, []);

  return { messages, isStreaming, sendMessage, clearChat };
}
