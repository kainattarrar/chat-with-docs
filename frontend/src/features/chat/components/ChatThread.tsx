"use client";

import { useEffect, useRef } from "react";
import { MessageBubble } from "@/features/chat/components/MessageBubble";
import type { ChatMessage } from "@/features/chat/hooks/useChat";

interface ChatThreadProps {
  messages: ChatMessage[];
}

export function ChatThread({ messages }: ChatThreadProps) {
  const bottomRef = useRef<HTMLDivElement>(null);

  // Runs on every content change, including each streamed token, so the
  // thread stays pinned to the bottom as the answer grows.
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: "end" });
  }, [messages]);

  return (
    <div className="flex-1 space-y-4 overflow-y-auto px-4 py-4">
      {messages.map((message) => (
        <MessageBubble key={message.id} message={message} />
      ))}
      <div ref={bottomRef} />
    </div>
  );
}
