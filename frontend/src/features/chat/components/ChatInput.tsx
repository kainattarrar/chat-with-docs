"use client";

import type { KeyboardEvent } from "react";
import { Button } from "@/components/ui/Button";
import { Spinner } from "@/components/ui/Spinner";

interface ChatInputProps {
  value: string;
  onChange: (value: string) => void;
  onSend: (text: string) => void;
  disabled: boolean;
}

export function ChatInput({ value, onChange, onSend, disabled }: ChatInputProps) {
  const handleSend = () => {
    const trimmed = value.trim();
    if (!trimmed || disabled) return;
    onSend(trimmed);
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="flex shrink-0 items-end gap-2 border-t border-zinc-200 p-4 dark:border-zinc-800">
      <textarea
        value={value}
        onChange={(event) => onChange(event.target.value)}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        placeholder="Ask a question about your documents…"
        rows={1}
        className="max-h-32 flex-1 resize-none overflow-y-auto rounded-md border border-zinc-200 px-3 py-2 text-sm focus:ring-1 focus:ring-zinc-400 focus:outline-none disabled:opacity-50 dark:border-zinc-800 dark:bg-zinc-900"
      />
      <Button onClick={handleSend} disabled={disabled || !value.trim()}>
        {disabled ? <Spinner className="h-4 w-4" /> : "Send"}
      </Button>
    </div>
  );
}
