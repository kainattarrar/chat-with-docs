"use client";

import { useState } from "react";
import { useChatContext } from "@/features/chat/context/ChatContext";
import { ChatThread } from "@/features/chat/components/ChatThread";
import { ChatInput } from "@/features/chat/components/ChatInput";
import { EmptyState } from "@/features/chat/components/EmptyState";

export function ChatPage() {
  const { messages, isStreaming, sendMessage } = useChatContext();
  const [inputValue, setInputValue] = useState("");

  const handleSend = (text: string) => {
    setInputValue("");
    sendMessage(text);
  };

  return (
    <div className="flex h-full flex-col">
      {messages.length === 0 ? (
        <EmptyState onExampleClick={setInputValue} />
      ) : (
        <ChatThread messages={messages} />
      )}
      <ChatInput
        value={inputValue}
        onChange={setInputValue}
        onSend={handleSend}
        disabled={isStreaming}
      />
    </div>
  );
}
