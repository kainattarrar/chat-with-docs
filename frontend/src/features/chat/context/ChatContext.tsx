"use client";

import { createContext, useContext } from "react";
import { useChat } from "@/features/chat/hooks/useChat";

type ChatContextValue = ReturnType<typeof useChat>;

const ChatContext = createContext<ChatContextValue | null>(null);

// Header (New chat button) and the main content area (thread + input) are
// siblings under the root layout, not parent/child — Context is what lets
// them share one chat session without threading props through the layout.
export function ChatProvider({ children }: { children: React.ReactNode }) {
  const chat = useChat();
  return <ChatContext.Provider value={chat}>{children}</ChatContext.Provider>;
}

export function useChatContext() {
  const context = useContext(ChatContext);
  if (!context) {
    throw new Error("useChatContext must be used within a ChatProvider");
  }
  return context;
}
