import { Spinner } from "@/components/ui/Spinner";
import { SourcesList } from "@/features/chat/components/SourcesList";
import type { ChatMessage } from "@/features/chat/hooks/useChat";

interface MessageBubbleProps {
  message: ChatMessage;
}

export function MessageBubble({ message }: MessageBubbleProps) {
  const isUser = message.role === "user";
  const isPending = Boolean(message.isStreaming) && message.content.length === 0;

  return (
    <div className={`flex ${isUser ? "justify-end" : "justify-start"}`}>
      <div
        className={`max-w-[75%] rounded-lg px-4 py-2 text-sm ${
          isUser
            ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
            : message.isError
              ? "bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-400"
              : "bg-zinc-100 text-zinc-800 dark:bg-zinc-800 dark:text-zinc-200"
        }`}
      >
        {isPending ? (
          <Spinner className="h-4 w-4" />
        ) : (
          <p className="whitespace-pre-wrap">{message.content}</p>
        )}

        {!isUser && !message.isError && message.sources && message.sources.length > 0 && (
          <SourcesList sources={message.sources} />
        )}
      </div>
    </div>
  );
}
