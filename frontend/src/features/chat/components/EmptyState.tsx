const EXAMPLE_QUESTIONS = [
  "What are the key points in this document?",
  "Summarize the main findings.",
  "What dates or deadlines are mentioned?",
];

interface EmptyStateProps {
  onExampleClick: (question: string) => void;
}

export function EmptyState({ onExampleClick }: EmptyStateProps) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 px-4 text-center">
      <h2 className="text-lg font-semibold text-zinc-900 dark:text-zinc-50">
        Chat With Your Documents
      </h2>
      <p className="text-sm text-zinc-500 dark:text-zinc-400">
        Ask a question about your uploaded documents to get started.
      </p>
      <div className="flex flex-col gap-2">
        {EXAMPLE_QUESTIONS.map((question) => (
          <button
            key={question}
            type="button"
            onClick={() => onExampleClick(question)}
            className="rounded-md border border-zinc-200 px-3 py-2 text-sm text-zinc-600 hover:bg-zinc-50 dark:border-zinc-800 dark:text-zinc-300 dark:hover:bg-zinc-800"
          >
            {question}
          </button>
        ))}
      </div>
    </div>
  );
}
