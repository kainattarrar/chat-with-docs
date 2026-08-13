"use client";

import { ConnectionStatus } from "@/components/ui/ConnectionStatus";
import { Button } from "@/components/ui/Button";
import { useChatContext } from "@/features/chat/context/ChatContext";

export function Header() {
  const { clearChat } = useChatContext();

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-zinc-200 px-4 dark:border-zinc-800">
      <div className="flex items-center gap-3">
        <h1 className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">
          Chat With Your Documents
        </h1>
        <ConnectionStatus />
      </div>
      <Button variant="secondary" onClick={clearChat}>
        New chat
      </Button>
    </header>
  );
}
