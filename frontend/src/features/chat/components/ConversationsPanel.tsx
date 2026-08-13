"use client";

import { useState } from "react";
import { useChatContext } from "@/features/chat/context/ChatContext";
import { ConversationRow } from "@/features/chat/components/ConversationRow";

export function ConversationsPanel() {
  const {
    conversations,
    isLoadingConversations,
    conversationsError,
    activeConversationId,
    selectConversation,
    removeConversation,
    isStreaming,
  } = useChatContext();
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const handleDelete = async (id: string, label: string) => {
    if (!window.confirm(`Delete "${label}"? This cannot be undone.`)) return;

    setDeleteError(null);
    try {
      await removeConversation(id);
    } catch {
      setDeleteError("Couldn't delete the conversation. Please try again.");
    }
  };

  return (
    <div className="flex h-full flex-col">
      <h2 className="text-xs font-medium tracking-wide text-zinc-500 uppercase dark:text-zinc-400">
        Chats
      </h2>

      {conversationsError && (
        <p className="mt-3 text-xs text-red-600 dark:text-red-400">{conversationsError}</p>
      )}
      {deleteError && <p className="mt-2 text-xs text-red-600 dark:text-red-400">{deleteError}</p>}

      {isLoadingConversations ? (
        <p className="mt-3 text-sm text-zinc-400 dark:text-zinc-600">Loading…</p>
      ) : conversations.length === 0 ? (
        <p className="mt-3 text-sm text-zinc-400 dark:text-zinc-600">
          No chats yet — ask a question to start one.
        </p>
      ) : (
        <ul className="mt-3 space-y-1">
          {conversations.map((conversation) => (
            <ConversationRow
              key={conversation.id}
              conversation={conversation}
              isActive={conversation.id === activeConversationId}
              disabled={isStreaming}
              onSelect={() => selectConversation(conversation.id)}
              onDelete={() => handleDelete(conversation.id, conversation.title?.trim() || "New chat")}
            />
          ))}
        </ul>
      )}
    </div>
  );
}
